using WarLeague.Core.Repositories;
using WarLeague.Core.Model;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Services
{
    public class RoundRobinService : IMatchupService
    {
        private readonly RoundRobinMatchupRepository _roundRobinMatchupRepository;
        private readonly TeamRepository _teamRepository;
        private readonly ConferenceRepository _conferenceRepository;
        private readonly WeekRepository _weekRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;

        public RoundRobinService(
            RoundRobinMatchupRepository roundRobinMatchupRepository,
            TeamRepository teamRepository,
            ConferenceRepository conferenceRepository,
            WeekRepository weekRepository,
            PlayerSeasonTeamRepository playerSeasonTeamRepository)
        {
            _roundRobinMatchupRepository = roundRobinMatchupRepository;
            _teamRepository = teamRepository;
            _conferenceRepository = conferenceRepository;
            _weekRepository = weekRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
        }

        /// <summary>
        /// Number of rounds needed for a single round-robin in a conference (circle method: N-1 if even, N if odd with bye).
        /// </summary>
        public static int GetRoundsForConferenceSize(int teamCount)
        {
            if (teamCount < 2) return 0;
            return teamCount % 2 == 0 ? teamCount - 1 : teamCount;
        }

        public async Task<(List<Match> createdMatches, List<WeeklyMatchup> matchupOutputs)> GetIndividualMatchupsAsync(int weekId)
        {
            var (week, teams) = await LoadWeekAndTeamsAsync(weekId);
            var teamMatchups = await GetExistingTeamMatchupsAsync(weekId);
            if (teamMatchups is null || teamMatchups.Count == 0)
            {
                return (new List<Match>(), new List<WeeklyMatchup>());
            }

            var submissionsByTeamId = await BuildSubmissionsByTeamAsync(week, week.SeasonId);

            var createdMatches = new List<Match>();
            var matchupOutputs = new List<WeeklyMatchup>();

            foreach (var (teamA, teamB) in teamMatchups)
            {
                if (teamA.Id == teamB.Id)
                {
                    // Round-robin does not currently use explicit BYE matchups in pairings generation,
                    // but guard here for consistency.
                    continue;
                }

                var submissionsA = submissionsByTeamId.TryGetValue(teamA.Id, out var aSubmissions)
                    ? aSubmissions
                    : new List<DeckSubmission>();
                var submissionsB = submissionsByTeamId.TryGetValue(teamB.Id, out var bSubmissions)
                    ? bSubmissions
                    : new List<DeckSubmission>();

                var sortedA = submissionsA.OrderBy(ds => ds.SeatNumber).Select(ds => ds.Player).ToList();
                var sortedB = submissionsB.OrderBy(ds => ds.SeatNumber).Select(ds => ds.Player).ToList();

                int pairCount = Math.Min(sortedA.Count, sortedB.Count);
                var pairs = new List<(Player, Player)>(capacity: pairCount);

                for (int i = 0; i < pairCount; i++)
                {
                    var p1 = sortedA[i];
                    var p2 = sortedB[i];
                    pairs.Add((p1, p2));

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

        public async Task<List<(Team a, Team b)>> GetTeamMatchupsAsync(int weekId)
        {
            var (week, teams) = await LoadWeekAndTeamsAsync(weekId);

            var conferenceGroups = teams
                .GroupBy(t => t.ConferenceId)
                .OrderBy(g => g.Key)
                .ToList();

            var allMatchups = new List<(Team, Team)>();

            foreach (var conferenceGroup in conferenceGroups)
            {
                var conferenceMatchups = GetConferenceTeamMatchups(conferenceGroup, week.WeekNumber);
                allMatchups.AddRange(conferenceMatchups);
            }

            return allMatchups;
        }

        private static List<(Team a, Team b)> GetConferenceTeamMatchups(IEnumerable<Team> conferenceTeams, int weekNumber)
        {
            var ordered = conferenceTeams
                .OrderBy(t => t.Id)
                .ToList();

            if (ordered.Count < 2)
            {
                return new List<(Team, Team)>();
            }

            var bye = new Team { Id = -1, Name = "BYE" };
            if (ordered.Count % 2 == 1)
            {
                ordered.Add(bye);
            }

            int n = ordered.Count;
            int rounds = n - 1;
            int roundIndex = ((weekNumber - 1) % rounds + rounds) % rounds;

            var arr = ordered.ToList();
            for (int r = 0; r < roundIndex; r++)
            {
                RotateRoundRobinInPlace(arr);
            }

            var matchups = new List<(Team, Team)>(capacity: n / 2);
            for (int i = 0; i < n / 2; i++)
            {
                var a = arr[i];
                var b = arr[n - 1 - i];

                if (a.Id == bye.Id || b.Id == bye.Id)
                {
                    continue;
                }

                matchups.Add((a, b));
            }

            return matchups;
        }

        public async Task<BaseResult> SaveTeamMatchupsAsync(int weekId, IReadOnlyList<(Team a, Team b)> teamMatchups)
        {
            var week = await _weekRepository.GetByIdAsync(weekId)
                ?? throw new InvalidOperationException($"Week with id {weekId} not found.");
            var teams = await _teamRepository.GetBySeasonAsync(week.SeasonId);

            var existingRoundRobinMatchups = await _roundRobinMatchupRepository.GetByWeekIdAsync(week.Id);
            if (existingRoundRobinMatchups.Count > 0)
            {
                return new BaseResult(false, $"Round-robin matchups already exist for week {week.WeekNumber}. Refusing to generate new pairings to avoid duplicates.");
            }

            var roundRobinMatchups = teamMatchups
                .Select(m =>
                {
                    // Normalize team order: ensure Team1Id <= Team2Id for unique index
                    var team1Id = Math.Min(m.a.Id, m.b.Id);
                    var team2Id = Math.Max(m.a.Id, m.b.Id);
                    return new RoundRobinMatchup
                    {
                        WeekId = week.Id,
                        Team1Id = team1Id,
                        Team2Id = team2Id,
                        MatchupType = MatchupType.Normal
                    };
                })
                .ToList();

            var participatingTeamIds = teamMatchups
                .SelectMany(m => new[] { m.a.Id, m.b.Id })
                .ToHashSet();

            var byeMatchups = teams
                .Where(t => !participatingTeamIds.Contains(t.Id))
                .Select(t => new RoundRobinMatchup
                {
                    WeekId = week.Id,
                    Team1Id = t.Id,
                    Team2Id = t.Id,
                    MatchupType = MatchupType.Bye
                })
                .ToList();

            roundRobinMatchups.AddRange(byeMatchups);
            await _roundRobinMatchupRepository.AddRangeAsync(roundRobinMatchups);

            return new BaseResult(true, "Round-robin matchups saved.");
        }

        public async Task<BaseResult> UpdateMatchupWinnersForWeekAsync(int weekId, IReadOnlyList<Match> matches)
        {
            var week = await _weekRepository.GetByIdAsync(weekId)
                ?? throw new InvalidOperationException($"Week with id {weekId} not found.");

            var roundRobinMatchups = await _roundRobinMatchupRepository.GetByWeekIdAsync(week.Id);
            if (roundRobinMatchups.Count == 0)
            {
                return new BaseResult(false, "No round-robin matchups found for the active week.");
            }

            foreach (var matchup in roundRobinMatchups)
            {
                if (matchup.MatchupType == MatchupType.Bye)
                {
                    matchup.TeamWinnerId = matchup.Team1Id;
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

            await _roundRobinMatchupRepository.UpdateRangeAsync(roundRobinMatchups);
            return new BaseResult(true, "Round-robin winners updated.");
        }

        public async Task<List<Team>> GetByeTeamsForPairingsDisplayAsync(int weekId)
        {
            var (week, teams) = await LoadWeekAndTeamsAsync(weekId);
            var teamMatchups = await GetExistingTeamMatchupsAsync(weekId) ?? new List<(Team a, Team b)>();

            var participatingIds = teamMatchups.SelectMany(m => new[] { m.a.Id, m.b.Id }).ToHashSet();
            var byeTeams = teams.Where(t => !participatingIds.Contains(t.Id)).ToList();
            return byeTeams;
        }

        public async Task<IReadOnlySet<int>> GetTeamIdsRequiredForSubmissionsAsync(int weekId)
        {
            var week = await _weekRepository.GetByIdAsync(weekId)
                ?? throw new InvalidOperationException($"Week with id {weekId} not found.");
            var teams = await _teamRepository.GetBySeasonAsync(week.SeasonId);

            var ids = teams.Select(t => t.Id).ToHashSet();
            return ids;
        }

        public async Task<RoundRobinSuggestionResult?> GetSuggestedRoundsAsync(int seasonId)
        {
            var teams = await _teamRepository.GetBySeasonAsync(seasonId);
            var conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
            var conferenceById = conferences.ToDictionary(c => c.Id);

            var byConference = teams
                .GroupBy(t => t.ConferenceId)
                .OrderBy(g => g.Key)
                .ToList();

            var result = new RoundRobinSuggestionResult();
            foreach (var grp in byConference)
            {
                int count = grp.Count();
                if (count < 2) continue;
                string name = conferenceById.TryGetValue(grp.Key, out var c) ? c.Name : $"Conference {grp.Key}";
                result.Conferences.Add(new RoundRobinConferenceSuggestion
                {
                    ConferenceName = name,
                    TeamCount = count,
                    Rounds = GetRoundsForConferenceSize(count)
                });
            }

            if (result.Conferences.Count == 0)
                return null;

            result.TotalSuggestedWeeks = result.Conferences.Max(x => x.Rounds);
            return result;
        }

        public async Task<List<(Team a, Team b)>?> GetExistingTeamMatchupsAsync(int weekId)
        {
            var (week, teams) = await LoadWeekAndTeamsAsync(weekId);

            var matchups = await _roundRobinMatchupRepository.GetByWeekIdAsync(week.Id);
            if (matchups.Count == 0) return null;

            var teamById = teams.ToDictionary(t => t.Id);
            var list = new List<(Team a, Team b)>();
            foreach (var m in matchups)
            {
                if (m.MatchupType == MatchupType.Bye) continue;
                if (!teamById.TryGetValue(m.Team1Id, out var t1) || !teamById.TryGetValue(m.Team2Id, out var t2))
                    continue;
                list.Add((t1, t2));
            }

            return list.Count == 0 ? null : list;
        }

        public Task<BaseResult> ValidateTeamCanSubmitForWeekAsync(int weekId, int teamId)
        {
            // Round-robin: any team in the season may submit.
            return Task.FromResult(new BaseResult(true, "Team may submit."));
        }

        private async Task<(Week week, List<Team> teams)> LoadWeekAndTeamsAsync(int weekId)
        {
            var week = await _weekRepository.GetByIdAsync(weekId)
                ?? throw new InvalidOperationException($"Week with id {weekId} not found.");

            var teams = await _teamRepository.GetBySeasonAsync(week.SeasonId);
            return (week, teams);
        }

        private async Task<Dictionary<int, List<DeckSubmission>>> BuildSubmissionsByTeamAsync(Week week, int seasonId)
        {
            var memberships = await _playerSeasonTeamRepository.GetBySeasonAsync(seasonId);
            var membershipByPlayerId = memberships
                .GroupBy(m => m.PlayerId)
                .ToDictionary(g => g.Key, g => g.First());

            var submissionsByTeamId = week.DeckSubmissions
                .Where(ds => membershipByPlayerId.ContainsKey(ds.PlayerId))
                .GroupBy(ds => membershipByPlayerId[ds.PlayerId].TeamId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return submissionsByTeamId;
        }

        // Circle method rotation: keep index 0 fixed, rotate the rest.
        // Example [A, B, C, D] -> [A, D, B, C]
        private static void RotateRoundRobinInPlace(List<Team> arr)
        {
            if (arr.Count <= 2) return;

            var last = arr[^1];
            for (int i = arr.Count - 1; i >= 2; i--)
            {
                arr[i] = arr[i - 1];
            }
            arr[1] = last;
        }
    }
}
