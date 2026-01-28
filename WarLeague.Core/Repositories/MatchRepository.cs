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

    public async Task<Match?> GetByIdAsync(int id)
    {
        return await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Include(m => m.Week)
            .SingleOrDefaultAsync(m => m.Id == id);
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

    public async Task<List<Match>> GetByPlayerIdAsync(int playerId)
    {
        return await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
            .ToListAsync();
    }

    public async Task<Match> AddAsync(Match match)
    {
        await _context.Matches.AddAsync(match);
        await _context.SaveChangesAsync();
        return match;
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
