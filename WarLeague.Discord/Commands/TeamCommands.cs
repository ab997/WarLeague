using Discord;
using Discord.Interactions;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("team", "Team management commands")]
public class TeamCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TeamService _teamService;
    private readonly PermissionService _permissionService;
    private readonly PlayerRepository _playerRepository;
    private readonly TeamRepository _teamRepository;
    private readonly DeckSubmissionService _deckSubmissionService;
    private readonly FormatRepository _formatRepository;
    private readonly WeekRepository _weekRepository;
    private readonly FileValidationService _fileValidationService;

    public TeamCommands(
        TeamService teamService,
        PermissionService permissionService,
        PlayerRepository playerRepository,
        TeamRepository teamRepository,
        DeckSubmissionService deckSubmissionService,
        FormatRepository formatRepository,
        WeekRepository weekRepository,
        FileValidationService fileValidationService)
    {
        _teamService = teamService;
        _permissionService = permissionService;
        _playerRepository = playerRepository;
        _teamRepository = teamRepository;
        _deckSubmissionService = deckSubmissionService;
        _formatRepository = formatRepository;
        _weekRepository = weekRepository;
        _fileValidationService = fileValidationService;
    }

    [SlashCommand("add-player", "Add a player to your team")]
    public async Task AddPlayer(
        [Summary("player", "Discord user to add to the team")] IUser user)
    {
        var captainId = await _permissionService.GetPlayerIdAsync(Context.User.Id);
        if (!captainId.HasValue)
        {
            await RespondAsync("You are not registered as a player.", ephemeral: false);
            return;
        }

        if (!await _permissionService.IsTeamCaptainAsync(Context.User.Id))
        {
            await RespondAsync("Only team captains can add players to teams.", ephemeral: false);
            return;
        }

        var team = await _teamRepository.GetByCaptainIdAsync(captainId.Value);
        if (team == null)
        {
            await RespondAsync("You are not a captain of any team.", ephemeral: false);
            return;
        }

        var playerToAdd = await _playerRepository.GetByDiscordUserIdAsync(user.Id);
        if (playerToAdd == null)
        {
            await RespondAsync($"User {user.Mention} is not registered as a player.", ephemeral: false);
            return;
        }

        try
        {
            await _teamService.AddPlayerToTeamAsync(team.Id, playerToAdd.Id, captainId.Value);
            await RespondAsync($"Successfully added {user.Mention} to {team.Name}!", ephemeral: false);
        }
        catch (Exception ex)
        {
            await RespondAsync($"Error: {ex.Message}", ephemeral: false);
        }
    }

    [SlashCommand("drop-player", "Remove a player from your team")]
    public async Task DropPlayer(
        [Summary("player", "Discord user to remove from the team")] IUser user)
    {
        var captainId = await _permissionService.GetPlayerIdAsync(Context.User.Id);
        if (!captainId.HasValue)
        {
            await RespondAsync("You are not registered as a player.", ephemeral: false);
            return;
        }

        if (!await _permissionService.IsTeamCaptainAsync(Context.User.Id))
        {
            await RespondAsync("Only team captains can remove players from teams.", ephemeral: false);
            return;
        }

        var team = await _teamRepository.GetByCaptainIdAsync(captainId.Value);
        if (team == null)
        {
            await RespondAsync("You are not a captain of any team.", ephemeral: false);
            return;
        }

        var playerToRemove = await _playerRepository.GetByDiscordUserIdAsync(user.Id);
        if (playerToRemove == null)
        {
            await RespondAsync($"User {user.Mention} is not registered as a player.", ephemeral: false);
            return;
        }

        try
        {
            await _teamService.RemovePlayerFromTeamAsync(team.Id, playerToRemove.Id, captainId.Value);
            await RespondAsync($"Successfully removed {user.Mention} from {team.Name}!", ephemeral: false);
        }
        catch (Exception ex)
        {
            await RespondAsync($"Error: {ex.Message}", ephemeral: false);
        }
    }

    [SlashCommand("submit-deck", "Submit a deck for a week")]
    public async Task SubmitDeck(
        [Summary("week", "Week number")] int weekNumber,
        [Summary("format", "Format name (HAT, GOAT, Edison, etc.)")] string formatName,
        [Summary("file", "Deck file (.ydk)")] IAttachment file)
    {
        var playerId = await _permissionService.GetPlayerIdAsync(Context.User.Id);
        if (!playerId.HasValue)
        {
            await RespondAsync("You are not registered as a player.", ephemeral: false);
            return;
        }

        if (!await _permissionService.IsTeamCaptainAsync(Context.User.Id))
        {
            await RespondAsync("Only team captains can submit decks.", ephemeral: false);
            return;
        }

        var player = await _playerRepository.GetByIdAsync(playerId.Value);
        if (player?.TeamId == null)
        {
            await RespondAsync("You are not on a team.", ephemeral: false);
            return;
        }

        if (!_fileValidationService.IsValidYdkFile(file.Filename))
        {
            await RespondAsync("Deck file must be a .ydk file.", ephemeral: false);
            return;
        }

        var format = await _formatRepository.GetByNameAsync(formatName);
        if (format == null)
        {
            await RespondAsync($"Format '{formatName}' not found.", ephemeral: false);
            return;
        }

        var week = await _weekRepository.GetByWeekNumberAsync(weekNumber, 1); // Assuming season 1 for now
        if (week == null)
        {
            await RespondAsync($"Week {weekNumber} not found.", ephemeral: false);
            return;
        }

        try
        {
            var submission = await _deckSubmissionService.SubmitDeckAsync(
                week.Id, playerId.Value, player.TeamId.Value, format.Id, file.Url);

            await RespondAsync($"Successfully submitted deck for Week {weekNumber} ({format.Name})!", ephemeral: false);
        }
        catch (Exception ex)
        {
            await RespondAsync($"Error: {ex.Message}", ephemeral: false);
        }
    }

    [SlashCommand("roster", "View your team's roster")]
    public async Task ViewRoster()
    {
        var playerId = await _permissionService.GetPlayerIdAsync(Context.User.Id);
        if (!playerId.HasValue)
        {
            await RespondAsync("You are not registered as a player.", ephemeral: false);
            return;
        }

        var player = await _playerRepository.GetByIdAsync(playerId.Value);
        if (player?.TeamId == null)
        {
            await RespondAsync("You are not on a team.", ephemeral: false);
            return;
        }

        var team = await _teamRepository.GetByIdAsync(player.TeamId.Value);
        if (team == null)
        {
            await RespondAsync("Your team was not found.", ephemeral: false);
            return;
        }

        var rosterText = $"**{team.Name} Roster**\n";
        rosterText += $"Captain: <@{team.Captain.DiscordUserId}>\n\n";
        rosterText += "**Players:**\n";

        foreach (var rosterPlayer in team.Players)
        {
            rosterText += $"- <@{rosterPlayer.DiscordUserId}> ({rosterPlayer.DiscordUserId})\n";
        }

        await RespondAsync(rosterText, ephemeral: false);
    }
}
