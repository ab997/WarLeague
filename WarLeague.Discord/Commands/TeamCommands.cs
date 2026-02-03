using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Numerics;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Model;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using static WarLeague.Discord.Helpers.ResultHelper;

namespace WarLeague.Discord.Commands;

[Group("team", "Team management commands")]
[RequireRole("Admin", Group = "Permission")]
[RequireRole("Captain", Group = "Permission")]
[EnsureSingleActiveSeason]
[EnsureChannelIsInFormatCategory]
public class TeamCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TeamService _teamService;
    private readonly WarLeagueDbContext _context;
    private readonly DiscordPlayerService _playerService;
    private readonly DiscordApiHelperService _helperService;
    private readonly TeamRepository _teamRepository;
    private readonly DiscordRoleService _roleService;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    public TeamCommands(DiscordPlayerService playerService, DiscordApiHelperService helperService, TeamRepository teamRepository, WarLeagueDbContext dbContext, TeamService teamService, DiscordRoleService roleService, PlayerSeasonTeamRepository playerSeasonTeamRepository)
    {
        _playerService = playerService;
        _helperService = helperService;
        _teamRepository = teamRepository;
        _context = dbContext;
        _teamService = teamService;
        _roleService = roleService;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
    }


    [SlashCommand("create", "Creates team with you as captain")]
    public async Task CreateAsync(
        [Summary("team-name", "Name of the team")] string teamName)
    {
        await DeferAsync(ephemeral: false);

        Player player = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _teamService.CreateAsync(season.Id, teamName, player.Id);

        if (!result.Success)
        {
            await FollowupAsync(Stringify(result));
            return;
        }

        SocketRoleResult roleResult = await _roleService.CreateAndAssignTeamRoleAsync(Context.Guild, teamName, player);

        if (!roleResult.Success)
        {
            await FollowupAsync(Stringify(result, roleResult));
            return;
        }
       
        BaseResult assignRoleResult = await _teamService.AssignDiscordRoleIdAsync(season.Id, teamName, roleResult.Role!.Id);

        await FollowupAsync(Stringify(result, roleResult, assignRoleResult));
    }

    

    [SlashCommand("admin-create", "Creates a team and assigns the specified user as captain (Admin only)")]
    [RequireRole("Admin")]
    public async Task AdminCreateAsync(
       [Summary("team-name", "Name of the team")] string teamName,
       [Summary("captain", "User to set as captain")] IUser captain)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player captainPlayer = await _playerService.EnsurePlayerExistsAsync(captain);

        BaseResult result = await _teamService.CreateAsync(season.Id, teamName, captainPlayer.Id);

        if (!result.Success)
        {
            await FollowupAsync(Stringify(result));
            return;
        }

        SocketRoleResult roleResult = await _roleService.CreateAndAssignTeamRoleAsync(Context.Guild, teamName, captainPlayer);

        if (!roleResult.Success)
        {
            await FollowupAsync(Stringify(result, roleResult));
            return;
        }
       
        BaseResult assignRoleResult = await _teamService.AssignDiscordRoleIdAsync(season.Id, teamName, roleResult.Role!.Id);

        await FollowupAsync(Stringify(result, roleResult, assignRoleResult));
    }

    [SlashCommand("delete", "Deletes team")]
    public async Task DeleteAsync(
       [Summary("team-name", "Name of the team")] string teamName)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player player = await _playerService.EnsurePlayerExistsAsync(Context.User);
        // Determine if caller has the Admin role in the guild
        bool isAdmin = _helperService.IsUserAdmin(Context);
        Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, season.Id);

        if (team == null)
        {
            await FollowupAsync($"Team with name '{teamName}' not found.");
            return;
        }

        // custom "authentication" or "authorization" has to be made in the discord services layer, never inside core domain layer
        if (!isAdmin && team.CaptainId != player.Id)
        {
            await FollowupAsync("Only the team captain or an Admin can delete this team.");
            return;
        }

        Team? deleted = await _teamService.DeleteAsync(season.Id, teamName);

        if (deleted is null) 
        {
            await FollowupAsync($"Team with name '{teamName}' not found.");
            return;
        }

        bool roleDeleted = await _roleService.DeleteTeamRoleAsync(Context.Guild, deleted);

        string message = $"Team '{teamName}' deleted.";
        if (!roleDeleted && deleted.DiscordRoleId.HasValue)
        {
            message += "\nWarning: Failed to delete Discord role.";
        }
        else if (roleDeleted)
        {
            message += "\nDiscord role deleted.";
        }

        await FollowupAsync(message);
    }

    [SlashCommand("add-member", "Adds a member to your team (captain only)")]
    public async Task AddMemberAsync(
       [Summary("member", "User to add")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        if (season.DisableTeamModification && !_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Team modifications are currently disabled for this season.");
            return;
        }

        Player caller = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        BaseResult result = await _teamService.CaptainAddMemberAsync(season.Id, caller.Id, targetPlayer.Id);

        if (!result.Success)
        {
            await FollowupAsync(Stringify(result));
            return;
        }

        Team? team = await _teamRepository.GetByCaptainAndSeasonAsync(caller.Id, season.Id);

        if (team?.DiscordRoleId.HasValue != true)
        {
            await FollowupAsync(Stringify(result, $"Team '{team?.Name}' was not found or does not have a Discord role assigned."));
            return;
        }

        SocketRole? role = Context.Guild.GetRole(team.DiscordRoleId!.Value);
        if (role == null)
        {
            await FollowupAsync(Stringify(result.Message, $"Team '{team?.Name}' does not have a Discord role assigned."));
            return;
        }

        bool roleAssigned = await _roleService.AssignRoleToPlayerAsync(Context.Guild, targetPlayer, role);
        if (!roleAssigned)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to assign Discord role."));
            return;
        }

        await FollowupAsync(Stringify(result.Message, "Discord role assigned."));
    }

    [SlashCommand("drop-member", "Removes a member from your team (captain only)")]
    public async Task DropMemberAsync(
       [Summary("member", "User to remove")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        if (season.DisableTeamModification && !_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Team modifications are currently disabled for this season.");
            return;
        }

        Player caller = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        BaseResult result = await _teamService.CaptainRemoveMemberAsync(season.Id, caller.Id, targetPlayer.Id);

        if (!result.Success)
        {
            await FollowupAsync(Stringify(result));
            return;
        }

        Team? team = await _teamRepository.GetByCaptainAndSeasonAsync(caller.Id, season.Id);

        if (team?.DiscordRoleId.HasValue != true)
        {
            await FollowupAsync(Stringify(result, $"Team '{team?.Name}' was not found or does not have a Discord role assigned."));
            return;
        }

        bool roleRemoved = await _roleService.RemoveRoleFromPlayerAsync(Context.Guild, targetPlayer, team);
        if (!roleRemoved)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to remove Discord role."));
            return;
        }

        await FollowupAsync(Stringify(result.Message, "Discord role removed."));
    }

    [SlashCommand("admin-add-member", "Adds a member to any team (Admin only)")]
    [RequireRole("Admin")]
    public async Task AdminAddMemberAsync(
      [Summary("team-name", "Name of the team")] string teamName,
      [Summary("member", "User to add")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        BaseResult result = await _teamService.AddMemberAsync(season.Id, targetPlayer.Id, teamName);

        if (!result.Success)
        {
            await FollowupAsync(Stringify(result));
            return;
        }

        Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, season.Id);

        if (team?.DiscordRoleId.HasValue != true)
        {
            await FollowupAsync(Stringify(result, $"Team '{teamName}' was not found or does not have a Discord role assigned."));
            return;
        }

        SocketRole? role = Context.Guild.GetRole(team.DiscordRoleId!.Value);
        if (role == null)
        {
            await FollowupAsync(Stringify(result.Message, $"Team '{teamName}' does not have a Discord role assigned."));
            return;
        }

        bool roleAssigned = await _roleService.AssignRoleToPlayerAsync(Context.Guild, targetPlayer, role);
        if (!roleAssigned)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to assign Discord role."));
            return;
        }

        await FollowupAsync(Stringify(result.Message, "Discord role assigned."));
    }
    [SlashCommand("admin-drop-member", "Removes a member from any team (Admin only)")]
    public async Task AdminDropMemberAsync(
      [Summary("member", "User to remove")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        if (!_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Only Admins can use this command.");
            return;
        }

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        PlayerSeasonTeam? playerSeasonTeam = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(targetPlayer.Id, season.Id);

        Team? team = playerSeasonTeam?.Team;
        ulong? discordRoleId = team?.DiscordRoleId;

        BaseResult result = await _teamService.RemoveMemberAsync(season.Id, targetPlayer.Id);

        if (!result.Success)
        {
            await FollowupAsync(Stringify(result));
            return;
        }

        if (team == null || !discordRoleId.HasValue)
        {
            await FollowupAsync(Stringify(result, $"Player was not on a team or team does not have a Discord role assigned."));
            return;
        }

        bool roleRemoved = await _roleService.RemoveRoleFromPlayerAsync(Context.Guild, targetPlayer, team);
        if (!roleRemoved)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to remove Discord role."));
            return;
        }

        await FollowupAsync(Stringify(result.Message, "Discord role removed."));
    }

    [SlashCommand("admin-transfer-member", "Transfers a member to another team (Admin only)")]
    [RequireRole("Admin")]
    public async Task AdminTransferMemberAsync(
    [Summary("member", "User to transfer")] IUser user,
    [Summary("team-name", "Target team name")] string teamName)
    {
        await DeferAsync(ephemeral: false);

        using var transaction = await _context.Database.BeginTransactionAsync();

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Player player = await _playerService.EnsurePlayerExistsAsync(user);

        BaseResult result = await _teamService.TransferMemberAsync(season.Id, player.Id, teamName);

        await FollowupAsync(result.Message);
    }

    [SlashCommand("admin-transfer-captainship", "Transfers captainship to another team member (Admin only)")]
    [RequireRole("Admin")]
    public async Task AdminTransferCaptainshipAsync(
    [Summary("team-name", "Name of the team")] string teamName,
    [Summary("new-captain", "User to become captain")] IUser newCaptain)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player newCaptainPlayer = await _playerService.EnsurePlayerExistsAsync(newCaptain);

        BaseResult result = await _teamService.TransferCaptainshipAsync(season.Id, newCaptainPlayer.Id, teamName);

        await FollowupAsync(result.Message);
    }
}
