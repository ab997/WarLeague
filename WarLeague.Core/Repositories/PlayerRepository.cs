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

    
}
