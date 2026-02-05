using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services
{
    public class WeekService
    {
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly MatchRepository _matchRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        public WeekService(WeekRepository weekRepository, TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository, MatchRepository matchRepository)
        {
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _matchRepository = matchRepository;
        }

        public async Task<Week?> CreateAsync(int seasonId, int weekNumber, DateTime startDate, DateTime endDate, DateTime? subCloseDate)
        {
            Week? week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);

            if (week != null)
            {
                return null;
            }

            Week weekNew = new Week
            {
                WeekNumber = weekNumber,
                SeasonId = seasonId,
                StartDate = startDate,
                EndDate = endDate,
                SubmissionsClosedDate = subCloseDate,
                Status = WeekStatus.NotOpenYet,
            };

            await _weekRepository.AddAsync(weekNew);

            return weekNew;
        }

        public async Task<Week?> UpdateAsync(int seasonId, int weekNumber, DateTime? startDate, DateTime? endDate, DateTime? subCloseDate, WeekStatus? weekStatus)
        {
            Week? week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);

            if (week is null)
            {
                return null;
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

            await _weekRepository.UpdateAsync(week);

            return week;
        }
        public async Task<BaseResult> CloseSubmissionsAsync(int seasonId)
        {
            Week? openWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.Open);

            if (openWeek is null)
            {
                return new BaseResult { Success = false, Message = "No open week found to start." };
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

        public async Task<BaseResult> CloseAsync(int seasonId)
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

        public async Task<Week?> DeleteAsync(int seasonId, int weekNumber)
        {
            Week? week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);

            if (week is null)
            {
                return null;
            }

            await _weekRepository.DeleteAsync(week);

            return week;
        }
    }
}
