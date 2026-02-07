using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services;

public class SubstitutionService
{
    private readonly TeamRepository _teamRepository;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    private readonly WeekRepository _weekRepository;
    private readonly MatchRepository _matchRepository;
    private readonly DeckSubmissionRepository _deckSubmissionRepository;
    private readonly WarLeagueDbContext _context;

    public SubstitutionService(
        TeamRepository teamRepository,
        PlayerSeasonTeamRepository playerSeasonTeamRepository,
        WeekRepository weekRepository,
        MatchRepository matchRepository,
        DeckSubmissionRepository deckSubmissionRepository,
        WarLeagueDbContext context)
    {
        _teamRepository = teamRepository;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _weekRepository = weekRepository;
        _matchRepository = matchRepository;
        _deckSubmissionRepository = deckSubmissionRepository;
        _context = context;
    }

    public async Task<BaseResult> SubstitutePlayerAsync(
        int seasonId,
        string teamName,
        int playerInId,
        int playerOutId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        // Validate team exists
        var team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
        if (team is null)
        {
            return new BaseResult(false, $"Team '{teamName}' not found for the active season.");
        }

        // Validate playerIn is on the team
        var playerInPst = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(playerInId, seasonId);
        if (playerInPst is null || playerInPst.TeamId != team.Id)
        {
            return new BaseResult(false, $"Player being substituted in is not a member of team '{teamName}'.");
        }

        // Validate playerOut is on the team
        var playerOutPst = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(playerOutId, seasonId);
        if (playerOutPst is null || playerOutPst.TeamId != team.Id)
        {
            return new BaseResult(false, $"Player being substituted out is not a member of team '{teamName}'.");
        }

        // Get current week
        var currentWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);
        if (currentWeek is null)
        {
            return new BaseResult(false, "No InProgress week found for substitution.");
        }

        // Check if playerIn is already playing in a match this week
        var playerInMatches = await _matchRepository.GetByPlayerAndWeekAsync(playerInId, currentWeek.Id);
        if (playerInMatches.Count > 0)
        {
            return new BaseResult(false, $"{playerInPst.Player.UserName} is already scheduled to play in a match this week.");
        }

        // Check if playerOut is playing in any scheduled match this week
        var playerOutMatches = await _matchRepository.GetByPlayerAndWeekAsync(playerOutId, currentWeek.Id);
        var scheduledMatch = playerOutMatches.SingleOrDefault(m => m.Status == MatchStatus.Scheduled);
        if (scheduledMatch is null)
        {
            return new BaseResult(false, $"{playerOutPst.Player.UserName} is not currently scheduled to play in any unreported match this week.");
        }

        // Update the match
        if (scheduledMatch.Player1Id == playerOutId)
        {
            scheduledMatch.Player1Id = playerInId;
        }
        else if (scheduledMatch.Player2Id == playerOutId)
        {
            scheduledMatch.Player2Id = playerInId;
        }

        await _matchRepository.UpdateAsync(scheduledMatch);

        // Update the deck submission if it exists
        var deckSubmission = await _deckSubmissionRepository.GetByPlayerAndWeekAsync(playerOutId, currentWeek.Id);
        if (deckSubmission is null)
        {
            return new BaseResult(
                false,
                $"Substitution successful in matches, but no deck submission found for {playerOutPst.Player.UserName} for week {currentWeek.WeekNumber} to update.");
        }

        deckSubmission.PlayerId = playerInId;
        await _deckSubmissionRepository.UpdateAsync(deckSubmission);

        await transaction.CommitAsync();

        return new BaseResult(
            true,
            $"Substitution successful: {playerInPst.Player.UserName} will now play instead of {playerOutPst.Player.UserName} for week {currentWeek.WeekNumber}.");
    }
}
