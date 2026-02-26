using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;


namespace WarLeague.Core.Repositories;

public class WeekRepository
{
    private readonly WarLeagueDbContext _context;

    public WeekRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<List<Week>> GetBySeasonAsync(int seasonId)
    {
        return await _context.Weeks
            .Where(w => w.SeasonId == seasonId)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync();
    }

    public async Task<Week?> GetByIdAsync(int weekId)
    {
        return await _context.Weeks
             .Include(w => w.DeckSubmissions)
            .SingleOrDefaultAsync(w => w.Id == weekId);
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

    public async Task<Week?> GetByWeekNumberAndSeasonAsync(int weekNumber, int id)
    {
        return await _context.Weeks
            .Where(w => w.SeasonId == id)
            .SingleOrDefaultAsync(w => w.WeekNumber == weekNumber);
    }

    public async Task<Week?> GetByWeekNumberAndSeasonWithSubmissionsAsync(int weekNumber, int seasonId)
    {
        return await _context.Weeks
            .Include(w => w.DeckSubmissions)
            .Where(w => w.SeasonId == seasonId && w.WeekNumber == weekNumber)
            .SingleOrDefaultAsync();
    }

    public async Task<Week?> GetSingleWeekBySeasonAndStatusOrDefaultAsync(int seasonId, WeekStatus status)
    {
        return await _context.Weeks
            .Include(w => w.DeckSubmissions)
            .Where(w => w.SeasonId == seasonId && w.Status == status)
            .SingleOrDefaultAsync();
    }

    public async Task DeleteAsync(Week week)
    {
        _context.Weeks.Remove(week);
        await _context.SaveChangesAsync();
    }
}
