using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Data.Entities;

namespace WarLeague.Core.Repositories;

public class TeamStandingsRepository
{
    private readonly WarLeagueDbContext _context;

    public TeamStandingsRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets team standings for the season in bracket order: best first by Tiebreaker (wins + tiebreaker rules),
    /// then Seed for consistency. Single-elimination is built from this order (1st = index 0, etc.).
    /// </summary>
    public async Task<List<TeamStandings>> GetBySeasonIdAsync(int seasonId)
    {
        return await _context.TeamStandings
            .Include(ts => ts.Team)
            .Where(ts => ts.SeasonId == seasonId)
            .OrderByDescending(ts => ts.Tiebreaker)
            .ThenBy(ts => ts.Seed)
            .ToListAsync();
    }

    /// <summary>
    /// Same as GetBySeasonIdAsync but without Team include (used when building bracket from standings).
    /// </summary>
    public async Task<List<TeamStandings>> GetBySeasonIdWithoutTeamAsync(int seasonId)
    {
        return await _context.TeamStandings
            .Where(ts => ts.SeasonId == seasonId)
            .OrderByDescending(ts => ts.Tiebreaker)
            .ThenBy(ts => ts.Seed)
            .ToListAsync();
    }

    public async Task<TeamStandings?> GetBySeasonIdAndTeamIdAsync(int seasonId, int teamId)
    {
        return await _context.TeamStandings
            .FirstOrDefaultAsync(ts => ts.SeasonId == seasonId && ts.TeamId == teamId);
    }

    public async Task AddRangeAsync(IEnumerable<TeamStandings> standings)
    {
        await _context.TeamStandings.AddRangeAsync(standings);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TeamStandings standing)
    {
        _context.TeamStandings.Update(standing);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<TeamStandings> standings)
    {
        _context.TeamStandings.UpdateRange(standings);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteBySeasonIdAsync(int seasonId)
    {
        var toDelete = await _context.TeamStandings
            .Where(ts => ts.SeasonId == seasonId)
            .ToListAsync();
        _context.TeamStandings.RemoveRange(toDelete);
        await _context.SaveChangesAsync();
    }
}
