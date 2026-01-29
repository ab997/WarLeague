using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;

namespace WarLeague.Core.Repositories;

public class MatchRepository
{
    private readonly WarLeagueDbContext _context;

    public MatchRepository(WarLeagueDbContext context)
    {
        _context = context;
    }


    public async Task<List<Match>> GetByWeekIdAsync(int weekId)
    {
        return await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.WeekId == weekId)
            .ToListAsync();
    }

    public async Task UpdateAsync(Match match)
    {
        _context.Matches.Update(match);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Match> matches)
    {
        await _context.Matches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Match>> GetByPlayerAndWeekAsync(int playerId, int weekId)
    {
        return await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => (m.Player1Id == playerId || m.Player2Id == playerId) && m.WeekId == weekId)
            .ToListAsync();
    }
}
