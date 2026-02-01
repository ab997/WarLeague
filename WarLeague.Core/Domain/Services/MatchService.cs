using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services
{
    public class MatchService
    {
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly MatchRepository _matchRepository;
        private readonly WarLeagueDbContext _context;
        public MatchService(WeekRepository weekRepository, TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository, MatchRepository matchRepository, WarLeagueDbContext context)
        {
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _matchRepository = matchRepository;
            _context = context;
        }

        public async Task<GeneratePairingsResult> GeneratePairingsAsync(int seasonId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Find the single week in SubmissionsClosed state for this season.
            Week? week;
            try
            {
                week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.SubmissionsClosed);
            }
            catch (InvalidOperationException)
            {
                return new GeneratePairingsResult { Success = false, Message = "Multiple weeks with status 'SubmissionsClosed' exist for the active season." };
            }

            if (week is null)
            {
                return new GeneratePairingsResult { Success = false, Message = "There is no week with status 'SubmissionsClosed' for the active season." };
            }

            var teams = await _teamRepository.GetBySeasonAsync(seasonId);
            if (teams.Count < 2)
            {
                return new GeneratePairingsResult { Success = false, Message = "Need at least 2 teams to generate pairings." };
            }

            // Resolve the team-vs-team matchups for this week (deterministic round-robin based on WeekNumber).
            var teamMatchups = GetRoundRobinTeamMatchupsForWeek(teams, week.WeekNumber);
            if (teamMatchups.Count == 0)
            {
                return new GeneratePairingsResult { Success = false, Message = "No team matchups available for this week (did everyone get a bye?)." };
            }

            // Build quick lookup: player -> team for this season.
            var memberships = await _playerSeasonTeamRepository.GetBySeasonAsync(seasonId);
            var membershipByPlayerId = memberships
                .GroupBy(m => m.PlayerId)
                .ToDictionary(g => g.Key, g => g.First());

            // Determine which players submitted for this week, then group those by team.
            var submittedPlayerIds = week.DeckSubmissions
                .Select(ds => ds.PlayerId)
                .Distinct()
                .ToHashSet();

            var submittedMemberships = memberships
                .Where(m => submittedPlayerIds.Contains(m.PlayerId))
                .ToList();

            var submittedByTeamId = submittedMemberships
                .GroupBy(m => m.TeamId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Player).ToList());

            // Safety: don't generate duplicates if matches already exist for this week.
            var existingMatches = await _matchRepository.GetByWeekIdAsync(week.Id);
            if (existingMatches.Count > 0)
            {
                return new GeneratePairingsResult { Success = false, Message = $"Matches already exist for week {week.WeekNumber}. Refusing to generate new pairings to avoid duplicates." };
            }

            var rng = new Random();
            var createdMatches = new List<Match>();

            // We will also build output per matchup.
            var matchupOutputs = new List<WeeklyMatchup>();

            foreach (var (teamA, teamB) in teamMatchups)
            {
                var listA = submittedByTeamId.TryGetValue(teamA.Id, out var aPlayers) ? aPlayers.ToList() : new List<Player>();
                var listB = submittedByTeamId.TryGetValue(teamB.Id, out var bPlayers) ? bPlayers.ToList() : new List<Player>();

                ShuffleInPlace(listA, rng);
                ShuffleInPlace(listB, rng);

                int pairCount = Math.Min(listA.Count, listB.Count);
                var pairs = new List<(Player, Player)>(capacity: pairCount);

                for (int i = 0; i < pairCount; i++)
                {
                    var p1 = listA[i];
                    var p2 = listB[i];
                    pairs.Add((p1, p2));

                    createdMatches.Add(new Match
                    {
                        WeekId = week.Id,
                        Player1Id = p1.Id,
                        Player2Id = p2.Id,
                        Status = MatchStatus.Scheduled
                    });
                }

                var unpairedA = listA.Skip(pairCount).ToList();
                var unpairedB = listB.Skip(pairCount).ToList();

                matchupOutputs.Add(new WeeklyMatchup(teamA, teamB, pairs, unpairedA, unpairedB));
            }

            if (createdMatches.Count == 0)
            {
                return new GeneratePairingsResult { Success = false, Message = "No pairings generated. Likely missing deck submissions for the teams playing this week." };
            }

            await _matchRepository.AddRangeAsync(createdMatches);

            // Move week to InProgress now that pairings are generated.
            week.Status = WeekStatus.InProgress;
            await _weekRepository.UpdateAsync(week);

            await transaction.CommitAsync();

            return new GeneratePairingsResult(
                Success: true,
                Message: "Pairings generated successfully.",
                Week: week,
                CreatedMatches: createdMatches,
                WeeklyMatchups: matchupOutputs
                );
        }

        private static List<(Team a, Team b)> GetRoundRobinTeamMatchupsForWeek(IReadOnlyList<Team> teams, int weekNumber)
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

        public async Task<Result> ReportLossAsync(int seasonId, int loserId, string replayUrl)
        {
            Week? week;
            try
            {
                week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);
            }
            catch (InvalidOperationException)
            {
                return new Result { Success = false, Message = $"Multiple weeks with status '{WeekStatus.InProgress}' exist for the active season." };
            }

            // Only allow reporting for matches where the caller actually has a scheduled match.
            var callerMatches = await _matchRepository.GetByPlayerAndWeekAsync(loserId, week!.Id);

            var scheduledMatches = callerMatches
                .Where(m => m.Status == MatchStatus.Scheduled)
                .ToList();

            if (scheduledMatches.Count == 0)
            {
                return new Result { Success = false, Message = "You do not have any scheduled matches that can be reported as a loss." };
            }

            if (scheduledMatches.Count > 1)
            {
                // Ambiguous which opponent this loss is against; require admins to resolve.
                var opponents = scheduledMatches
                    .Select(m => m.Player1Id == loserId ? m.Player2 : m.Player1)
                    .DistinctBy(p => p.Id)
                    .Select(p => $"<@{p.DiscordUserId}>")
                    .ToList();

                return new Result { Success = false, Message = "You have multiple scheduled matches pending; I can't determine which one you are reporting a loss for.\n" +
                    "Pending opponents: " + string.Join(", ", opponents) };
            }

            var match = scheduledMatches.Single();
            var opponentPlayer = match.Player1Id == loserId ? match.Player2 : match.Player1;

            // Loser is the caller, so winner is the opponent.
            match.WinnerId = opponentPlayer.Id;
            match.Status = MatchStatus.Reported;
            match.ReportedDate = DateTime.UtcNow;
            match.ReplayUrl = replayUrl;

            await _matchRepository.UpdateAsync(match);

            return new Result { Success = true, Message = "Match loss reported successfully." };
        }
    }
}
