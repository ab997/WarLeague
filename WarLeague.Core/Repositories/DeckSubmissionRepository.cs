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

    public async Task<DeckSubmission?> GetByPlayerAndWeekAsync(int playerId, int weekId)
    {
        return await _context.DeckSubmissions
            .Include(ds => ds.Player)
            .Include(ds => ds.Week)
            .SingleOrDefaultAsync(ds => ds.PlayerId == playerId && ds.WeekId == weekId);
    }

    public async Task<DeckSubmission?> GetBySeatAndWeekAndTeamAsync(int seatNumber, int weekId, int teamId)
    {
        return await _context.DeckSubmissions
            .Include(ds => ds.Player)
            .SingleOrDefaultAsync(ds => ds.SeatNumber == seatNumber && ds.WeekId == weekId && ds.Player.PlayerSeasonTeams.Any(pst => pst.TeamId == teamId));
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

    public async Task<bool> DeleteByPlayerAndWeekAsync(int playerId, int weekId)
    {
        var existing = await _context.DeckSubmissions
            .SingleOrDefaultAsync(ds => ds.PlayerId == playerId && ds.WeekId == weekId);

        if (existing is null)
        {
            return false;
        }

        _context.DeckSubmissions.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

}
