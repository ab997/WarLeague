using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services;

public class TeamService
{
    private readonly TeamRepository _teamRepository;
    private readonly PlayerRepository _playerRepository;
    private bool _captainActionsEnabled = true; // Can be controlled by admin

    public TeamService(TeamRepository teamRepository, PlayerRepository playerRepository)
    {
        _teamRepository = teamRepository;
        _playerRepository = playerRepository;
    }

    public void SetCaptainActionsEnabled(bool enabled)
    {
        _captainActionsEnabled = enabled;
    }

    public Task<bool> IsCaptainActionsEnabledAsync()
    {
        return Task.FromResult(_captainActionsEnabled);
    }

    public async Task<Team> AddPlayerToTeamAsync(int teamId, int playerId, int captainId)
    {
        if (!await IsCaptainActionsEnabledAsync())
        {
            throw new InvalidOperationException("Captain actions are currently disabled by an administrator.");
        }

        if (!await CanCaptainModifyTeamAsync(captainId, teamId))
        {
            throw new UnauthorizedAccessException("Only the team captain can modify the team roster.");
        }

        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null)
        {
            throw new ArgumentException($"Team with ID {teamId} not found.");
        }

        var player = await _playerRepository.GetByIdAsync(playerId);
        if (player == null)
        {
            throw new ArgumentException($"Player with ID {playerId} not found.");
        }

        if (player.TeamId != null)
        {
            throw new InvalidOperationException($"Player {player.DiscordUserId} is already on a team.");
        }

        player.TeamId = teamId;
        await _playerRepository.UpdateAsync(player);

        // Refresh team to get updated roster
        team = await _teamRepository.GetByIdAsync(teamId);
        return team!;
    }

    public async Task RemovePlayerFromTeamAsync(int teamId, int playerId, int captainId)
    {
        if (!await IsCaptainActionsEnabledAsync())
        {
            throw new InvalidOperationException("Captain actions are currently disabled by an administrator.");
        }

        if (!await CanCaptainModifyTeamAsync(captainId, teamId))
        {
            throw new UnauthorizedAccessException("Only the team captain can modify the team roster.");
        }

        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null)
        {
            throw new ArgumentException($"Team with ID {teamId} not found.");
        }

        var player = await _playerRepository.GetByIdAsync(playerId);
        if (player == null)
        {
            throw new ArgumentException($"Player with ID {playerId} not found.");
        }

        if (player.TeamId != teamId)
        {
            throw new InvalidOperationException($"Player {player.DiscordUserId} is not on this team.");
        }

        if (player.Id == team.CaptainId)
        {
            throw new InvalidOperationException("Cannot remove the team captain from the team.");
        }

        player.TeamId = null;
        await _playerRepository.UpdateAsync(player);
    }

    public async Task<bool> CanCaptainModifyTeamAsync(int captainId, int teamId)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null)
        {
            return false;
        }

        return team.CaptainId == captainId;
    }
}
