using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using WarLeague.Data;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Core.Services;
using WarLeague.Discord.Enums;
using WarLeague.Discord.Helpers;
using WarLeague.Discord.Model;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using static WarLeague.Discord.Helpers.ResultHelper;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Repositories;

namespace WarLeague.Discord.Commands;

[Group("team", "Team management commands")]
[RequireAppPermission(PermissionType.Admin, Group = "Permission")]
[RequireAppPermission(PermissionType.Captain, Group = "Permission")]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
[InitializeGuildContext]
public class TeamCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TeamService _teamService;
    private readonly WarLeagueDbContext _context;
    private readonly DiscordPlayerService _playerService;
    private readonly DiscordApiHelperService _helperService;
    private readonly TeamRepository _teamRepository;
    private readonly DiscordRoleService _roleService;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    private readonly PermissionRepository _permissionRepository;
    public TeamCommands(DiscordPlayerService playerService, DiscordApiHelperService helperService, TeamRepository teamRepository, WarLeagueDbContext dbContext, TeamService teamService, DiscordRoleService roleService, PlayerSeasonTeamRepository playerSeasonTeamRepository, PermissionRepository permissionRepository)
    {
        _playerService = playerService;
        _helperService = helperService;
        _teamRepository = teamRepository;
        _context = dbContext;
        _teamService = teamService;
        _roleService = roleService;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _permissionRepository = permissionRepository;
    }


    [SlashCommand("create", "Creates team with you as captain")]
    public async Task CreateAsync(
        [Summary("team-name", "Name of the team")] string teamName)
    {
        await DeferAsync(ephemeral: false);

        Player player = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        bool canBypassTeamModificationCheck = _helperService.IsUserAdmin(Context);

        BaseResult result = await _teamService.CreateAsync(season.Id, teamName, player.Id, canBypassTeamModificationCheck);

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
       
        BaseResult assignRoleResult = await _teamService.AssignDiscordRoleIdAsync(season.Id, teamName, roleResult.Role!.Id, canBypassTeamModificationCheck);

        await FollowupAsync(Stringify(result, roleResult, assignRoleResult));
    }

    

    [SlashCommand("admin-create", "Creates a team and assigns the specified user as captain (Admin only)")]
    [RequireAppPermission(PermissionType.Admin)]
    public async Task AdminCreateAsync(
       [Summary("team-name", "Name of the team")] string teamName,
       [Summary("captain", "User to set as captain")] IUser captain)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player captainPlayer = await _playerService.EnsurePlayerExistsAsync(captain);

        BaseResult result = await _teamService.CreateAsync(season.Id, teamName, captainPlayer.Id, true);

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
       
        BaseResult assignRoleResult = await _teamService.AssignDiscordRoleIdAsync(season.Id, teamName, roleResult.Role!.Id, true);

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

        BaseResult result = await _teamService.DeleteAsync(season.Id, teamName);

        if (!result.Success)
        {
            await FollowupAsync(Stringify(result));
            return;
        }

        bool roleDeleted = await _roleService.DeleteTeamRoleAsync(Context.Guild, team);

        if (!roleDeleted && team.DiscordRoleId.HasValue)
        {
            await FollowupAsync(Stringify(result, "Warning: Failed to delete Discord role."));
            return;
        }

        if (roleDeleted)
        {
            await FollowupAsync(Stringify(result, "Discord role deleted."));
            return;
        }

        await FollowupAsync(Stringify(result));
    }

    [SlashCommand("add-member", "Adds a member to your team (captain only)")]
    public async Task AddMemberAsync(
       [Summary("member", "User to add")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player caller = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);
        bool canBypassTeamModificationCheck = _helperService.IsUserAdmin(Context);

        BaseResult result = await _teamService.CaptainAddMemberAsync(season.Id, caller.Id, targetPlayer.Id, canBypassTeamModificationCheck);

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
        Player caller = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);
        bool canBypassTeamModificationCheck = _helperService.IsUserAdmin(Context);

        BaseResult result = await _teamService.CaptainRemoveMemberAsync(season.Id, caller.Id, targetPlayer.Id, canBypassTeamModificationCheck);

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

        bool roleRemoved = await _roleService.RemoveTeamRoleFromPlayerAsync(Context.Guild, targetPlayer, team);
        if (!roleRemoved)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to remove Discord role from player."));
            return;
        }

        await FollowupAsync(Stringify(result.Message, "Discord role removed from player."));
    }

    [SlashCommand("admin-add-member", "Adds a member to any team (Admin only)")]
    [RequireAppPermission(PermissionType.Admin)]
    public async Task AdminAddMemberAsync(
      [Summary("team-name", "Name of the team")] string teamName,
      [Summary("member", "User to add")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        BaseResult result = await _teamService.AddMemberAsync(season.Id, targetPlayer.Id, teamName, true);

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
    [RequireAppPermission(PermissionType.Admin)]
    public async Task AdminDropMemberAsync(
      [Summary("member", "User to remove")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        PlayerSeasonTeam? playerSeasonTeam = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(targetPlayer.Id, season.Id);

        Team? team = playerSeasonTeam?.Team;
        ulong? discordRoleId = team?.DiscordRoleId;

        BaseResult result = await _teamService.RemoveMemberAsync(season.Id, targetPlayer.Id, true);

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

        bool roleRemoved = await _roleService.RemoveTeamRoleFromPlayerAsync(Context.Guild, targetPlayer, team);
        if (!roleRemoved)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to remove Discord role from player."));
            return;
        }

        await FollowupAsync(Stringify(result.Message, "Discord role removed from player."));
    }

    [SlashCommand("admin-transfer-member", "Transfers a member to another team (Admin only)")]
    [RequireAppPermission(PermissionType.Admin)]
    public async Task AdminTransferMemberAsync(
    [Summary("member", "User to transfer")] IUser user,
    [Summary("team-name", "Target team name")] string teamName)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Player player = await _playerService.EnsurePlayerExistsAsync(user);

        PlayerSeasonTeam? oldPlayerSeasonTeam = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(player.Id, season.Id);

        Team? oldTeam = oldPlayerSeasonTeam?.Team;
        ulong? oldDiscordRoleId = oldTeam?.DiscordRoleId;

        BaseResult result = await _teamService.TransferMemberAsync(season.Id, player.Id, teamName, true);

        if (!result.Success)
        {
            await FollowupAsync(Stringify(result));
            return;
        }

        Team? newTeam = await _teamRepository.GetByNameAndSeasonAsync(teamName, season.Id);

        if (newTeam?.DiscordRoleId.HasValue != true)
        {
            await FollowupAsync(Stringify(result, $"Target team '{teamName}' was not found or does not have a Discord role assigned."));
            return;
        }

        SocketRole? newRole = Context.Guild.GetRole(newTeam.DiscordRoleId!.Value);
        if (newRole == null)
        {
            await FollowupAsync(Stringify(result.Message, $"Target team '{teamName}' does not have a Discord role assigned."));
            return;
        }

        bool oldRoleRemoved = true;
        if (oldTeam != null && oldDiscordRoleId.HasValue)
        {
            oldRoleRemoved = await _roleService.RemoveTeamRoleFromPlayerAsync(Context.Guild, player, oldTeam);
        }

        bool newRoleAssigned = await _roleService.AssignRoleToPlayerAsync(Context.Guild, player, newRole);

        if (!oldRoleRemoved && !newRoleAssigned)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to remove old Discord role and assign new Discord role."));
            return;
        }

        if (!oldRoleRemoved)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to remove old Discord role, but new role was assigned."));
            return;
        }

        if (!newRoleAssigned)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to assign new Discord role."));
            return;
        }

        await FollowupAsync(Stringify(result.Message, "Discord roles updated."));
    }

    [SlashCommand("admin-transfer-captainship", "Transfers captainship to another team member (Admin only)")]
    [RequireAppPermission(PermissionType.Admin)]
    public async Task AdminTransferCaptainshipAsync(
    [Summary("team-name", "Name of the team")] string teamName,
    [Summary("new-captain", "User to become captain")] IUser newCaptain)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, season.Id);

        if (team == null)
        {
            await FollowupAsync(Stringify($"Team with name '{teamName}' not found."));
            return;
        }

        Player oldCaptainPlayer = team.Captain;
        Player newCaptainPlayer = await _playerService.EnsurePlayerExistsAsync(newCaptain);

        BaseResult result = await _teamService.TransferCaptainshipAsync(season.Id, newCaptainPlayer.Id, teamName, true);

        if (!result.Success)
        {
            await FollowupAsync(Stringify(result));
            return;
        }

        ulong? captainRoleId = (await _permissionRepository.GetRoleIdsAsync(Context.Guild.Id, PermissionType.Captain)).FirstOrDefault();
        SocketRole? captainRole = Context.Guild.Roles.FirstOrDefault(r => r.Id == captainRoleId);
        if (captainRole == null)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Captain role not found in guild."));
            return;
        }

        bool oldRoleRemoved = await _roleService.RemoveRoleFromPlayerAsync(Context.Guild, oldCaptainPlayer, captainRole);
        bool newRoleAssigned = await _roleService.AssignRoleToPlayerAsync(Context.Guild, newCaptainPlayer, captainRole);

        if (!oldRoleRemoved && !newRoleAssigned)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to remove old Captain role and assign new Captain role."));
            return;
        }

        if (!oldRoleRemoved)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to remove old Captain role, but new role was assigned."));
            return;
        }

        if (!newRoleAssigned)
        {
            await FollowupAsync(Stringify(result.Message, "Warning: Failed to assign new Captain role."));
            return;
        }

        await FollowupAsync(Stringify(result.Message, "Captain role updated."));
    }

    [SlashCommand("update-color", "Updates your team's Discord role color (captain only)")]
    [RequireAppPermission(PermissionType.Captain)]
    public async Task UpdateColorAsync(
        [Summary("color", "Color to set for your team")] TeamColor color)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player caller = await _playerService.EnsurePlayerExistsAsync(Context.User);

        Team? team = await _teamRepository.GetByCaptainAndSeasonAsync(caller.Id, season.Id);

        if (team == null)
        {
            await FollowupAsync("You are not the captain of any team in this season.");
            return;
        }

        if (!team.DiscordRoleId.HasValue)
        {
            await FollowupAsync($"Team '{team.Name}' does not have a Discord role assigned.");
            return;
        }

        Color discordColor = color.ToDiscordColor();
        bool colorUpdated = await _roleService.ChangeRoleColorAsync(Context.Guild, team, discordColor);

        if (!colorUpdated)
        {
            await FollowupAsync("Failed to update team color. The Discord role might not exist.");
            return;
        }

        await FollowupAsync($"Team color updated to {color}.");
    }

    [SlashCommand("update-color-hex", "Updates your team's Discord role color using a hex code (captain only)")]
    [RequireAppPermission(PermissionType.Captain)]
    public async Task UpdateColorHexAsync(
        [Summary("hex-code", "Hex color code (e.g., #FF5733 or FF5733)")] string hexCode)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player caller = await _playerService.EnsurePlayerExistsAsync(Context.User);

        Team? team = await _teamRepository.GetByCaptainAndSeasonAsync(caller.Id, season.Id);

        if (team == null)
        {
            await FollowupAsync("You are not the captain of any team in this season.");
            return;
        }

        if (!team.DiscordRoleId.HasValue)
        {
            await FollowupAsync($"Team '{team.Name}' does not have a Discord role assigned.");
            return;
        }

        Color? discordColor = TryParseHexColor(hexCode);

        if (!discordColor.HasValue)
        {
            await FollowupAsync("Invalid hex code. Please provide a valid 6-character hex code (e.g., #FF5733 or FF5733).");
            return;
        }

        bool colorUpdated = await _roleService.ChangeRoleColorAsync(Context.Guild, team, discordColor.Value);

        if (!colorUpdated)
        {
            await FollowupAsync("Failed to update team color. The Discord role might not exist.");
            return;
        }

        await FollowupAsync($"Team color updated to {hexCode}.");
    }

    private static Color? TryParseHexColor(string hexCode)
    {
        string hexInput = hexCode.StartsWith('#') ? hexCode[1..] : hexCode;

        if (hexInput.Length != 6)
        {
            return null;
        }

        if (!uint.TryParse(hexInput, System.Globalization.NumberStyles.HexNumber, null, out uint hexValue))
        {
            return null;
        }

        byte r = (byte)((hexValue >> 16) & 0xFF);
        byte g = (byte)((hexValue >> 8) & 0xFF);
        byte b = (byte)(hexValue & 0xFF);

        return new Color(r, g, b);
    }
}
