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





        public async Task<BaseResult> ReportWinAsync(int seasonId, int winnerId, string replayUrl, int? winnerWins = null, int? loserWins = null)
        {
            if (!IsValidReplayUrl(replayUrl))
            {
                return new BaseResult { Success = false, Message = "Please provide a valid HTTP/HTTPS replay URL." };
            }

            Week? week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);

            if (week is null)
            {
                return new BaseResult { Success = false, Message = $"There is no week with status '{WeekStatus.InProgress}' for the active season." };
            }

            var callerMatches = await _matchRepository.GetByPlayerAndWeekAsync(winnerId, week.Id);

            var scheduledMatches = callerMatches
                .Where(m => m.Status == MatchStatus.Scheduled)
                .ToList();

            if (scheduledMatches.Count == 0)
            {
                return new BaseResult { Success = false, Message = "You do not have any scheduled matches that can be reported as a win." };
            }

            if (scheduledMatches.Count > 1)
            {
                var opponents = scheduledMatches
                    .Select(m => m.Player1Id == winnerId ? m.Player2 : m.Player1)
                    .DistinctBy(p => p.Id)
                    .Select(p => $"<@{p.DiscordUserId}>")
                    .ToList();

                return new BaseResult { Success = false, Message = "You have multiple scheduled matches pending; I can't determine which one you are reporting a win for.\n" +
                    "Pending opponents: " + string.Join(", ", opponents) };
            }

            var match = scheduledMatches.Single();
            Player opponentPlayer = match.Player1Id == winnerId ? match.Player2 : match.Player1;

            Team team = (await _teamRepository.GetByPlayerAndSeasonAsync(winnerId, seasonId))!;

            match.WinnerId = winnerId;
            match.Status = MatchStatus.Reported;
            match.MatchResultType = MatchResultType.Normal;
            match.ReportedDate = DateTime.UtcNow;
            match.ReplayUrl = replayUrl;
            match.WinnerTeamId = team.Id;

            if (match.Player1Id == winnerId)
            {
                match.Player1Wins = winnerWins;
                match.Player2Wins = loserWins;
            }
            else
            {
                match.Player1Wins = loserWins;
                match.Player2Wins = winnerWins;
            }

            await _matchRepository.UpdateAsync(match);

            return new BaseResult { Success = true, Message = "Match win reported successfully." };
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
            Week? week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);

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
            match.WinnerTeamId = null;
            match.Status = MatchStatus.Scheduled;
            match.ReportedDate = null;
            match.ReplayUrl = null;
            match.MatchResultType = null;
            match.Player1Wins = null;
            match.Player2Wins = null;

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
        /// <param name="player1Wins">Optional wins for Player1.</param>
        /// <param name="player2Wins">Optional wins for Player2.</param>
        /// <returns>Result indicating success or error message.</returns>
        public async Task<BaseResult> ReportResultAsync(int seasonId, int winnerId, int loserId, string replayUrl, int? player1Wins = null, int? player2Wins = null)
        {
            if (winnerId == loserId)
            {
                return new BaseResult { Success = false, Message = "Winner and loser must be different players." };
            }

            if (!IsValidReplayUrl(replayUrl))
            {
                return new BaseResult { Success = false, Message = "Please provide a valid HTTP/HTTPS replay URL." };
            }

            Week? week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);

            if (week is null)
            {
                return new BaseResult { Success = false, Message = $"There is no week with status '{WeekStatus.InProgress}' for the active season." };
            }

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
            match.Player1Wins = player1Wins;
            match.Player2Wins = player2Wins;

            await _matchRepository.UpdateAsync(match);

            return new BaseResult { Success = true, Message = "Match result reported successfully." };
        }

       

        public async Task<BaseResult> NoShowAsync(int seasonId, int winnerId, int loserId)
        {
            Week? week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);

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

        /// <summary>
        /// Ensures team-vs-team matchups exist for the week: uses existing if present, otherwise computes and saves them.
        /// Called when a week is opened so that generate-pairings only creates individual matchups.
        /// </summary>
        public async Task<BaseResult> EnsureTeamMatchupsForWeekAsync(int seasonId, Week week, List<Team> teams)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            var matchupService = _matchupServiceFactory.GetMatchupService(season);

            List<(Team a, Team b)>? existingMatchups = await matchupService.GetExistingTeamMatchupsAsync(week.Id);
            if (existingMatchups != null && existingMatchups.Count > 0)
            {
                return new BaseResult(true, "Team pairings already exist for this week.");
            }

            List<(Team a, Team b)> teamMatchups = await matchupService.GetTeamMatchupsAsync(week.Id);
            if (teamMatchups.Count == 0)
            {
                return new BaseResult(false, "No team pairings were generate, this is probably a bug");
            }

            return await matchupService.SaveTeamMatchupsAsync(week.Id, teamMatchups);
        }

        public async Task<GeneratePairingsResult> GeneratePairingsAsync(int seasonId, Week week, List<Team> teams)
        {
            // Get season to determine which matchup service to use
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            var matchupService = _matchupServiceFactory.GetMatchupService(season);

            // Team matchups are created when the week is opened (or by generate-round-robin-schedule). We only use existing ones here.
            List<(Team a, Team b)>? existingMatchups = await matchupService.GetExistingTeamMatchupsAsync(week.Id);
            if (existingMatchups == null || existingMatchups.Count == 0)
            {
                return new GeneratePairingsResult { Success = false, Message = "No team pairings for this week. Open the week first to generate team pairings." };
            }

            // Safety: don't generate duplicates if matches already exist for this week.
            var existingMatches = await _matchRepository.GetByWeekIdAsync(week.Id);
            if (existingMatches.Count > 0)
            {
                return new GeneratePairingsResult { Success = false, Message = $"Matches already exist for week {week.WeekNumber}. Refusing to generate new pairings to avoid duplicates." };
            }

            var (createdMatches, matchupOutputs) = await matchupService.GetIndividualMatchupsAsync(week.Id);

            var byeTeams = await matchupService.GetByeTeamsForPairingsDisplayAsync(week.Id);

            if (createdMatches.Count == 0)
            {
                return new GeneratePairingsResult { Success = false, Message = "No pairings generated. Likely missing deck submissions for the teams playing this week." };
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
