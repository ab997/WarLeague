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

    public async Task AddAsync(Player player)
    {
        _context.Players.Add(player);
        await _context.SaveChangesAsync();
    }

    public Player? GetByDiscordUserId(ulong userId)
    {
        return _context.Players
            .SingleOrDefault(p => p.DiscordUserId == userId);
    }

    internal async Task<Player> GetByIdAsync(int playerId)
    {
        return await _context.Players
            .SingleAsync(p => p.Id == playerId);
    }
}
