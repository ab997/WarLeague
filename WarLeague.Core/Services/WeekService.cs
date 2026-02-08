using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Helpers;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Services
{
    public class WeekService
    {
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly MatchRepository _matchRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly SeasonRepository _seasonRepository;
        private readonly MatchService _matchService;
        private readonly WarLeagueDbContext _context;
        public WeekService(WeekRepository weekRepository, TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository, MatchRepository matchRepository, SeasonRepository seasonRepository, WarLeagueDbContext context, MatchService matchService)
        {
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _matchRepository = matchRepository;
            _seasonRepository = seasonRepository;
            _context = context;
            _matchService = matchService;
        }

        public async Task<BaseResult> CreateAsync(int seasonId, int weekNumber, DateTime startDate, DateTime endDate, DateTime? subCloseDate, int submissionsRequired)
        {
            // Validate date ordering
            if (startDate > endDate)
            {
                return new BaseResult(false, "Start date must be before end date.");
            }

            // Validate submissionsRequired against season minimum
            Season? season = await _seasonRepository.GetByIdOrDefault(seasonId);
            if (season is null)
            {
                return new BaseResult(false, "Season not found.");
            }

            if (submissionsRequired > season.MinimumTeamMembers)
            {
                return new BaseResult(false, $"Week cannot have more required submissions ({submissionsRequired}) than the season minimum team members requirement ({season.MinimumTeamMembers}).");
            }

            Week? week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);

            if (week != null)
            {
                return new BaseResult(false, $"Week with number {weekNumber} already exists.");
            }

            Week weekNew = new Week
            {
                WeekNumber = weekNumber,
                SeasonId = seasonId,
                StartDate = startDate,
                EndDate = endDate,
                SubmissionsClosedDate = subCloseDate,
                Status = WeekStatus.NotOpenYet,
                SubmissionsRequired = submissionsRequired
            };

            await _weekRepository.AddAsync(weekNew);

            return new BaseResult(true, "Week created.");
        }

        /// <summary>
        /// week number is needed because there can be many NotOpenYet weeks
        /// </summary>
        /// <param name="seasonId"></param>
        /// <param name="weekNumber"></param>
        public async Task<BaseResult> TransitionToOpenWeekAsync(int seasonId, int weekNumber)
        {
            // Check if another week is already open
            Week? existingOpenWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.Open);
            if (existingOpenWeek is not null && existingOpenWeek.WeekNumber != weekNumber)
            {
                return new BaseResult(false, $"Week {existingOpenWeek.WeekNumber} is already Open. Close or update it before opening another week.");
            }

            // check if this week is in status NotOpenYet, if not return error
            Week? weekToOpen = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);
            if (weekToOpen is null)
            {
                return new BaseResult(false, $"Week with number {weekNumber} does not exist.");
            }

            if (weekToOpen.Status != WeekStatus.NotOpenYet)
            {
                return new BaseResult(false, $"Week with number {weekNumber} is not in a valid state to be opened. Expected status: NotOpenYet, Actual status: {weekToOpen.Status}");
            }

            // Get the season to check team modification status
            Season? season = await _seasonRepository.GetByIdOrDefault(seasonId);
            if (season is null)
            {
                return new BaseResult(false, "Season not found.");
            }

            // Update week status to Open
            BaseResult updateResult = await UpdateAsync(seasonId, weekNumber, null, null, null, WeekStatus.Open, null);
            if (!updateResult.Success)
            {
                return updateResult;
            }

            // Disable team modifications for the season if not already disabled
            string additionalMessage = "\nTeam modifications have already been disabled for the season.";
            if (!season.DisableTeamModification)
            {
                season.DisableTeamModification = true;
                await _seasonRepository.UpdateAsync(season);
                additionalMessage = "\nTeam modifications have been automatically disabled for the season.";
            }

            return new BaseResult(true, $"Week {weekNumber} set to Open.{additionalMessage}");
        }

        public async Task<BaseResult> UpdateAsync(int seasonId, int weekNumber, DateTime? startDate, DateTime? endDate, DateTime? subCloseDate, WeekStatus? weekStatus, int? submissionsRequired)
        {
            Week? week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);

            if (week is null)
            {
                return new BaseResult(false, $"Week with number {weekNumber} does not exist.");
            }

            // Validate date ordering if both are being updated
            DateTime effectiveStartDate = startDate ?? week.StartDate;
            DateTime effectiveEndDate = endDate ?? week.EndDate;
            
            if (effectiveStartDate > effectiveEndDate)
            {
                return new BaseResult(false, "Start date must be before end date.");
            }

            // Validate submissionsRequired against season minimum if being updated
            if (submissionsRequired.HasValue)
            {
                Season? season = await _seasonRepository.GetByIdOrDefault(seasonId);
                if (season is null)
                {
                    return new BaseResult(false, "Season not found.");
                }

                if (submissionsRequired.Value > season.MinimumTeamMembers)
                {
                    return new BaseResult(false, $"Week cannot have more required submissions ({submissionsRequired.Value}) than the season minimum team members requirement ({season.MinimumTeamMembers}).");
                }
            }

            if (startDate.HasValue)
            {
                week.StartDate = startDate.Value;
            }
            if (endDate.HasValue)
            {
                week.EndDate = endDate.Value;
            }
            if (subCloseDate.HasValue)
            {
                week.SubmissionsClosedDate = subCloseDate.Value;
            }
            if (weekStatus.HasValue)
            {
                week.Status = weekStatus.Value;
            }
            if (submissionsRequired.HasValue)
            {
                week.SubmissionsRequired = submissionsRequired.Value;
            }

            await _weekRepository.UpdateAsync(week);

            return new BaseResult(true, $"Week {weekNumber} updated.");
        }
        public async Task<BaseResult> TransitionToCloseSubmissionsAsync(int seasonId)
        {
            Week? openWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.Open);

            if (openWeek is null)
            {
                return new BaseResult { Success = false, Message = "No open week found to start." };
            }

            Week? submissionClosedWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.SubmissionsClosed);

            if (submissionClosedWeek is not null)
            {
                return new BaseResult { Success = false, Message = "There is already a week in SubmissionClosed status" }; 
            }

            var teams = await _teamRepository.GetBySeasonAsync(seasonId);

            if (teams.Count < 2)
            {
                return new BaseResult { Success = false, Message = "Not enough teams to start the week." };
            }

            var psts = await _playerSeasonTeamRepository.GetBySeasonAsync(seasonId);

            var invalidTeams = ValidateTeamDeckSubmissions(openWeek, teams, psts);

            if (invalidTeams.Count > 0)
            {
                return new BaseResult
                {
                    Success = false,
                    Message = $"Cannot start week because not all teams have exactly {openWeek.SubmissionsRequired} submitted decks:\n" +
                    string.Join("\n", invalidTeams)
                };
            }

            openWeek.Status = WeekStatus.SubmissionsClosed;
            await _weekRepository.UpdateAsync(openWeek);

            return new BaseResult { Success = true, Message = "Week started successfully, all submissions are valid. Submission are now closed." };
        }

        private static List<string> ValidateTeamDeckSubmissions(Week openWeek, List<Team> teams, List<PlayerSeasonTeam> psts)
        {
            List<string> invalidTeams = [];
            foreach (var team in teams.OrderBy(t => t.Name))
            {
                var teamPlayerIds = psts
                    .Where(p => p.TeamId == team.Id)
                    .Select(p => p.PlayerId)
                    .Distinct()
                    .ToList();

                var submittedCount = openWeek.DeckSubmissions
                    .Where(ds => teamPlayerIds.Contains(ds.PlayerId))
                    .Select(ds => ds.PlayerId)
                    .Distinct()
                    .Count();

                if (submittedCount != openWeek.SubmissionsRequired)
                {
                    invalidTeams.Add($"{team.Name} ({submittedCount}/{openWeek.SubmissionsRequired})");
                }
            }
            return invalidTeams;
        }

        public async Task<BaseResult> TransitionToCompletedAsync(int seasonId)
        {
            Week? activeWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);

            if (activeWeek is null)
            {
                return new BaseResult { Success = false, Message = "No InProgress week found to close." };
            }

            var matches = await _matchRepository.GetByWeekIdAsync(activeWeek.Id);

            if (matches.Count == 0)
            {
                return new BaseResult { Success = false, Message = "No matches found for the active week." };
            }

            bool allReported = matches.All(m => m.Status == MatchStatus.Reported);

            if (!allReported)
            {
                var pendingCount = matches.Count(m => m.Status != MatchStatus.Reported);
                return new BaseResult { Success = false, Message = $"Cannot close week: {pendingCount} match(es) not reported." };
            }

            activeWeek.Status = WeekStatus.Completed;
            await _weekRepository.UpdateAsync(activeWeek);

            return new BaseResult { Success = true, Message = "Week closed successfully." };
        }

        public async Task<GeneratePairingsResult> TransitionToInProgressAsync(int seasonId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Find the single week in SubmissionsClosed state for this season.
            Week? week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.SubmissionsClosed);

            if (week is null)
            {
                return new GeneratePairingsResult { Success = false, Message = "There is no week with status 'SubmissionsClosed' for the active season." };
            }

            Week? inprogress = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);
            if (inprogress is not null)
            {
                return new GeneratePairingsResult { Success = false, Message = $"Week {inprogress.WeekNumber} is already InProgress." };
            }

            var teams = await _teamRepository.GetBySeasonAsync(seasonId);
            if (teams.Count < 2)
            {
                return new GeneratePairingsResult { Success = false, Message = "Need at least 2 teams to generate pairings." };
            }

            GeneratePairingsResult result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);
            if (!result.Success)
            {
                return result;
            }

            // Move week to InProgress now that pairings are generated.
            week.Status = WeekStatus.InProgress;
            await _weekRepository.UpdateAsync(week);

            await transaction.CommitAsync();

            return result;
        }

        

        public async Task<List<string>> GetPendingMatchPairsAsync(int seasonId)
        {
            // Find active week (prefer InProgress)
            Week? activeWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);

            if (activeWeek is null)
            {
                return new List<string>();
            }

            var matches = await _matchRepository.GetByWeekIdAsync(activeWeek.Id);

            var lines = matches
                .Where(m => m.Status != MatchStatus.Reported)
                .Select(m =>
                {
                    var p1 = m.Player1;
                    var p2 = m.Player2;
                    var m1 = p1 != null ? $"<@{p1.DiscordUserId}>" : "[TBD]";
                    var m2 = p2 != null ? $"<@{p2.DiscordUserId}>" : "[TBD]";
                    return $"{m1} vs {m2}";
                })
                .ToList();

            return lines;
        }

        public async Task<BaseResult> DeleteAsync(int seasonId, int weekNumber)
        {
            Week? week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);

            if (week is null)
            {
                return new BaseResult(false, $"Week with number {weekNumber} does not exists.");
            }

            await _weekRepository.DeleteAsync(week);

            return new BaseResult(true, "Week deleted.");
        }
    }
}
