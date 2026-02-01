using Discord;
using Discord.Interactions;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("report", "Match result reporting commands")]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
public class ReportCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordPlayerService _playerService;
    private readonly MatchRepository _matchRepository;
    private readonly WeekRepository _weekRepository;
    private readonly DiscordApiHelperService _helperService;

    public ReportCommands(
        DiscordPlayerService playerService,
        MatchRepository matchRepository,
        WeekRepository weekRepository,
        DiscordApiHelperService helperService)
    {
        _playerService = playerService;
        _matchRepository = matchRepository;
        _weekRepository = weekRepository;
        _helperService = helperService;
    }

    [SlashCommand("loss", "Report a loss and attach a replay URL")]
    public async Task ReportLossAsync(
        [Summary("replay-url", "Replay URL for this match")] string replayUrl)
    {
        await DeferAsync(ephemeral: false);

        if (Context.Guild is null)
        {
            await FollowupAsync("This command can only be used inside a guild.");
            return;
        }

        if (!Uri.TryCreate(replayUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            await FollowupAsync("Please provide a valid HTTP/HTTPS replay URL.");
            return;
        }

        // Ensure caller exists as Player in the system.
        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Week? week;
        try
        {
            week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(season.Id, WeekStatus.InProgress);
        }
        catch (InvalidOperationException)
        {
            await FollowupAsync($"There are multiple weeks with status '{WeekStatus.InProgress}' for the active season. Please ask an Admin to fix the week statuses.");
            return;
        }

        // Only allow reporting for matches where the caller actually has a scheduled match.
        var callerMatches = await _matchRepository.GetByPlayerAndWeekAsync(callerPlayer.Id, week!.Id);

        var scheduledMatches = callerMatches
            .Where(m => m.Status == MatchStatus.Scheduled)
            .ToList();

        if (scheduledMatches.Count == 0)
        {
            await FollowupAsync("You do not have any scheduled matches that can be reported as a loss.");
            return;
        }

        if (scheduledMatches.Count > 1)
        {
            // Ambiguous which opponent this loss is against; require admins to resolve.
            var opponents = scheduledMatches
                .Select(m => m.Player1Id == callerPlayer.Id ? m.Player2 : m.Player1)
                .DistinctBy(p => p.Id)
                .Select(p => $"<@{p.DiscordUserId}>")
                .ToList();

            await FollowupAsync(
                "You have multiple scheduled matches pending; I can't determine which one you are reporting a loss for.\n" +
                "Pending opponents: " + string.Join(", ", opponents));
            return;
        }

        var match = scheduledMatches.Single();
        var opponentPlayer = match.Player1Id == callerPlayer.Id ? match.Player2 : match.Player1;

        // Loser is the caller, so winner is the opponent.
        match.WinnerId = opponentPlayer.Id;
        match.Status = MatchStatus.Reported;
        match.ReportedDate = DateTime.UtcNow;
        match.ReplayUrl = replayUrl;

        await _matchRepository.UpdateAsync(match);

        await FollowupAsync(
            $"Recorded loss for <@{callerPlayer.DiscordUserId}> vs <@{opponentPlayer.DiscordUserId}>.\n" +
            $"Winner: <@{opponentPlayer.DiscordUserId}>.\n" +
            $"Replay: {replayUrl}");
    }
}

