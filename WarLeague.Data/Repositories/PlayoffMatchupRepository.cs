using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Data.Entities;

namespace WarLeague.Core.Repositories;

public class PlayoffMatchupRepository
{
    private readonly WarLeagueDbContext _context;

    public PlayoffMatchupRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlayoffMatchup>> GetByWeekIdAsync(int weekId)
    {
        return await _context.PlayoffMatchups
            .Where(m => m.WeekId == weekId)
            .ToListAsync();
    }

    public async Task<List<PlayoffMatchup>> GetBySeasonIdAsync(int seasonId)
    {
        return await _context.PlayoffMatchups
            .Include(m => m.Week)
            .Where(m => m.Week.SeasonId == seasonId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<PlayoffMatchup> matchups)
    {
        await _context.PlayoffMatchups.AddRangeAsync(matchups);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<PlayoffMatchup> matchups)
    {
        _context.PlayoffMatchups.UpdateRange(matchups);
        await _context.SaveChangesAsync();
    }
}
