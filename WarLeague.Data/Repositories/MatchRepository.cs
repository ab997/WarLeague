using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Repositories;

public class MatchRepository
{
    private readonly WarLeagueDbContext _context;

    public MatchRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all matches for completed weeks in the given season (for tiebreaker series/game aggregates).
    /// </summary>
    public async Task<List<Match>> GetBySeasonIdForCompletedWeeksAsync(int seasonId)
    {
        return await _context.Matches
            .Include(m => m.Week)
            .Where(m => m.Week.SeasonId == seasonId && m.Week.Status == WeekStatus.Completed)
            .OrderBy(m => m.Week.WeekNumber)
            .ToListAsync();
    }
    public async Task<List<Match>> GetBySeasonAndTeamIdAsync(int seasonId, int teamId)
    {
        return await _context.Matches
            .Include(m => m.Week)
            .Include(x => x.Team1)
            .Include(x => x.Team2)
            .Where(m => m.Week.SeasonId == seasonId && (m.Team1Id == teamId || m.Team2Id == teamId))
            .OrderBy(m => m.Week.WeekNumber)
            .ToListAsync();
    }

    public async Task<List<Match>> GetByWeekIdAsync(int weekId)
    {
        return await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Team1)
                .ThenInclude(c => c.Conference)
            .Include(m => m.Team2)
                .ThenInclude(c => c.Conference)
            .Include(m => m.Winner)
            .Where(m => m.WeekId == weekId)
            .ToListAsync();
    }

    public async Task UpdateAsync(Match match)
    {
        _context.Matches.Update(match);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Match> matches)
    {
        await _context.Matches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Match>> GetByPlayerAndWeekAsync(int playerId, int weekId)
    {
        return await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => (m.Player1Id == playerId || m.Player2Id == playerId) && m.WeekId == weekId)
            .ToListAsync();
    }

    public async Task<List<Match>> GetScheduledMatchesAsync(int winnerId, int loserId, Week week)
    {
        var matches = await GetByWeekIdAsync(week.Id);
        var candidateMatches = matches
            .Where(m => m.Status == MatchStatus.Scheduled &&
                        ((m.Player1Id == winnerId && m.Player2Id == loserId) ||
                         (m.Player1Id == loserId && m.Player2Id == winnerId)))
            .ToList();
        return candidateMatches;
    }

    /// <summary>
    /// Returns true if any match references the given team (Team1Id, Team2Id, or WinnerTeamId).
    /// </summary>
    public async Task<bool> AnyMatchReferencesTeamAsync(int teamId)
    {
        return await _context.Matches
            .AnyAsync(m => m.Team1Id == teamId || m.Team2Id == teamId || m.WinnerTeamId == teamId);
    }
}
