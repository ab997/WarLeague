using Discord.Interactions;
using Discord;
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

        if (!_helperService.IsValidReplayUrl(replayUrl))
        {
            await FollowupAsync("Please provide a valid HTTP/HTTPS replay URL.");
            return;
        }

        // Ensure caller exists as Player in the system.
        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _matchService.ReportLossAsync(season.Id, callerPlayer.Id, replayUrl);

        await FollowupAsync(result.Message);
    }

    [SlashCommand("undo", "Undo a previously reported match result between two players")]
    [RequireRole("Admin")]
    public async Task UndoAsync(
        [Summary("player1", "First player")] IUser player1,
        [Summary("player2", "Second player")] IUser player2)
    {
        await DeferAsync(ephemeral: false);

        // Ensure players exist as Player in the system.
        Player p1 = await _playerService.EnsurePlayerExistsAsync(player1);
        Player p2 = await _playerService.EnsurePlayerExistsAsync(player2);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _matchService.UndoResultAsync(season.Id, p1.Id, p2.Id);

        await FollowupAsync(result.Message);
    }

    [SlashCommand("result", "Admin: Report a result for a scheduled match between two players")]
    [RequireRole("Admin")]
    public async Task ReportResultAsync(
        [Summary("winner", "Winner player")] IUser winner,
        [Summary("loser", "Loser player")] IUser loser,
        [Summary("replay-url", "Replay URL for this match")] string replayUrl)
    {
        await DeferAsync(ephemeral: false);

        // Validate replay URL
        if (!_helperService.IsValidReplayUrl(replayUrl))
        {
            await FollowupAsync("Please provide a valid HTTP/HTTPS replay URL.");
            return;
        }

        // Resolve season
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        // Ensure players exist
        Player w = await _playerService.EnsurePlayerExistsAsync(winner);
        Player l = await _playerService.EnsurePlayerExistsAsync(loser);

        if (w.Id == l.Id)
        {
            await FollowupAsync("Winner and loser must be different players.");
            return;
        }

        BaseResult result = await _matchService.ReportResultAsync(season.Id, w.Id, l.Id, replayUrl);
        await FollowupAsync(result.Message);
    }
}

