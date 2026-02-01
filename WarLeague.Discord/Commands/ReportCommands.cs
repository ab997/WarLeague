using Discord.Interactions;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Domain.Services;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("report", "Match result reporting commands")]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
public class ReportCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordPlayerService _playerService;
    private readonly MatchService _matchService;
    private readonly DiscordApiHelperService _helperService;

    public ReportCommands(
        DiscordPlayerService playerService,
        DiscordApiHelperService helperService,
        MatchService matchService)
    {
        _playerService = playerService;
        _matchService = matchService;
        _helperService = helperService;
    }

    [SlashCommand("loss", "Report a loss and attach a replay URL")]
    public async Task ReportLossAsync(
        [Summary("replay-url", "Replay URL for this match")] string replayUrl)
    {
        await DeferAsync(ephemeral: false);

        if (!Uri.TryCreate(replayUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            await FollowupAsync("Please provide a valid HTTP/HTTPS replay URL.");
            return;
        }

        // Ensure caller exists as Player in the system.
        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Result result = await _matchService.ReportLossAsync(season.Id, callerPlayer.Id, replayUrl);

        await FollowupAsync(result.Message);
    }

    [SlashCommand("undo", "Undo a previously reported match result for this week")]
    public async Task UndoAsync()
    {
        await DeferAsync(ephemeral: false);

        // Ensure caller exists as Player in the system.
        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Result result = await _matchService.UndoResultAsync(season.Id, callerPlayer.Id);

        await FollowupAsync(result.Message);
    }
}

