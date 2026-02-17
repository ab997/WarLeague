using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Data.Entities;

namespace WarLeague.Core.Repositories;

public class RoundRobinMatchupRepository
{
    private readonly WarLeagueDbContext _context;

    public RoundRobinMatchupRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoundRobinMatchup>> GetByWeekIdAsync(int weekId)
    {
        return await _context.RoundRobinMatchups
            .Where(m => m.WeekId == weekId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<RoundRobinMatchup> matchups)
    {
        await _context.RoundRobinMatchups.AddRangeAsync(matchups);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<RoundRobinMatchup> matchups)
    {
        _context.RoundRobinMatchups.UpdateRange(matchups);
        await _context.SaveChangesAsync();
    }
}