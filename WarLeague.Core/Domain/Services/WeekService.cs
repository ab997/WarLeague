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
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        public WeekService(WeekRepository weekRepository, TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository)
        {
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
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
        public async Task<Result> StartWeekAsync(int seasonId, int requiredDecksByTeams)
        {
            Week? openWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.Open);

            if (openWeek is null)
            {
                return new Result { Success = false, Message = "No open week found to start." };
            }

            var teams = await _teamRepository.GetBySeasonAsync(seasonId);

            if (teams.Count < 2)
            {
                return new Result { Success = false, Message = "Not enough teams to start the week." };
            }

            var psts = await _playerSeasonTeamRepository.GetBySeasonAsync(seasonId);

            var invalidTeams = new List<string>();

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

                if (submittedCount != requiredDecksByTeams)
                {
                    invalidTeams.Add($"{team.Name} ({submittedCount}/{requiredDecksByTeams})");
                }
            }

            if (invalidTeams.Count > 0)
            {
                return new Result
                {
                    Success = false,
                    Message = $"Cannot start week because not all teams have exactly {requiredDecksByTeams} submitted decks:\n" +
                    string.Join("\n", invalidTeams)
                };
            }

            openWeek.Status = WeekStatus.SubmissionsClosed;
            await _weekRepository.UpdateAsync(openWeek);

            return new Result { Success = true, Message = "Week started successfully. Submission are now closed." };
        }
    }
}
