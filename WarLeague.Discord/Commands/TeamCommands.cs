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
[EnsureSingleActiveSeason]
[EnsureChannelIsInFormatCategory]
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

        await transaction.CommitAsync();

        await FollowupAsync($"Team '{teamName}' created with you as captain.");
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

        await transaction.CommitAsync();

        await FollowupAsync($"Team '{teamName}' created with {captain.Mention} as captain.");
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

        // Block captain modifications if disabled for the season (Admins bypass)
        if (season.DisableTeamModification && !_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Team modifications are currently disabled for this season.");
            return;
        }

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

        await transaction.CommitAsync();

        await FollowupAsync($"Added {user.Mention} to your team '{team.Name}'.");
    }

    [SlashCommand("drop-member", "Removes a member from your team (captain only)")]
    public async Task DropMemberAsync(
       [Summary("member", "User to remove")] IUser user)
    {
        await DeferAsync(ephemeral: false);

        using var transaction = await _context.Database.BeginTransactionAsync();

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        // Block captain modifications if disabled for the season (Admins bypass)
        if (season.DisableTeamModification && !_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Team modifications are currently disabled for this season.");
            return;
        }

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

        // Prevent dropping the captain
        if (team.CaptainId == targetPlayer.Id)
        {
            await FollowupAsync("The team captain cannot be removed. Transfer captainship first if needed.");
            return;
        }

        // Ensure the user is a member of this team in the current season
        PlayerSeasonTeam? pst =
            await _playerSeasonTeamRepository.GetByPlayerSeasonAndTeamAsync(
                targetPlayer.Id,
                season.Id,
                team.Id);

        if (pst == null)
        {
            await FollowupAsync($"{user.Mention} is not a member of your team '{team.Name}'.");
            return;
        }

        await _playerSeasonTeamRepository.DeleteAsync(pst);

        await transaction.CommitAsync();

        await FollowupAsync($"Removed {user.Mention} from your team '{team.Name}'.");
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

        await transaction.CommitAsync();

        await FollowupAsync($"Added {user.Mention} to team '{teamName}'.");
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

        using var transaction = await _context.Database.BeginTransactionAsync();

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        // Ensure target player exists
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(user);

        // Ensure the player has a membership in the current season
        PlayerSeasonTeam? pst =
            await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(targetPlayer.Id, season.Id);

        if (pst == null)
        {
            await FollowupAsync($"{user.Mention} is not a member of any team for the current season.");
            return;
        }

        // Prevent dropping captains
        bool isCaptain = !await _playerSeasonTeamRepository.EnsurePlayerIsNotCaptainOfTeamInSeasonAsync(targetPlayer.Id, season.Id);
        if (isCaptain)
        {
            await FollowupAsync("Captains cannot be removed. Transfer captainship first if needed.");
            return;
        }

        await _playerSeasonTeamRepository.DeleteAsync(pst);

        await transaction.CommitAsync();

        await FollowupAsync($"Removed {user.Mention} from team '{pst.Team.Name}'.");
    }

    [SlashCommand("admin-transfer-member", "Transfers a member to another team (Admin only)")]
    public async Task AdminTransferMemberAsync(
    [Summary("member", "User to transfer")] IUser user,
    [Summary("team-name", "Target team name")] string teamName)
    {
        await DeferAsync(ephemeral: false);

        if (!_helperService.IsUserAdmin(Context))
        {
            await FollowupAsync("Only Admins can use this command.");
            return;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Team? targetTeam = await _teamRepository.GetByNameAsync(teamName);
        if (targetTeam == null)
        {
            await FollowupAsync($"Team with name '{teamName}' not found.");
            return;
        }

        Player player = await _playerService.EnsurePlayerExistsAsync(user);

        // Captains cannot be transferred
        bool canTransfer = await _playerSeasonTeamRepository.EnsurePlayerIsNotCaptainOfTeamInSeasonAsync(player.Id, season.Id);
        if (!canTransfer)
        {
            await FollowupAsync("Captains cannot be transferred.");
            return;
        }

        // Find existing membership (if any)
        PlayerSeasonTeam? existingPst =
            await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(player.Id, season.Id);

        if (existingPst != null)
        {
            await _playerSeasonTeamRepository.DeleteAsync(existingPst);
        }

        PlayerSeasonTeam newPst = new PlayerSeasonTeam
        {
            Player = player,
            Season = season,
            Team = targetTeam
        };

        await _playerSeasonTeamRepository.AddAsync(newPst);

        await transaction.CommitAsync();

        await FollowupAsync($"Transferred {user.Mention} to team '{targetTeam.Name}'.");
    }

    [SlashCommand("admin-transfer-captainship", "Transfers captainship to another team member (Admin only)")]
    public async Task AdminTransferCaptainshipAsync(
    [Summary("team-name", "Name of the team")] string teamName,
    [Summary("new-captain", "User to become captain")] IUser newCaptain)
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

        Player newCaptainPlayer = await _playerService.EnsurePlayerExistsAsync(newCaptain);

        // Ensure the user is already a member of the team
        PlayerSeasonTeam? pst =
            await _playerSeasonTeamRepository.GetByPlayerSeasonAndTeamAsync(
                newCaptainPlayer.Id,
                season.Id,
                team.Id);

        if (pst == null)
        {
            await FollowupAsync($"{newCaptain.Mention} is not a member of team '{team.Name}'. Captainship transfer is supported only among existing members.");
            return;
        }

        team.CaptainId = newCaptainPlayer.Id;

        await _teamRepository.UpdateAsync(team);

        await transaction.CommitAsync();

        await FollowupAsync($"{newCaptain.Mention} is now the captain of '{team.Name}'.");
    }

    
}
