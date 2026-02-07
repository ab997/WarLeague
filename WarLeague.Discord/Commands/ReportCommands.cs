using Discord.Interactions;
using Discord;
using WarLeague.Data.Entities;
using WarLeague.Discord.Constants;
using WarLeague.Discord.Helpers;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using WarLeague.Core.Services;
using WarLeague.Core.Model;

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

        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _matchService.ReportLossAsync(season.Id, callerPlayer.Id, replayUrl);

        await FollowupAsync(ResultHelper.Stringify(result));
    }

    [SlashCommand("undo", "Undo a previously reported match result between two players")]
    [RequireRole(DiscordRoleConstants.Admin)]
    public async Task UndoAsync(
        [Summary("player1", "First player")] IUser player1,
        [Summary("player2", "Second player")] IUser player2)
    {
        await DeferAsync(ephemeral: false);

        Player p1 = await _playerService.EnsurePlayerExistsAsync(player1);
        Player p2 = await _playerService.EnsurePlayerExistsAsync(player2);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _matchService.UndoResultAsync(season.Id, p1.Id, p2.Id);

        await FollowupAsync(ResultHelper.Stringify(result));
    }

    [SlashCommand("no-show", "Mark a match as no show")]
    [RequireRole(DiscordRoleConstants.Admin)]
    public async Task NoShowAsync(
       [Summary("player-winmer", "Winner player")] IUser playerWinner,
       [Summary("player-no-show", "No show player")] IUser playerNoShow)
    {
        await DeferAsync(ephemeral: false);

        Player p1 = await _playerService.EnsurePlayerExistsAsync(playerWinner);
        Player p2 = await _playerService.EnsurePlayerExistsAsync(playerNoShow);
        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _matchService.NoShowAsync(season.Id, p1.Id, p2.Id);

        await FollowupAsync(ResultHelper.Stringify(result));
    }

    [SlashCommand("result", "Admin: Report a result for a scheduled match between two players")]
    [RequireRole(DiscordRoleConstants.Admin)]
    public async Task ReportResultAsync(
        [Summary("winner", "Winner player")] IUser winner,
        [Summary("loser", "Loser player")] IUser loser,
        [Summary("replay-url", "Replay URL for this match")] string replayUrl)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player w = await _playerService.EnsurePlayerExistsAsync(winner);
        Player l = await _playerService.EnsurePlayerExistsAsync(loser);

        BaseResult result = await _matchService.ReportResultAsync(season.Id, w.Id, l.Id, replayUrl);

        await FollowupAsync(ResultHelper.Stringify(result));
    }
}

