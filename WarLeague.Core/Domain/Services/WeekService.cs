using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services;

public class WeekService
{
    private readonly WeekRepository _weekRepository;

    public WeekService(WeekRepository weekRepository)
    {
        _weekRepository = weekRepository;
    }

    public async Task<Week> StartWeekAsync(int weekNumber, int seasonId, DateTime startDate, DateTime endDate)
    {
        // Check if week already exists
        var existingWeek = await _weekRepository.GetByWeekNumberAsync(weekNumber, seasonId);
        if (existingWeek != null)
        {
            throw new InvalidOperationException($"Week {weekNumber} for season {seasonId} already exists.");
        }

        var week = new Week
        {
            WeekNumber = weekNumber,
            SeasonId = seasonId,
            StartDate = startDate,
            EndDate = endDate,
            Status = WeekStatus.Open
        };

        return await _weekRepository.AddAsync(week);
    }

    public async Task<Week> CloseSubmissionsAsync(int weekId)
    {
        var week = await _weekRepository.GetByIdAsync(weekId);
        if (week == null)
        {
            throw new ArgumentException($"Week with ID {weekId} not found.");
        }

        if (week.Status != WeekStatus.Open)
        {
            throw new InvalidOperationException($"Week is not in Open status. Current status: {week.Status}");
        }

        week.Status = WeekStatus.SubmissionsClosed;
        week.SubmissionsClosedDate = DateTime.UtcNow;

        await _weekRepository.UpdateAsync(week);
        return week;
    }

    public async Task<Week> CompleteWeekAsync(int weekId)
    {
        var week = await _weekRepository.GetByIdAsync(weekId);
        if (week == null)
        {
            throw new ArgumentException($"Week with ID {weekId} not found.");
        }

        if (week.Status != WeekStatus.SubmissionsClosed)
        {
            throw new InvalidOperationException($"Week must be in SubmissionsClosed status before completing. Current status: {week.Status}");
        }

        week.Status = WeekStatus.Completed;

        await _weekRepository.UpdateAsync(week);
        return week;
    }

    public async Task<Week?> GetCurrentWeekAsync()
    {
        return await _weekRepository.GetCurrentWeekAsync();
    }
}
