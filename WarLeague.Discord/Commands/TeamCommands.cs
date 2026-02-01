using Discord;
using Discord.Interactions;
using System.Numerics;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

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
    public TeamCommands(DiscordPlayerService playerService, DiscordApiHelperService helperService, TeamRepository teamRepository, WarLeagueDbContext dbContext, TeamService teamService)
    {
        _playerService = playerService;
        _helperService = helperService;
        _teamRepository = teamRepository;
        _context = dbContext;
        _teamService = teamService;
    }


    [SlashCommand("create", "Creates team with you as captain")]
    public async Task CreateAsync(
        [Summary("team-name", "Name of the team")] string teamName)
    {
        await DeferAsync(ephemeral: false);

        Player player = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Result result = await _teamService.CreateAsync(season.Id, teamName, player.Id);

        await FollowupAsync(result.Message);
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

        Result result = await _teamService.CreateAsync(season.Id, teamName, captainPlayer.Id);

        await FollowupAsync(result.Message);
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

        await FollowupAsync($"Team '{teamName}' deleted.");
    }

    [SlashCommand("add-member", "Adds a member to your team (captain only)")]
    public async Task AddMemberAsync(
       [Summary("member", "User to add")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        // Block captain modifications if disabled for the season (Admins bypass)
        if (season.DisableTeamModification && !_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Team modifications are currently disabled for this season.");
            return;
        }

        // Ensure caller is a player
        Player caller = await _playerService.EnsurePlayerExistsAsync(Context.User);

        // Ensure target player exists
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        Result result = await _teamService.CaptainAddMemberAsync(season.Id, caller.Id, targetPlayer.Id);

        await FollowupAsync(result.Message);
    }

    [SlashCommand("drop-member", "Removes a member from your team (captain only)")]
    public async Task DropMemberAsync(
       [Summary("member", "User to remove")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        // Block captain modifications if disabled for the season (Admins bypass)
        if (season.DisableTeamModification && !_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Team modifications are currently disabled for this season.");
            return;
        }

        // Ensure caller is a player
        Player caller = await _playerService.EnsurePlayerExistsAsync(Context.User);

        // Ensure target player exists
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        Result result = await _teamService.CaptainRemoveMemberAsync(season.Id, caller.Id, targetPlayer.Id);

        await FollowupAsync(result.Message);
    }

    [SlashCommand("admin-add-member", "Adds a member to any team (Admin only)")]
    [RequireRole("Admin")]
    public async Task AdminAddMemberAsync(
      [Summary("team-name", "Name of the team")] string teamName,
      [Summary("member", "User to add")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        // Ensure target player exists
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        Result result = await _teamService.AddMemberAsync(season.Id, targetPlayer.Id, teamName);

        await FollowupAsync(result.Message);
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

        // Ensure target player exists
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        Result result = await _teamService.RemoveMemberAsync(season.Id, targetPlayer.Id);

        await FollowupAsync(result.Message);
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

        Result result = await _teamService.TransferMemberAsync(season.Id, player.Id, teamName);

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

        Result result = await _teamService.TransferCaptainshipAsync(season.Id, newCaptainPlayer.Id, teamName);

        await FollowupAsync(result.Message);
    }
}
