using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Core.Model;

namespace WarLeague.Core.Helpers
{
    public static class RoundRobin
    {
        public static (List<Match> createdMatches, List<WeeklyMatchup> matchupOutputs) Run(Week week, List<(Team a, Team b)> teamMatchups, Dictionary<int, List<DeckSubmission>> submissionsByTeamId)
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

        public static List<(Team a, Team b)> GetRoundRobinTeamMatchupsForWeek(IReadOnlyList<Team> teams, int weekNumber)
        {
            // Deterministic ordering so reruns yield same team matchups.
            var ordered = teams
                .OrderBy(t => t.Id)
                .ToList();

            // Round-robin "circle method". If odd, add a BYE.
            var bye = new Team { Id = -1, Name = "BYE" };
            if (ordered.Count % 2 == 1)
            {
                ordered.Add(bye);
            }

            int n = ordered.Count;
            if (n < 2) return new List<(Team, Team)>();

            int rounds = n - 1;
            int roundIndex = ((weekNumber - 1) % rounds + rounds) % rounds; // safe modulo

            // Start with round 0 arrangement, rotate to requested round.
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
                if (a.Id == bye.Id || b.Id == bye.Id) continue;
                matchups.Add((a, b));
            }

            return matchups;
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

        private static void ShuffleInPlace<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
