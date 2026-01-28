using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;


namespace WarLeague.Core.Repositories;

public class WeekRepository
{
    private readonly WarLeagueDbContext _context;

    public WeekRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<Week?> GetByIdAsync(int id)
    {
        return await _context.Weeks
            .Include(w => w.Matches)
            .Include(w => w.DeckSubmissions)
            .SingleOrDefaultAsync(w => w.Id == id);
    }

    public async Task<Week?> GetCurrentWeekAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Weeks
            .Include(w => w.Matches)
            .Include(w => w.DeckSubmissions)
            .Where(w => w.StartDate <= now && w.EndDate >= now)
            .OrderByDescending(w => w.StartDate)
            .SingleOrDefaultAsync();
    }

    public async Task<Week?> GetByWeekNumberAsync(int weekNumber, int seasonId)
    {
        return await _context.Weeks
            .Include(w => w.Matches)
            .Include(w => w.DeckSubmissions)
            .SingleOrDefaultAsync(w => w.WeekNumber == weekNumber && w.SeasonId == seasonId);
    }

    public async Task<Week?> GetByWeekNumberAsync(int weekNumber)
    {
        return await _context.Weeks
            .SingleOrDefaultAsync(w => w.WeekNumber == weekNumber);
    }

    public async Task<List<Week>> GetAllAsync()
    {
        return await _context.Weeks
            .Include(w => w.Matches)
            .Include(w => w.DeckSubmissions)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync();
    }

    public async Task<List<Week>> GetBySeasonAsync(int seasonId)
    {
        return await _context.Weeks
            .Where(w => w.SeasonId == seasonId)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync();
    }

    public async Task<Week> AddAsync(Week week)
    {
        await _context.Weeks.AddAsync(week);
        await _context.SaveChangesAsync();
        return week;
    }

    public async Task UpdateAsync(Week week)
    {
        _context.Weeks.Update(week);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Ensures there is at most one Open week per season by closing any other Open weeks.
    /// This does not change the status of the specified weekId.
    /// </summary>
    public async Task CloseOtherOpenWeeksAsync(int seasonId, int weekId)
    {
        var otherOpenWeeks = await _context.Weeks
            .Where(w => w.SeasonId == seasonId && w.Id != weekId && w.Status == WeekStatus.Open)
            .ToListAsync();

        if (otherOpenWeeks.Count == 0) return;

        foreach (var w in otherOpenWeeks)
        {
            // The simplest, safest automatic transition for a previously-open week:
            // closing submissions prevents multiple simultaneous "open" weeks.
            w.Status = WeekStatus.SubmissionsClosed;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<Week?> GetByWeekNumberAndSeasonAsync(int weekNumber, int id)
    {
        return await _context.Weeks
            .Where(w => w.SeasonId == id)
            .SingleOrDefaultAsync(w => w.WeekNumber == weekNumber);
    }

    /// <summary>
    /// Returns the single open week for the given season.
    /// Returns null if there is no open week.
    /// Throws if multiple open weeks exist (data inconsistency).
    /// </summary>
    public async Task<Week?> GetOpenWeekBySeasonAsync(int seasonId)
    {
        var openWeeks = await _context.Weeks
            .Include(w => w.DeckSubmissions)
            .Where(w => w.SeasonId == seasonId && w.Status == WeekStatus.Open)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync();

        if (openWeeks.Count == 0) return null;
        if (openWeeks.Count == 1) return openWeeks[0];

        throw new InvalidOperationException("Multiple open weeks exist for this season.");
    }

    /// <summary>
    /// Returns the single week in SubmissionsClosed status for the given season.
    /// Returns null if there is no such week.
    /// Throws if multiple such weeks exist (data inconsistency).
    /// </summary>
    public async Task<Week?> GetSingleSubmissionsClosedWeekBySeasonAsync(int seasonId)
    {
        var weeks = await _context.Weeks
            .Include(w => w.DeckSubmissions)
            .Where(w => w.SeasonId == seasonId && w.Status == WeekStatus.SubmissionsClosed)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync();

        if (weeks.Count == 0) return null;
        if (weeks.Count == 1) return weeks[0];

        throw new InvalidOperationException("Multiple weeks with status SubmissionsClosed exist for this season.");
    }
}
