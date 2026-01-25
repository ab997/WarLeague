using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;

namespace WarLeague.Core.Repositories;

public class DeckSubmissionRepository
{
    private readonly WarLeagueDbContext _context;

    public DeckSubmissionRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<DeckSubmission?> GetByIdAsync(int id)
    {
        return await _context.DeckSubmissions
            .Include(d => d.Player)
            .Include(d => d.Week)
            .SingleOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DeckSubmission?> GetByPlayerAndWeekAsync(int playerId, int weekId)
    {
        return await _context.DeckSubmissions
            .Include(d => d.Player)
            .SingleOrDefaultAsync(d => d.PlayerId == playerId && d.WeekId == weekId);
    }

    public async Task<List<DeckSubmission>> GetByWeekIdAsync(int weekId)
    {
        return await _context.DeckSubmissions
            .Include(d => d.Player)
            .Where(d => d.WeekId == weekId)
            .ToListAsync();
    }

    public async Task<List<DeckSubmission>> GetByTeamIdAndWeekIdAsync(int teamId, int weekId)
    {
        return await _context.DeckSubmissions
            .Include(d => d.Player)
            .ToListAsync();
    }

    public async Task<DeckSubmission> AddAsync(DeckSubmission submission)
    {
        await _context.DeckSubmissions.AddAsync(submission);
        await _context.SaveChangesAsync();
        return submission;
    }

    public async Task UpdateAsync(DeckSubmission submission)
    {
        _context.DeckSubmissions.Update(submission);
        await _context.SaveChangesAsync();
    }
}
