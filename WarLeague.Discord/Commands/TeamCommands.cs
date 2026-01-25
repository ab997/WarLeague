using Discord;
using Discord.Interactions;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("team", "Team management commands")]
[RequireRole("Admin", Group = "Permission")]
[RequireRole("Captain", Group = "Permission")]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
public class TeamCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly WarLeagueDbContext _context;
    private readonly PlayerService _playerService;
    private readonly DiscordApiHelperService _helperService;
    private readonly TeamRepository _teamRepository;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    public TeamCommands(PlayerService playerService, DiscordApiHelperService helperService, TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository, WarLeagueDbContext dbContext)
    {
        _playerService = playerService;
        _helperService = helperService;
        _teamRepository = teamRepository;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _context = dbContext;
    }


    [SlashCommand("create", "Creates team with you as captain")]
    public async Task CreateAsync(
        [Summary("team-name", "Name of the team")] string teamName)
    {
        await DeferAsync(ephemeral: false);

        using var transaction = await _context.Database.BeginTransactionAsync();

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Team? check = await _teamRepository.GetByNameAsync(teamName);

        if (check != null)
        {
            await FollowupAsync($"A team with the name '{teamName}' already exists.");
            return;
        }

        Player player = await _playerService.EnsurePlayerExistsAsync(Context.User);


        bool success = await _playerSeasonTeamRepository.EnsurePlayerIsNotMemberOfTeamInSeasonAsync(player.Id, season.Id);

        if (!success)
        {
            await FollowupAsync($"You are already a member of another team.");
            return;
        }

        Team team = new Team
        {
            Name = teamName,
            Captain = player,
            CreatedDate = DateTime.UtcNow,
            Season = season,
        };

        await _teamRepository.AddAsync(team);

        PlayerSeasonTeam pst = new PlayerSeasonTeam
        {
            Player = player,
            Season = season,
            Team = team
        };

        await _playerSeasonTeamRepository.AddAsync(pst);

        await FollowupAsync($"Team '{teamName}' created with you as captain.");

        await transaction.CommitAsync();
    }

    [SlashCommand("admin-create", "Creates a team and assigns the specified user as captain (Admin only)")]
    public async Task AdminCreateAsync(
       [Summary("team-name", "Name of the team")] string teamName,
       [Summary("captain", "User to set as captain")] IUser captain)
    {
        await DeferAsync(ephemeral: false);

        // ensure only admins can use this
        if (!_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Only users with the Admin role can use this command.");
            return;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Team? existing = await _teamRepository.GetByNameAsync(teamName);
        if (existing != null)
        {
            await FollowupAsync($"A team with the name '{teamName}' already exists.");
            return;
        }

        // Ensure the captain exists as a Player
        Player captainPlayer = await _playerService.EnsurePlayerExistsAsync(captain);

        // Ensure captain is not already a member of another team this season
        bool captainAvailable = await _playerSeasonTeamRepository.EnsurePlayerIsNotMemberOfTeamInSeasonAsync(captainPlayer.Id, season.Id);
        if (!captainAvailable)
        {
            await FollowupAsync($"The user {captain.Mention} is already a member of another team for this season.");
            return;
        }

        Team team = new Team
        {
            Name = teamName,
            Captain = captainPlayer,
            CreatedDate = DateTime.UtcNow,
            Season = season,
        };

        await _teamRepository.AddAsync(team);

        PlayerSeasonTeam pst = new PlayerSeasonTeam
        {
            Player = captainPlayer,
            Season = season,
            Team = team
        };

        await _playerSeasonTeamRepository.AddAsync(pst);

        await FollowupAsync($"Team '{teamName}' created with {captain.Mention} as captain.");

        await transaction.CommitAsync();
    }

    [SlashCommand("delete", "Deletes team")]
    public async Task DeleteAsync(
       [Summary("team-name", "Name of the team")] string teamName)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, season.Id);
        if (team == null)
        {
            await FollowupAsync($"Team with name '{teamName}' not found.");
            return;
        }

        Player player = await _playerService.EnsurePlayerExistsAsync(Context.User);

        // Determine if caller has the Admin role in the guild
        bool isAdmin = _helperService.IsUserAdmin(Context);

        if (!isAdmin && team.CaptainId != player.Id)
        {
            await FollowupAsync("Only the team captain or an Admin can delete this team.");
            return;
        }

        await _teamRepository.DeleteAsync(team);

        await FollowupAsync($"Team '{teamName}' deleted.");
    }

    [SlashCommand("add-member", "Adds a member to your team (captain only)")]
    public async Task AddMemberAsync(
       [Summary("member", "User to add")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        using var transaction = await _context.Database.BeginTransactionAsync();

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        // Ensure caller is a player
        Player caller = await _playerService.EnsurePlayerExistsAsync(Context.User);

        // Find the team where caller is captain for the current season
        Team? team = await _teamRepository.GetByCaptainAndSeasonAsync(caller.Id, season.Id);
        if (team == null)
        {
            await FollowupAsync("You are not the captain of any team for the current season.");
            return;
        }

        // Ensure target player exists
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        bool notMember = await _playerSeasonTeamRepository.EnsurePlayerIsNotMemberOfTeamInSeasonAsync(targetPlayer.Id, season.Id);
        if (!notMember)
        {
            await FollowupAsync($"The user {user.Mention} is already a member of another team for this season.");
            return;
        }

        PlayerSeasonTeam pst = new PlayerSeasonTeam
        {
            Player = targetPlayer,
            Season = season,
            Team = team
        };

        await _playerSeasonTeamRepository.AddAsync(pst);

        await FollowupAsync($"Added {user.Mention} to your team '{team.Name}'.");

        await transaction.CommitAsync();
    }

    [SlashCommand("admin-add-member", "Adds a member to any team (Admin only)")]
    public async Task AdminAddMemberAsync(
      [Summary("team-name", "Name of the team")] string teamName,
      [Summary("member", "User to add")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        if (!_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Only Admins can use this command.");
            return;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Team? team = await _teamRepository.GetByNameAsync(teamName);
        if (team == null)
        {
            await FollowupAsync($"Team with name '{teamName}' not found.");
            return;
        }

        // Ensure target player exists
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        bool notMember = await _playerSeasonTeamRepository.EnsurePlayerIsNotMemberOfTeamInSeasonAsync(targetPlayer.Id, season.Id);
        if (!notMember)
        {
            await FollowupAsync($"The user {user.Mention} is already a member of another team for this season.");
            return;
        }

        PlayerSeasonTeam pst = new PlayerSeasonTeam
        {
            Player = targetPlayer,
            Season = season,
            Team = team
        };

        await _playerSeasonTeamRepository.AddAsync(pst);

        await FollowupAsync($"Added {user.Mention} to team '{teamName}'.");

        await transaction.CommitAsync();
    }
}
