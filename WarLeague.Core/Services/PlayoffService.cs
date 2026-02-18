using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Services
{
    public class PlayoffService : IMatchupService
    {
        private readonly PlayoffMatchupRepository _playoffMatchupRepository;
        private readonly RoundRobinMatchupRepository _roundRobinMatchupRepository;
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly ConferenceRepository _conferenceRepository;
        private readonly SeasonRepository _seasonRepository;

        public PlayoffService(
            PlayoffMatchupRepository playoffMatchupRepository,
            RoundRobinMatchupRepository roundRobinMatchupRepository,
            WeekRepository weekRepository,
            TeamRepository teamRepository,
            ConferenceRepository conferenceRepository,
            SeasonRepository seasonRepository)
        {
            _playoffMatchupRepository = playoffMatchupRepository;
            _roundRobinMatchupRepository = roundRobinMatchupRepository;
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _conferenceRepository = conferenceRepository;
            _seasonRepository = seasonRepository;
        }

        public (List<Match> createdMatches, List<WeeklyMatchup> matchupOutputs) GetIndividualMatchups(Week week, List<(Team a, Team b)> teamMatchups, Dictionary<int, List<DeckSubmission>> submissionsByTeamId)
        {
            var createdMatches = new List<Match>();

            // We will also build output per matchup.
            var matchupOutputs = new List<WeeklyMatchup>();
            foreach (var (teamA, teamB) in teamMatchups)
            {
                var submissionsA = submissionsByTeamId.TryGetValue(teamA.Id, out var aSubmissions) ? aSubmissions : new List<DeckSubmission>();
                var submissionsB = submissionsByTeamId.TryGetValue(teamB.Id, out var bSubmissions) ? bSubmissions : new List<DeckSubmission>();

                // Sort by SeatNumber instead of randomizing
                var sortedA = submissionsA.OrderBy(ds => ds.SeatNumber).Select(ds => ds.Player).ToList();
                var sortedB = submissionsB.OrderBy(ds => ds.SeatNumber).Select(ds => ds.Player).ToList();

                int pairCount = Math.Min(sortedA.Count, sortedB.Count);
                var pairs = new List<(Player, Player)>(capacity: pairCount);

                for (int i = 0; i < pairCount; i++)
                {
                    var p1 = sortedA[i];
                    var p2 = sortedB[i];
                    pairs.Add((p1, p2));

                    createdMatches.Add(new Match
                    {
                        WeekId = week.Id,
                        Player1Id = p1.Id,
                        Player2Id = p2.Id,
                        Team1Id = teamA.Id,
                        Team2Id = teamB.Id,
                        Status = MatchStatus.Scheduled
                    });
                }

                var unpairedA = sortedA.Skip(pairCount).ToList();
                var unpairedB = sortedB.Skip(pairCount).ToList();

                matchupOutputs.Add(new WeeklyMatchup(teamA, teamB, pairs, unpairedA, unpairedB));
            }

            return (createdMatches, matchupOutputs);
        }

        public async Task<List<(Team a, Team b)>> GetTeamMatchups(IReadOnlyList<Team> teams, int weekNumber)
        {
            if (teams.Count == 0)
            {
                return new List<(Team, Team)>();
            }

            // Get the season from first team's SeasonId
            var firstTeam = teams.First();
            var season = await _seasonRepository.GetByIdOrDefault(firstTeam.SeasonId);
            
            if (season == null)
            {
                return new List<(Team, Team)>();
            }

            // Check if this is the first playoff week by looking for existing playoff matchups
            var allWeeks = await _weekRepository.GetBySeasonAsync(season.Id);
            var playoffWeeks = allWeeks.Where(w => w.Status == WeekStatus.Completed || w.Status == WeekStatus.InProgress || w.Status == WeekStatus.SubmissionsClosed)
                .OrderBy(w => w.WeekNumber)
                .ToList();

            var existingPlayoffMatchups = await _playoffMatchupRepository.GetBySeasonIdAsync(season.Id);
            
            if (existingPlayoffMatchups.Count == 0)
            {
                // First playoff week - seed teams based on round-robin standings
                return await GetFirstPlayoffWeekMatchupsAsync(season, teams);
            }
            else
            {
                // Subsequent playoff week - advance winners from previous week
                var previousWeek = playoffWeeks
                    .Where(w => w.WeekNumber < weekNumber)
                    .OrderByDescending(w => w.WeekNumber)
                    .FirstOrDefault();

                if (previousWeek == null)
                {
                    return new List<(Team, Team)>();
                }

                return await GetSubsequentPlayoffWeekMatchupsAsync(previousWeek, weekNumber);
            }
        }

        private async Task<List<(Team a, Team b)>> GetFirstPlayoffWeekMatchupsAsync(Season season, IReadOnlyList<Team> teams)
        {
            // Get all completed round-robin weeks
            var allWeeks = await _weekRepository.GetBySeasonAsync(season.Id);
            var completedWeeks = allWeeks
                .Where(w => w.Status == WeekStatus.Completed)
                .OrderBy(w => w.WeekNumber)
                .ToList();

            // Calculate standings from round-robin matchups
            var standings = new Dictionary<int, int>(); // TeamId -> Wins

            foreach (var week in completedWeeks)
            {
                var roundRobinMatchups = await _roundRobinMatchupRepository.GetByWeekIdAsync(week.Id);
                foreach (var matchup in roundRobinMatchups)
                {
                    if (matchup.TeamWinnerId.HasValue)
                    {
                        standings.TryGetValue(matchup.TeamWinnerId.Value, out var currentWins);
                        standings[matchup.TeamWinnerId.Value] = currentWins + 1;
                    }
                }
            }

            // Get conferences with playoff team counts
            var conferences = await _conferenceRepository.GetBySeasonAsync(season.Id);
            var playoffTeams = new List<Team>();

            foreach (var conference in conferences)
            {
                if (conference.PlayoffTeamsCount.HasValue && conference.PlayoffTeamsCount.Value > 0)
                {
                    var conferenceTeams = teams.Where(t => t.ConferenceId == conference.Id).ToList();
                    var seededTeamsLocal = conferenceTeams
                        .OrderByDescending(t => standings.GetValueOrDefault(t.Id, 0))
                        .ThenBy(t => t.Id) // Tiebreaker: lower ID
                        .Take(conference.PlayoffTeamsCount.Value)
                        .ToList();

                    playoffTeams.AddRange(seededTeamsLocal);
                }
            }

            if (playoffTeams.Count < 2)
            {
                return new List<(Team, Team)>();
            }

            // Generate bracket matchups (single elimination seeding: 1 vs N, 2 vs N-1, etc.)
            var seededTeams = playoffTeams
                .OrderByDescending(t => standings.GetValueOrDefault(t.Id, 0))
                .ThenBy(t => t.Id)
                .ToList();

            return GenerateBracketMatchups(seededTeams, round: 1);
        }

        private async Task<List<(Team a, Team b)>> GetSubsequentPlayoffWeekMatchupsAsync(Week previousWeek, int currentWeekNumber)
        {
            var previousMatchups = await _playoffMatchupRepository.GetByWeekIdAsync(previousWeek.Id);
            var winnersWithPosition = previousMatchups
                .Where(m => m.TeamWinnerId.HasValue)
                .OrderBy(m => m.BracketPosition)
                .Select(m => m.TeamWinnerId!.Value)
                .ToList();

            if (winnersWithPosition.Count < 2)
            {
                return new List<(Team, Team)>();
            }

            // Get team objects, maintaining order by bracket position
            var allTeams = await _teamRepository.GetBySeasonAsync(previousWeek.SeasonId);
            var teamDict = allTeams.Where(t => winnersWithPosition.Contains(t.Id)).ToDictionary(t => t.Id);
            var winnerTeams = winnersWithPosition.Select(id => teamDict[id]).ToList();

            // Generate bracket matchups for next round
            // Round will be calculated in SaveTeamMatchupsAsync based on matchup count
            return GenerateBracketMatchups(winnerTeams, round: 0);
        }

        private static List<(Team a, Team b)> GenerateBracketMatchups(List<Team> teams, int round)
        {
            var matchups = new List<(Team, Team)>();
            
            // Single elimination bracket: pair teams (1st vs last, 2nd vs 2nd-to-last, etc.)
            // Maintain order from previous round (winners should be ordered by their bracket position)
            int count = teams.Count;
            for (int i = 0; i < count / 2; i++)
            {
                var teamA = teams[i];
                var teamB = teams[count - 1 - i];
                matchups.Add((teamA, teamB));
            }

            return matchups;
        }

        private static int CalculateRoundNumber(int matchupCount)
        {
            // Calculate which round this is based on matchup count
            // 4 matchups = round 1 (quarterfinals), 2 matchups = round 2 (semifinals), 1 matchup = round 3 (finals)
            // Round = log2(maxPossibleMatchups) - log2(currentMatchups) + 1
            // For 8 teams: max = 4 matchups, so round 1 = log2(4) - log2(4) + 1 = 1
            // For 4 teams: max = 4 matchups, so round 2 = log2(4) - log2(2) + 1 = 2
            // For 2 teams: max = 4 matchups, so round 3 = log2(4) - log2(1) + 1 = 3
            
            if (matchupCount <= 0) return 1;
            
            // Find the power of 2 that matchupCount fits into
            int maxMatchups = 1;
            while (maxMatchups < matchupCount)
            {
                maxMatchups *= 2;
            }
            
            // Calculate round: if we have 4 matchups, it's round 1; 2 matchups is round 2; 1 matchup is round 3
            int round = (int)Math.Log2(maxMatchups) - (int)Math.Log2(matchupCount) + 1;
            return round;
        }

        public async Task<BaseResult> SaveTeamMatchupsAsync(Week week, IReadOnlyList<Team> teams, IReadOnlyList<(Team a, Team b)> teamMatchups)
        {
            var existingPlayoffMatchups = await _playoffMatchupRepository.GetByWeekIdAsync(week.Id);
            if (existingPlayoffMatchups.Count > 0)
            {
                return new BaseResult(false, $"Playoff matchups already exist for week {week.WeekNumber}. Refusing to generate new pairings to avoid duplicates.");
            }

            // Determine round number based on number of matchups
            // 4 matchups = round 1 (quarterfinals), 2 matchups = round 2 (semifinals), 1 matchup = round 3 (finals)
            int round = CalculateRoundNumber(teamMatchups.Count);

            var playoffMatchups = teamMatchups
                .Select((m, index) =>
                {
                    // Normalize team order: ensure Team1Id < Team2Id for unique index
                    var team1Id = Math.Min(m.a.Id, m.b.Id);
                    var team2Id = Math.Max(m.a.Id, m.b.Id);
                    return new PlayoffMatchup
                    {
                        WeekId = week.Id,
                        Team1Id = team1Id,
                        Team2Id = team2Id,
                        Round = round,
                        BracketPosition = index
                    };
                })
                .ToList();

            await _playoffMatchupRepository.AddRangeAsync(playoffMatchups);

            return new BaseResult(true, "Playoff matchups saved.");
        }

        public async Task<BaseResult> UpdateMatchupWinnersForWeekAsync(Week week, IReadOnlyList<Match> matches)
        {
            var playoffMatchups = await _playoffMatchupRepository.GetByWeekIdAsync(week.Id);
            if (playoffMatchups.Count == 0)
            {
                return new BaseResult(false, "No playoff matchups found for the active week.");
            }

            foreach (var matchup in playoffMatchups)
            {
                var teamMatches = matches.Where(m =>
                    (m.Team1Id == matchup.Team1Id && m.Team2Id == matchup.Team2Id)
                    || (m.Team1Id == matchup.Team2Id && m.Team2Id == matchup.Team1Id))
                    .ToList();

                var team1Wins = teamMatches.Count(m => m.WinnerTeamId == matchup.Team1Id);
                var team2Wins = teamMatches.Count(m => m.WinnerTeamId == matchup.Team2Id);

                if (team1Wins == team2Wins)
                {
                    return new BaseResult(false, $"Cannot close week: matchup between TeamId {matchup.Team1Id} and TeamId {matchup.Team2Id} is tied ({team1Wins}-{team2Wins}).");
                }

                matchup.TeamWinnerId = team1Wins > team2Wins
                    ? matchup.Team1Id
                    : matchup.Team2Id;
            }

            await _playoffMatchupRepository.UpdateRangeAsync(playoffMatchups);
            return new BaseResult(true, "Playoff winners updated.");
        }
    }
}
