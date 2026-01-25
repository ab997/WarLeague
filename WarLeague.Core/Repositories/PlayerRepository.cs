using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;

namespace WarLeague.Core.Repositories;

public class PlayerRepository
{
    private readonly WarLeagueDbContext _context;

    public PlayerRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<Player?> GetByIdAsync(int id)
    {
        return await _context.Players
            .Include(p => p.Team)
            .SingleOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Player?> GetByDiscordUserIdAsync(ulong discordUserId)
    {
        return await _context.Players
            .Include(p => p.Team)
            .SingleOrDefaultAsync(p => p.DiscordUserId == discordUserId);
    }

    public async Task<List<Player>> GetByTeamIdAsync(int teamId)
    {
        return await _context.Players
            .Where(p => p.TeamId == teamId)
            .ToListAsync();
    }

    public async Task<Player> AddAsync(Player player)
    {
        await _context.Players.AddAsync(player);
        await _context.SaveChangesAsync();
        return player;
    }

    public async Task UpdateAsync(Player player)
    {
        _context.Players.Update(player);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Players.AnyAsync(p => p.Id == id);
    }

    public async Task<bool> IsPlayerInTeamAsync(int playerId)
    {
        return await _context.Players
            .AnyAsync(p => p.Id == playerId && p.TeamId != null);
    }
}
