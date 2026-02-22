using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
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
        private readonly TeamStandingsRepository _teamStandingsRepository;

        public PlayoffService(
            PlayoffMatchupRepository playoffMatchupRepository,
            RoundRobinMatchupRepository roundRobinMatchupRepository,
            WeekRepository weekRepository,
            TeamRepository teamRepository,
            ConferenceRepository conferenceRepository,
            SeasonRepository seasonRepository,
            TeamStandingsRepository teamStandingsRepository)
        {
            _playoffMatchupRepository = playoffMatchupRepository;
            _roundRobinMatchupRepository = roundRobinMatchupRepository;
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _conferenceRepository = conferenceRepository;
            _seasonRepository = seasonRepository;
            _teamStandingsRepository = teamStandingsRepository;
        }

        public (List<Match> createdMatches, List<WeeklyMatchup> matchupOutputs) GetIndividualMatchups(Week week, List<(Team a, Team b)> teamMatchups, Dictionary<int, List<DeckSubmission>> submissionsByTeamId)
        {
            var createdMatches = new List<Match>();

            // We will also build output per matchup.
            var matchupOutputs = new List<WeeklyMatchup>();
            foreach (var (teamA, teamB) in teamMatchups)
            {
                // Skip BYE matchups (where teamA == teamB) - no matches need to be created
                if (teamA.Id == teamB.Id)
                {
                    continue;
                }

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

                    // Canonical order (Player1Id < Player2Id) for DB unique constraint
                    var player1Id = Math.Min(p1.Id, p2.Id);
                    var player2Id = Math.Max(p1.Id, p2.Id);
                    createdMatches.Add(new Match
                    {
                        WeekId = week.Id,
                        Player1Id = player1Id,
                        Player2Id = player2Id,
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
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(firstTeam.SeasonId);

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
            var (matchups, _) = await GetFirstPlayoffWeekMatchupsAndPlayoffTeamsAsync(season, teams);
            return matchups;
        }

        /// <summary>
        /// Returns first-round playoff matchups and the list of teams that qualified for playoffs.
        /// Derives playoff qualifiers from global standings + conferences (top N per conference by Seed).
        /// First-round seeding order is by global Seed (asc).
        /// </summary>
        private async Task<(List<(Team a, Team b)> matchups, List<Team> playoffTeams)> GetFirstPlayoffWeekMatchupsAndPlayoffTeamsAsync(Season season, IReadOnlyList<Team> teams)
        {
            var standings = await _teamStandingsRepository.GetBySeasonIdWithoutTeamAsync(season.Id);
            var conferences = await _conferenceRepository.GetBySeasonAsync(season.Id);
            var seededTeams = GetPlayoffQualifiersFromStandings(standings, teams, conferences);

            if (seededTeams.Count < 2)
                return (new List<(Team, Team)>(), seededTeams);

            var matchups = GenerateBracketMatchups(seededTeams, round: 1);
            return (matchups, seededTeams);
        }

        /// <summary>
        /// Derives playoff qualifiers from global standings: for each conference with PlayoffTeamsCount > 0,
        /// takes top N teams by Seed (asc); returns combined list ordered by global Seed (asc) for bracket seeding.
        /// </summary>
        private static List<Team> GetPlayoffQualifiersFromStandings(
            IReadOnlyList<TeamStandings> standings,
            IReadOnlyList<Team> teams,
            IReadOnlyList<Conference> conferences)
        {
            var standingByTeamId = standings.ToDictionary(s => s.TeamId);
            var playoffTeams = new List<Team>();

            foreach (var conference in conferences.Where(c => c.PlayoffTeamsCount > 0))
            {
                var conferenceQualifiers = teams
                    .Where(t => t.ConferenceId == conference.Id && standingByTeamId.ContainsKey(t.Id))
                    .OrderBy(t => standingByTeamId[t.Id].Seed)
                    .Take(conference.PlayoffTeamsCount)
                    .ToList();
                playoffTeams.AddRange(conferenceQualifiers);
            }

            return playoffTeams.OrderBy(t => standingByTeamId[t.Id].Seed).ToList();
        }

        /// <summary>
        /// Returns the set of team IDs that qualify for playoffs (top N per conference by Seed from global standings).
        /// Used by TeamStandingsService to allow tiebreaker edits only for playoff qualifiers.
        /// </summary>
        public async Task<IReadOnlySet<int>> GetPlayoffQualifierTeamIdsAsync(int seasonId)
        {
            var standings = await _teamStandingsRepository.GetBySeasonIdWithoutTeamAsync(seasonId);
            var teams = await _teamRepository.GetBySeasonAsync(seasonId);
            var conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
            var qualifiers = GetPlayoffQualifiersFromStandings(standings, teams, conferences);
            return qualifiers.Select(t => t.Id).ToHashSet();
        }

        /// <summary>
        /// Returns first-round playoff matchups and qualifier lists for the season (for switch-to-playoffs reporting).
        /// </summary>
        public async Task<(List<(Team a, Team b)> matchups, List<Team> playoffTeams, List<Team> nonPlayoffTeams)> GetFirstPlayoffWeekMatchupsAndQualifiersAsync(int seasonId)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            var teams = await _teamRepository.GetBySeasonAsync(seasonId);
            var (matchups, playoffTeams) = await GetFirstPlayoffWeekMatchupsAndPlayoffTeamsAsync(season, teams);
            var playoffIds = playoffTeams.Select(t => t.Id).ToHashSet();
            var nonPlayoffTeams = teams.Where(t => !playoffIds.Contains(t.Id)).ToList();
            return (matchups, playoffTeams, nonPlayoffTeams);
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
            
            int count = teams.Count;
            
            // Calculate next power of 2 to determine bracket size
            int nextPowerOfTwo = 1;
            while (nextPowerOfTwo < count)
            {
                nextPowerOfTwo *= 2;
            }
            
            // Number of teams that get byes (asymmetric top cut)
            int byeCount = nextPowerOfTwo - count;
            
            // Top seeds get byes - create BYE matchups (team vs itself)
            for (int i = 0; i < byeCount; i++)
            {
                matchups.Add((teams[i], teams[i])); // BYE matchup: team vs itself
            }
            
            // Remaining teams play in round 1
            // Pair teams: (byeCount) vs (count-1), (byeCount+1) vs (count-2), etc.
            int playingTeamsStart = byeCount;
            int playingTeamsEnd = count - 1;
            
            for (int i = playingTeamsStart; i < playingTeamsStart + (count - byeCount) / 2; i++)
            {
                var teamA = teams[i];
                var teamB = teams[playingTeamsEnd - (i - playingTeamsStart)];
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
                    // Check if this is a BYE matchup (team vs itself)
                    bool isBye = m.a.Id == m.b.Id;
                    
                    // For BYE matchups, use the team as both Team1Id and Team2Id
                    // For normal matchups, normalize team order: ensure Team1Id <= Team2Id for unique index
                    var team1Id = m.a.Id;
                    var team2Id = isBye ? m.a.Id : m.b.Id;
                    
                    // For normal matchups, ensure Team1Id <= Team2Id
                    if (!isBye && team1Id > team2Id)
                    {
                        (team1Id, team2Id) = (team2Id, team1Id);
                    }
                    
                    return new PlayoffMatchup
                    {
                        WeekId = week.Id,
                        Team1Id = team1Id,
                        Team2Id = team2Id,
                        MatchupType = isBye ? MatchupType.Bye : MatchupType.Normal,
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
                // Handle BYE matchups: team automatically advances
                if (matchup.MatchupType == MatchupType.Bye)
                {
                    matchup.TeamWinnerId = matchup.Team1Id; // Team gets the bye and advances
                    continue;
                }

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

        public Task<List<Team>> GetByeTeamsForPairingsDisplayAsync(IReadOnlyList<(Team a, Team b)> teamMatchups, IReadOnlyList<Team> allTeams)
        {
            // BYE matchup = same team on both sides (equivalent to MatchupType.Bye after save)
            var byeTeams = teamMatchups
                .Where(m => m.a.Id == m.b.Id)
                .Select(m => m.a)
                .Distinct()
                .ToList();
            return Task.FromResult(byeTeams);
        }

        public async Task<IReadOnlySet<int>> GetTeamIdsRequiredForSubmissionsAsync(IReadOnlyList<Team> teams, int weekNumber)
        {
            var matchups = await GetTeamMatchups(teams, weekNumber);
            var ids = matchups.SelectMany(m => new[] { m.a.Id, m.b.Id }).ToHashSet();
            return ids;
        }

        public Task<RoundRobinSuggestionResult?> GetSuggestedRoundsAsync(int seasonId) => Task.FromResult<RoundRobinSuggestionResult?>(null);

        public async Task<List<(Team a, Team b)>?> GetExistingTeamMatchupsAsync(Week week, IReadOnlyList<Team> teams)
        {
            var existingMatchups = await _playoffMatchupRepository.GetByWeekIdAsync(week.Id);
            if (existingMatchups.Count == 0)
                return null;

            var teamById = teams.ToDictionary(t => t.Id);
            var list = new List<(Team a, Team b)>();
            foreach (var m in existingMatchups.OrderBy(x => x.BracketPosition))
            {
                if (!teamById.TryGetValue(m.Team1Id, out var t1))
                    continue;
                bool isBye = m.MatchupType == MatchupType.Bye || m.Team1Id == m.Team2Id;
                if (isBye)
                {
                    list.Add((t1, t1));
                    continue;
                }
                if (!teamById.TryGetValue(m.Team2Id, out var t2))
                    continue;
                list.Add((t1, t2));
            }
            return list.Count == 0 ? null : list;
        }

        public async Task<BaseResult> ValidateTeamCanSubmitForWeekAsync(Season season, Week week, int teamId)
        {
            var teams = await _teamRepository.GetBySeasonAsync(season.Id);
            var teamById = teams.ToDictionary(t => t.Id);
            if (!teamById.ContainsKey(teamId))
            {
                return new BaseResult(false, "Team is not in this season.");
            }

            var existingMatchups = await GetExistingTeamMatchupsAsync(week, teams);
            if (existingMatchups == null || existingMatchups.Count == 0)
            {
                throw new InvalidOperationException($"No playoff matchups exist for week {week.WeekNumber}. Matchups must be generated before teams can submit.");
            }

            var eligibleIds = existingMatchups
                .SelectMany(m => new[] { m.a.Id, m.b.Id })
                .ToHashSet();
            if (eligibleIds.Contains(teamId))
                return new BaseResult(true, "Team may submit.");
            return new BaseResult(false, "Only teams that are in this week's playoff matchups may submit decks.");
        }
    }
}
