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

    public async Task AddAsync(Team team)
    {
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();
    }

    public async Task<Team?> GetByNameAsync(string teamName)
    {
        return await _context.Teams
            .SingleOrDefaultAsync(t => t.Name == teamName);
    }

    public async Task<Team?> GetByNameAndSeasonAsync(string teamName, int seasonId)
    {
        return await _context.Teams
            .Include(x => x.Captain)
            .Where(x => x.SeasonId == seasonId)
            .SingleOrDefaultAsync(t => t.Name == teamName);
    }

    public async Task<Team?> GetByCaptainAndSeasonAsync(int captainId, int seasonId)
    {
        return await _context.Teams
            .SingleOrDefaultAsync(t => t.CaptainId == captainId && t.SeasonId == seasonId);
    }

    public async Task DeleteAsync(Team team)
    {
        // Remove dependent PlayerSeasonTeam entries first (cascade delete disabled)
        var psts = _context.PlayerSeasonTeams.Where(p => p.TeamId == team.Id);
        //_context.PlayerSeasonTeams.RemoveRange(psts);

        // Remove the team
        _context.Teams.Remove(team);

        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Team team)
    {
        _context.Teams.Update(team);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Team>> GetBySeasonAsync(int id)
    {
        return await _context.Teams
            .Where(t => t.SeasonId == id)
            .ToListAsync();
    }
}
