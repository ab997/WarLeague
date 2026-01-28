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

    public async Task<Week?> GetSingleWeekBySeasonAndStatusAsync(int seasonId, WeekStatus status)
    {
        var weeks = await _context.Weeks
            .Include(w => w.DeckSubmissions)
            .Where(w => w.SeasonId == seasonId && w.Status == status)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync();

        if (weeks.Count == 0) return null;
        if (weeks.Count == 1) return weeks[0];

        throw new InvalidOperationException($"Multiple weeks with status {status} exist for this season.");
    }
}
