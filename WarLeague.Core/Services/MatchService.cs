using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Services
{
    public class MatchService
    {
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly MatchRepository _matchRepository;
        private readonly MatchupServiceFactory _matchupServiceFactory;
        private readonly SeasonRepository _seasonRepository;
        public MatchService(WeekRepository weekRepository, TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository, MatchRepository matchRepository, MatchupServiceFactory matchupServiceFactory, SeasonRepository seasonRepository)
        {
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _matchRepository = matchRepository;
            _matchupServiceFactory = matchupServiceFactory;
            _seasonRepository = seasonRepository;
        }





        public async Task<BaseResult> ReportLossAsync(int seasonId, int loserId, string replayUrl)
        {
            if (!IsValidReplayUrl(replayUrl))
            {
                return new BaseResult { Success = false, Message = "Please provide a valid HTTP/HTTPS replay URL." };
            }

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
            Player opponentPlayer = match.Player1Id == loserId ? match.Player2 : match.Player1;

            Team team = (await _teamRepository.GetByPlayerAndSeasonAsync(opponentPlayer.Id, seasonId))!;

            // Loser is the caller, so winner is the opponent.
            match.WinnerId = opponentPlayer.Id;
            match.Status = MatchStatus.Reported;
            match.MatchResultType = MatchResultType.Normal;
            match.ReportedDate = DateTime.UtcNow;
            match.ReplayUrl = replayUrl;
            match.WinnerTeamId = team.Id;

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
            match.WinnerId = null;

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
            if (winnerId == loserId)
            {
                return new BaseResult { Success = false, Message = "Winner and loser must be different players." };
            }

            if (!IsValidReplayUrl(replayUrl))
            {
                return new BaseResult { Success = false, Message = "Please provide a valid HTTP/HTTPS replay URL." };
            }

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

            Team team = (await _teamRepository.GetByPlayerAndSeasonAsync(winnerId, seasonId))!;

            var match = candidateMatches.Single();
            match.WinnerId = winnerId;
            match.Status = MatchStatus.Reported;
            match.MatchResultType = MatchResultType.Normal;
            match.ReportedDate = DateTime.UtcNow;
            match.ReplayUrl = replayUrl;
            match.WinnerTeamId = team.Id;

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

            Team team = (await _teamRepository.GetByPlayerAndSeasonAsync(winnerId, seasonId))!;

            var match = candidateMatches.Single();
            match.WinnerId = winnerId;
            match.Status = MatchStatus.Reported;
            match.MatchResultType = MatchResultType.NoShow;
            match.ReportedDate = DateTime.UtcNow;
            match.WinnerTeamId = team.Id;

            await _matchRepository.UpdateAsync(match);

            return new BaseResult { Success = true, Message = "Match result reported successfully." };
        }

        public async Task<GeneratePairingsResult> GeneratePairingsAsync(int seasonId, Week week, List<Team> teams)
        {
            // Get season to determine which matchup service to use
            var season = await _seasonRepository.GetByIdOrDefault(seasonId);
            if (season == null)
            {
                return new GeneratePairingsResult { Success = false, Message = "Season not found." };
            }

            var matchupService = _matchupServiceFactory.GetMatchupService(season);

            // Resolve the team-vs-team matchups for this week (deterministic round-robin based on WeekNumber).
            List<(Team a, Team b)> teamMatchups = await matchupService.GetTeamMatchups(teams, week.WeekNumber);
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

            var (createdMatches, matchupOutputs) = matchupService.GetIndividualMatchups(week, teamMatchups, submissionsByTeamId);

            var participatingTeamIds = teamMatchups
                .SelectMany(m => new[] { m.a.Id, m.b.Id })
                .ToHashSet();

            var byeTeams = teams
                .Where(t => !participatingTeamIds.Contains(t.Id))
                .ToList();

            if (createdMatches.Count == 0)
            {
                return new GeneratePairingsResult { Success = false, Message = "No pairings generated. Likely missing deck submissions for the teams playing this week." };
            }

            BaseResult saveTeamMatchupsResult = await matchupService.SaveTeamMatchupsAsync(week, teams, teamMatchups);
            if (!saveTeamMatchupsResult.Success)
            {
                return new GeneratePairingsResult { Success = false, Message = saveTeamMatchupsResult.Message };
            }

            await _matchRepository.AddRangeAsync(createdMatches);

            return new GeneratePairingsResult
            {
                Success = true,
                Message = "Pairings generated successfully.",
                Week = week,
                CreatedMatches = createdMatches,
                WeeklyMatchups = matchupOutputs,
                ByeTeams = byeTeams
            };
        }

        private static bool IsValidReplayUrl(string? replayUrl)
        {
            if (string.IsNullOrWhiteSpace(replayUrl)) return false;
            if (!Uri.TryCreate(replayUrl, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
