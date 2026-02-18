using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Repositories;

public class ConferenceRepository
{
    private readonly WarLeagueDbContext _context;

    public ConferenceRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<Conference?> GetByNameAndSeasonAsync(string name, int seasonId)
    {
        return await _context.Conferences
            .SingleOrDefaultAsync(c => c.SeasonId == seasonId && c.Name == name);
    }

    public async Task<List<Conference>> GetBySeasonAsync(int seasonId)
    {
        return await _context.Conferences
            .Where(c => c.SeasonId == seasonId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task AddAsync(Conference conference)
    {
        _context.Conferences.Add(conference);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Conference conference)
    {
        _context.Conferences.Update(conference);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Conference conference)
    {
        _context.Conferences.Remove(conference);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasTeamsAsync(int conferenceId)
    {
        return await _context.Teams.AnyAsync(t => t.ConferenceId == conferenceId);
    }
}
