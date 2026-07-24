using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Entities;

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

    public async Task<Team?> GetByNameAndSeasonAsync(string teamName, int seasonId)
    {
        return await _context.Teams
            .Include(x => x.Captain)
            .Where(x => x.SeasonId == seasonId)
            .SingleOrDefaultAsync(t => t.Name == teamName);
    }

    public async Task<Team?> GetByIdAndSeasonAsync(int teamId, int seasonId)
    {
        return await _context.Teams
            .Include(x => x.Captain)
            .Where(x => x.SeasonId == seasonId)
            .SingleOrDefaultAsync(t => t.Id == teamId);
    }

    public async Task<Team?> GetByPlayerAndSeasonAsync(int playerId, int seasonId)
    {
        return (await _context.PlayerSeasonTeams
            .Include(x => x.Team)
            .SingleOrDefaultAsync(pst => pst.PlayerId == playerId && pst.SeasonId == seasonId))
            ?.Team;
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
        _context.PlayerSeasonTeams.RemoveRange(psts);

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
            .Include(x => x.Conference)
            .Where(t => t.SeasonId == id)
            .ToListAsync();
    }

    /// <summary>
    /// Teams for a season, optionally filtered by name prefix (case-insensitive), for autocomplete.
    /// </summary>
    public async Task<List<Team>> GetBySeasonAndNamePrefixAsync(int seasonId, string? prefix, int limit)
    {
        var query = _context.Teams.Where(t => t.SeasonId == seasonId);
        if (!string.IsNullOrWhiteSpace(prefix))
            query = query.Where(t => t.Name.StartsWith(prefix));
        return await query.OrderBy(t => t.Name).Take(limit).ToListAsync();
    }
}
