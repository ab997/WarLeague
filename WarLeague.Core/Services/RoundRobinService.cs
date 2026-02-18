using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Services
{
    public class RoundRobinService : IMatchupService
    {
        private readonly RoundRobinMatchupRepository _roundRobinMatchupRepository;

        public RoundRobinService(RoundRobinMatchupRepository roundRobinMatchupRepository)
        {
            _roundRobinMatchupRepository = roundRobinMatchupRepository;
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

        public List<(Team a, Team b)> GetTeamMatchups(IReadOnlyList<Team> teams, int weekNumber)
        {
            var conferenceGroups = teams
                .GroupBy(t => t.ConferenceId)
                .OrderBy(g => g.Key)
                .ToList();

            var allMatchups = new List<(Team, Team)>();

            foreach (var conferenceGroup in conferenceGroups)
            {
                var conferenceMatchups = GetConferenceTeamMatchups(conferenceGroup, weekNumber);
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

        public async Task<BaseResult> SaveTeamMatchupsAsync(Week week, IReadOnlyList<Team> teams, IReadOnlyList<(Team a, Team b)> teamMatchups)
        {
            var existingRoundRobinMatchups = await _roundRobinMatchupRepository.GetByWeekIdAsync(week.Id);
            if (existingRoundRobinMatchups.Count > 0)
            {
                return new BaseResult(false, $"Round-robin matchups already exist for week {week.WeekNumber}. Refusing to generate new pairings to avoid duplicates.");
            }

            var roundRobinMatchups = teamMatchups
                .Select(m => new RoundRobinMatchup
                {
                    WeekId = week.Id,
                    Team1Id = m.a.Id,
                    Team2Id = m.b.Id,
                    MatchupType = MatchupType.Normal
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

        public async Task<BaseResult> UpdateMatchupWinnersForWeekAsync(Week week, IReadOnlyList<Match> matches)
        {
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
