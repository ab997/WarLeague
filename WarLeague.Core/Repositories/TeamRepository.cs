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

   
}
