using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;

namespace WarLeague.Core.Repositories;

public class TeamRepository
{
    private readonly WarLeagueDbContext _context;

    public TeamRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<Team?> GetByIdAsync(int id)
    {
        return await _context.Teams
            .Include(t => t.Captain)
            .Include(t => t.Players)
            .SingleOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Team?> GetByCaptainIdAsync(int captainId)
    {
        return await _context.Teams
            .Include(t => t.Captain)
            .Include(t => t.Players)
            .SingleOrDefaultAsync(t => t.CaptainId == captainId);
    }

    public async Task<List<Team>> GetAllActiveAsync()
    {
        return await _context.Teams
            .Include(t => t.Captain)
            .Include(t => t.Players)
            .Where(t => t.IsActive)
            .ToListAsync();
    }

    public async Task<Team> AddAsync(Team team)
    {
        await _context.Teams.AddAsync(team);
        await _context.SaveChangesAsync();
        return team;
    }

    public async Task UpdateAsync(Team team)
    {
        _context.Teams.Update(team);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Teams.AnyAsync(t => t.Id == id);
    }
}
