using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("mod", "Moderator commands")]
public class ModCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly WeekService _weekService;
    private readonly MatchService _matchService;
    private readonly DeckSubmissionService _deckSubmissionService;
    private readonly PermissionService _permissionService;
    private readonly TeamRepository _teamRepository;
    private readonly PlayerRepository _playerRepository;
    private readonly WeekRepository _weekRepository;
    private readonly TeamService _teamService;
    private readonly IConfiguration _configuration;

    public ModCommands(
        WeekService weekService,
        MatchService matchService,
        DeckSubmissionService deckSubmissionService,
        PermissionService permissionService,
        TeamRepository teamRepository,
        PlayerRepository playerRepository,
        WeekRepository weekRepository,
        TeamService teamService,
        IConfiguration configuration)
    {
        _weekService = weekService;
        _matchService = matchService;
        _deckSubmissionService = deckSubmissionService;
        _permissionService = permissionService;
        _teamRepository = teamRepository;
        _playerRepository = playerRepository;
        _weekRepository = weekRepository;
        _teamService = teamService;
        _configuration = configuration;
    }

 

    [SlashCommand("close-submissions", "Close submissions for a week")]
    public async Task CloseSubmissions(
        [Summary("week", "Week number")] int weekNumber)
    {
        if (!await _permissionService.IsAdminAsync(Context.User.Id))
        {
            await RespondAsync("Only administrators can close submissions.", ephemeral: true);
            return;
        }

        var week = await _weekRepository.GetByWeekNumberAsync(weekNumber, 1); // Assuming season 1
        if (week == null)
        {
            await RespondAsync($"Week {weekNumber} not found.", ephemeral: true);
            return;
        }

        try
        {
            await _weekService.CloseSubmissionsAsync(week.Id);
            await RespondAsync($"Submissions closed for Week {weekNumber}.", ephemeral: false);
        }
        catch (Exception ex)
        {
            await RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [SlashCommand("check-submissions", "Check which teams have submitted decks")]
    public async Task CheckSubmissions(
        [Summary("week", "Week number")] int weekNumber)
    {
        if (!await _permissionService.IsAdminAsync(Context.User.Id))
        {
            await RespondAsync("Only administrators can check submissions.", ephemeral: true);
            return;
        }

        var week = await _weekRepository.GetByWeekNumberAsync(weekNumber, 1); // Assuming season 1
        if (week == null)
        {
            await RespondAsync($"Week {weekNumber} not found.", ephemeral: true);
            return;
        }

        var teams = await _teamRepository.GetAllActiveAsync();
        var submissions = await _deckSubmissionService.GetSubmissionsByWeekAsync(week.Id);

        var submittedTeamIds = submissions.Select(s => s.Player.TeamId).Distinct().ToHashSet();
        var submittedTeams = teams.Where(t => submittedTeamIds.Contains(t.Id)).ToList();
        var missingTeams = teams.Where(t => !submittedTeamIds.Contains(t.Id)).ToList();

        var response = $"**Submissions Status for Week {weekNumber}**\n\n";
        response += $"**Submitted ({submittedTeams.Count}/{teams.Count}):**\n";
        foreach (var team in submittedTeams)
        {
            response += $"- {team.Name}\n";
        }

        response += $"\n**Missing ({missingTeams.Count}/{teams.Count}):**\n";
        foreach (var team in missingTeams)
        {
            response += $"- {team.Name}\n";
        }

        await RespondAsync(response, ephemeral: false);
    }

    [SlashCommand("view-decks", "View submitted deck lists for a team")]
    public async Task ViewDecks(
        [Summary("week", "Week number")] int weekNumber,
        [Summary("team", "Team name")] string teamName)
    {
        if (!await _permissionService.IsAdminAsync(Context.User.Id))
        {
            await RespondAsync("Only administrators can view deck lists.", ephemeral: true);
            return;
        }

        var week = await _weekRepository.GetByWeekNumberAsync(weekNumber, 1); // Assuming season 1
        if (week == null)
        {
            await RespondAsync($"Week {weekNumber} not found.", ephemeral: true);
            return;
        }

        var teams = await _teamRepository.GetAllActiveAsync();
        var team = teams.SingleOrDefault(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase));
        if (team == null)
        {
            await RespondAsync($"Team '{teamName}' not found.", ephemeral: true);
            return;
        }

        var submissions = await _deckSubmissionService.GetSubmissionsByTeamAndWeekAsync(team.Id, week.Id);

        if (submissions.Count == 0)
        {
            await RespondAsync($"No deck submissions found for {team.Name} in Week {weekNumber}.", ephemeral: false);
            return;
        }

        var response = $"**Deck Submissions for {team.Name} - Week {weekNumber}**\n\n";
        foreach (var submission in submissions)
        {
            response += $"**{submission.Player.DiscordUsername}**\n";
            response += $"Format: {submission.Week.Season.Format.Name}\n";
            response += $"Deck: {submission.DeckFile}\n";
            response += $"Submitted: {submission.SubmittedDate:yyyy-MM-dd HH:mm}\n";
            response += $"Validated: {(submission.IsValidated ? "Yes" : "No (TODO: legality check)")}\n\n";
        }

        await RespondAsync(response, ephemeral: false);
    }

    [SlashCommand("substitute", "Make a player substitution")]
    public async Task Substitute(
        [Summary("player1", "Player to replace")] IUser player1User,
        [Summary("player2", "Replacement player")] IUser player2User,
        [Summary("week", "Week number")] int weekNumber)
    {
        if (!await _permissionService.IsAdminAsync(Context.User.Id))
        {
            await RespondAsync("Only administrators can make substitutions.", ephemeral: true);
            return;
        }

        var player1 = await _playerRepository.GetByDiscordUserIdAsync(player1User.Id);
        var player2 = await _playerRepository.GetByDiscordUserIdAsync(player2User.Id);

        if (player1 == null || player2 == null)
        {
            await RespondAsync("One or both players are not registered.", ephemeral: true);
            return;
        }

        var week = await _weekRepository.GetByWeekNumberAsync(weekNumber, 1); // Assuming season 1
        if (week == null)
        {
            await RespondAsync($"Week {weekNumber} not found.", ephemeral: true);
            return;
        }

        // TODO: Implement substitution logic
        // This would involve updating matches and deck submissions
        await RespondAsync($"Substitution functionality not yet implemented. This would replace {player1User.Mention} with {player2User.Mention} in Week {weekNumber}.", ephemeral: false);
    }

    [SlashCommand("toggle-captain-actions", "Enable/disable captain add/drop functionality")]
    public async Task ToggleCaptainActions()
    {
        if (!await _permissionService.IsAdminAsync(Context.User.Id))
        {
            await RespondAsync("Only administrators can toggle captain actions.", ephemeral: true);
            return;
        }

        // This would need to be stored in database or configuration
        // For now, we'll use a simple approach with configuration
        var currentValue = _configuration.GetValue<bool>("WarLeague:CaptainActionsEnabled", true);
        var newValue = !currentValue;

        // TODO: Update configuration/database
        await RespondAsync($"Captain actions are now {(newValue ? "enabled" : "disabled")}. (Note: This change is not persisted yet)", ephemeral: false);
    }

    [SlashCommand("announce-week-start", "Post a week start announcement")]
    public async Task AnnounceWeekStart(
        [Summary("week", "Week number")] int weekNumber,
        [Summary("channel", "Channel to post announcement in")] IChannel? channel = null)
    {
        if (!await _permissionService.IsAdminAsync(Context.User.Id))
        {
            await RespondAsync("Only administrators can post announcements.", ephemeral: true);
            return;
        }

        var week = await _weekRepository.GetByWeekNumberAsync(weekNumber, 1); // Assuming season 1
        if (week == null)
        {
            await RespondAsync($"Week {weekNumber} not found.", ephemeral: true);
            return;
        }

        var targetChannel = channel as ITextChannel ?? Context.Channel as ITextChannel;
        if (targetChannel == null)
        {
            await RespondAsync("Invalid channel.", ephemeral: true);
            return;
        }

        var announcement = $"# Week {weekNumber} Has Started! 🎮\n\n";
        announcement += $"**Start Date:** {week.StartDate:yyyy-MM-dd}\n";
        announcement += $"**End Date:** {week.EndDate:yyyy-MM-dd}\n\n";
        announcement += "Deck submissions are now open! Team captains can submit decks using `/team submit-deck`.\n";
        announcement += "Good luck to all teams!";

        await targetChannel.SendMessageAsync(announcement);
        await RespondAsync($"Announcement posted in {targetChannel.Mention}!", ephemeral: true);
    }
}
