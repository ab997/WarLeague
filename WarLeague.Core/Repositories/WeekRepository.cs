using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;


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

    public async Task<List<Week>> GetAllAsync()
    {
        return await _context.Weeks
            .Include(w => w.Matches)
            .Include(w => w.DeckSubmissions)
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
}
