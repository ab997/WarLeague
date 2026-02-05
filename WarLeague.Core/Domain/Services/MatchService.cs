using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Helpers;
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
            var teamMatchups = RoundRobin.GetRoundRobinTeamMatchupsForWeek(teams, week.WeekNumber);
            if (teamMatchups.Count == 0)
            {
                return new GeneratePairingsResult { Success = false, Message = "No team matchups available for this week (did everyone get a bye?)." };
            }

            // Build quick lookup: player -> team for this season.
            var memberships = await _playerSeasonTeamRepository.GetBySeasonAsync(seasonId);
            var membershipByPlayerId = memberships
                .GroupBy(m => m.PlayerId)
                .ToDictionary(g => g.Key, g => g.First());

            // Group deck submissions by team, preserving SeatNumber.
            var submissionsByTeamId = week.DeckSubmissions
                .Where(ds => membershipByPlayerId.ContainsKey(ds.PlayerId))
                .GroupBy(ds => membershipByPlayerId[ds.PlayerId].TeamId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Safety: don't generate duplicates if matches already exist for this week.
            var existingMatches = await _matchRepository.GetByWeekIdAsync(week.Id);
            if (existingMatches.Count > 0)
            {
                return new GeneratePairingsResult { Success = false, Message = $"Matches already exist for week {week.WeekNumber}. Refusing to generate new pairings to avoid duplicates." };
            }

            var (createdMatches, matchupOutputs) = RoundRobin.Run(week, teamMatchups, submissionsByTeamId);

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
                true,
                "Pairings generated successfully.",
                week,
                createdMatches,
                matchupOutputs
                );
        }

        

        public async Task<BaseResult> ReportLossAsync(int seasonId, int loserId, string replayUrl)
        {
            Week? week;
            try
            {
                week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);
            }
            catch (InvalidOperationException)
            {
                return new BaseResult { Success = false, Message = $"Multiple weeks with status '{WeekStatus.InProgress}' exist for the active season." };
            }

            // Only allow reporting for matches where the caller actually has a scheduled match.
            var callerMatches = await _matchRepository.GetByPlayerAndWeekAsync(loserId, week!.Id);

            var scheduledMatches = callerMatches
                .Where(m => m.Status == MatchStatus.Scheduled)
                .ToList();

            if (scheduledMatches.Count == 0)
            {
                return new BaseResult { Success = false, Message = "You do not have any scheduled matches that can be reported as a loss." };
            }

            if (scheduledMatches.Count > 1)
            {
                // Ambiguous which opponent this loss is against; require admins to resolve.
                var opponents = scheduledMatches
                    .Select(m => m.Player1Id == loserId ? m.Player2 : m.Player1)
                    .DistinctBy(p => p.Id)
                    .Select(p => $"<@{p.DiscordUserId}>")
                    .ToList();

                return new BaseResult { Success = false, Message = "You have multiple scheduled matches pending; I can't determine which one you are reporting a loss for.\n" +
                    "Pending opponents: " + string.Join(", ", opponents) };
            }

            var match = scheduledMatches.Single();
            var opponentPlayer = match.Player1Id == loserId ? match.Player2 : match.Player1;

            // Loser is the caller, so winner is the opponent.
            match.WinnerId = opponentPlayer.Id;
            match.Status = MatchStatus.Reported;
            match.MatchResultType = MatchResultType.Normal;
            match.ReportedDate = DateTime.UtcNow;
            match.ReplayUrl = replayUrl;

            await _matchRepository.UpdateAsync(match);

            return new BaseResult { Success = true, Message = "Match loss reported successfully." };
        }

      

        /// <summary>
        /// Undoes a previously reported match result between two specified players for the current in-progress week.
        /// Admin-only operation.
        /// </summary>
        /// <param name="seasonId">Season identifier.</param>
        /// <param name="player1Id">First player's id.</param>
        /// <param name="player2Id">Second player's id.</param>
        /// <returns>Result indicating success or error message.</returns>
        public async Task<BaseResult> UndoResultAsync(int seasonId, int player1Id, int player2Id)
        {
            Week? week;
            try
            {
                week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);
            }
            catch (InvalidOperationException)
            {
                return new BaseResult { Success = false, Message = $"Multiple weeks with status '{WeekStatus.InProgress}' exist for the active season." };
            }

            if (week is null)
            {
                return new BaseResult { Success = false, Message = $"There is no week with status '{WeekStatus.InProgress}' for the active season." };
            }

            // Find reported match between the two players for this week.
            var matches = await _matchRepository.GetByWeekIdAsync(week.Id);
            var candidateMatches = matches
                .Where(m => m.Status == MatchStatus.Reported &&
                            ((m.Player1Id == player1Id && m.Player2Id == player2Id) ||
                             (m.Player1Id == player2Id && m.Player2Id == player1Id)))
                .ToList();

            if (candidateMatches.Count == 0)
            {
                return new BaseResult { Success = false, Message = "No reported match found between the specified players for the current week." };
            }

            if (candidateMatches.Count > 1)
            {
                return new BaseResult { Success = false, Message = "Multiple reported matches found between these players; unable to determine which one to undo." };
            }

            var match = candidateMatches.Single();

            // Reset match back to scheduled state.
            match.WinnerId = null;
            match.Status = MatchStatus.Scheduled;
            match.ReportedDate = null;
            match.ReplayUrl = null;

            await _matchRepository.UpdateAsync(match);

            return new BaseResult { Success = true, Message = "Reported match has been undone and returned to scheduled state." };
        }

        /// <summary>
        /// Reports a result for a scheduled match between two players. Admin-only operation.
        /// </summary>
        /// <param name="seasonId">Season identifier.</param>
        /// <param name="winnerId">Winner player's id.</param>
        /// <param name="loserId">Loser player's id.</param>
        /// <param name="replayUrl">Replay URL to attach.</param>
        /// <returns>Result indicating success or error message.</returns>
        public async Task<BaseResult> ReportResultAsync(int seasonId, int winnerId, int loserId, string replayUrl)
        {
            Week? week;
            try
            {
                week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);
            }
            catch (InvalidOperationException)
            {
                return new BaseResult { Success = false, Message = $"Multiple weeks with status '{WeekStatus.InProgress}' exist for the active season." };
            }

            if (week is null)
            {
                return new BaseResult { Success = false, Message = $"There is no week with status '{WeekStatus.InProgress}' for the active season." };
            }

            // Find the scheduled match between the specified winner and loser for this week.
            List<Match> candidateMatches = await _matchRepository.GetScheduledMatchesAsync(winnerId, loserId, week);

            if (candidateMatches.Count == 0)
            {
                return new BaseResult { Success = false, Message = "No scheduled match found between the specified players for the current week." };
            }

            if (candidateMatches.Count > 1)
            {
                return new BaseResult { Success = false, Message = "Multiple scheduled matches found between these players; unable to determine which one to report." };
            }

            var match = candidateMatches.Single();
            match.WinnerId = winnerId;
            match.Status = MatchStatus.Reported;
            match.MatchResultType = MatchResultType.Normal;
            match.ReportedDate = DateTime.UtcNow;
            match.ReplayUrl = replayUrl;

            await _matchRepository.UpdateAsync(match);

            return new BaseResult { Success = true, Message = "Match result reported successfully." };
        }

       

        public async Task<BaseResult> NoShowAsync(int seasonId, int winnerId, int loserId)
        {
            Week? week;
            try
            {
                week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);
            }
            catch (InvalidOperationException)
            {
                return new BaseResult { Success = false, Message = $"Multiple weeks with status '{WeekStatus.InProgress}' exist for the active season." };
            }

            if (week is null)
            {
                return new BaseResult { Success = false, Message = $"There is no week with status '{WeekStatus.InProgress}' for the active season." };
            }

            // Find the scheduled match between the specified winner and loser for this week.
            List<Match> candidateMatches = await _matchRepository.GetScheduledMatchesAsync(winnerId, loserId, week);

            if (candidateMatches.Count == 0)
            {
                return new BaseResult { Success = false, Message = "No scheduled match found between the specified players for the current week." };
            }

            if (candidateMatches.Count > 1)
            {
                return new BaseResult { Success = false, Message = "Multiple scheduled matches found between these players; unable to determine which one to report." };
            }

            var match = candidateMatches.Single();
            match.WinnerId = winnerId;
            match.Status = MatchStatus.Reported;
            match.MatchResultType = MatchResultType.NoShow;
            match.ReportedDate = DateTime.UtcNow;

            await _matchRepository.UpdateAsync(match);

            return new BaseResult { Success = true, Message = "Match result reported successfully." };
        }

        
    }
}
