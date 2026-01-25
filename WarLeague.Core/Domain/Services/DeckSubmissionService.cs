using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services;

public class DeckSubmissionService
{
    private readonly DeckSubmissionRepository _deckSubmissionRepository;
    private readonly WeekRepository _weekRepository;
    private readonly PlayerRepository _playerRepository;
    private readonly TeamRepository _teamRepository;
    private readonly FormatRepository _formatRepository;

    public DeckSubmissionService(
        DeckSubmissionRepository deckSubmissionRepository,
        WeekRepository weekRepository,
        PlayerRepository playerRepository,
        TeamRepository teamRepository,
        FormatRepository formatRepository)
    {
        _deckSubmissionRepository = deckSubmissionRepository;
        _weekRepository = weekRepository;
        _playerRepository = playerRepository;
        _teamRepository = teamRepository;
        _formatRepository = formatRepository;
    }

    public async Task<DeckSubmission> SubmitDeckAsync(int weekId, int playerId, int teamId, int formatId, string deckFileUrl)
    {
        if (!await CanSubmitDeckAsync(weekId))
        {
            throw new InvalidOperationException("Deck submissions are closed for this week.");
        }

        var week = await _weekRepository.GetByIdAsync(weekId);
        if (week == null)
        {
            throw new ArgumentException($"Week with ID {weekId} not found.");
        }

        var player = await _playerRepository.GetByIdAsync(playerId);
        if (player == null)
        {
            throw new ArgumentException($"Player with ID {playerId} not found.");
        }

        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null)
        {
            throw new ArgumentException($"Team with ID {teamId} not found.");
        }

        if (player.TeamId != teamId)
        {
            throw new InvalidOperationException("Player is not a member of the specified team.");
        }

        var format = await _formatRepository.GetByIdAsync(formatId);
        if (format == null)
        {
            throw new ArgumentException($"Format with ID {formatId} not found.");
        }

        // Check if player already submitted for this week
        var existingSubmission = await _deckSubmissionRepository.GetByPlayerAndWeekAsync(playerId, weekId);
        if (existingSubmission != null)
        {
            // Update existing submission
            existingSubmission.DeckFile = deckFileUrl;
            existingSubmission.SubmittedDate = DateTime.UtcNow;
            existingSubmission.IsValidated = false; // TODO: Implement legality check

            await _deckSubmissionRepository.UpdateAsync(existingSubmission);
            return existingSubmission;
        }

        var submission = new DeckSubmission
        {
            WeekId = weekId,
            PlayerId = playerId,
            DeckFile = deckFileUrl,
            SubmittedDate = DateTime.UtcNow,
            IsValidated = false // TODO: Implement legality check
        };

        return await _deckSubmissionRepository.AddAsync(submission);
    }

    public async Task<DeckSubmission?> GetSubmissionAsync(int playerId, int weekId)
    {
        return await _deckSubmissionRepository.GetByPlayerAndWeekAsync(playerId, weekId);
    }

    public async Task<List<DeckSubmission>> GetSubmissionsByWeekAsync(int weekId)
    {
        return await _deckSubmissionRepository.GetByWeekIdAsync(weekId);
    }

    public async Task<List<DeckSubmission>> GetSubmissionsByTeamAndWeekAsync(int teamId, int weekId)
    {
        return await _deckSubmissionRepository.GetByTeamIdAndWeekIdAsync(teamId, weekId);
    }

    public async Task<bool> CanSubmitDeckAsync(int weekId)
    {
        var week = await _weekRepository.GetByIdAsync(weekId);
        if (week == null)
        {
            return false;
        }

        return week.Status == WeekStatus.Open;
    }
}
