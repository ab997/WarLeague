using Discord;
using Discord.WebSocket;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Repositories;

namespace WarLeague.Discord.Services;

public class PermissionService
{
    private readonly PlayerRepository _playerRepository;
    private readonly TeamRepository _teamRepository;

    public PermissionService(PlayerRepository playerRepository, TeamRepository teamRepository)
    {
        _playerRepository = playerRepository;
        _teamRepository = teamRepository;
    }

    public async Task<Role> GetUserRoleAsync(ulong discordUserId)
    {
        var player = await _playerRepository.GetByDiscordUserIdAsync(discordUserId);
        if (player == null)
        {
            return Role.Player;
        }

        return Role.Player;
    }

    public async Task<bool> IsAdminAsync(ulong discordUserId)
    {
        var role = await GetUserRoleAsync(discordUserId);
        return role == Role.Admin;
    }

    public async Task<bool> IsTeamCaptainAsync(ulong discordUserId)
    {
        var role = await GetUserRoleAsync(discordUserId);
        return role == Role.TeamCaptain || role == Role.Admin;
    }

    public async Task<int?> GetPlayerIdAsync(ulong discordUserId)
    {
        var player = await _playerRepository.GetByDiscordUserIdAsync(discordUserId);
        return player?.Id;
    }

    public async Task<int?> GetTeamIdForPlayerAsync(ulong discordUserId)
    {
        var player = await _playerRepository.GetByDiscordUserIdAsync(discordUserId);
        return player?.TeamId;
    }
}
