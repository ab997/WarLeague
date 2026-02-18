using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Enums;

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

    /// <summary>
    /// Gets all round-robin matchups for completed weeks in the given season (for standings).
    /// </summary>
    public async Task<List<RoundRobinMatchup>> GetBySeasonIdForCompletedWeeksAsync(int seasonId)
    {
        return await _context.RoundRobinMatchups
            .Include(m => m.Week)
            .Where(m => m.Week.SeasonId == seasonId && m.Week.Status == WeekStatus.Completed)
            .OrderBy(m => m.Week.WeekNumber)
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