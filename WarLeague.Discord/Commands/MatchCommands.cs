//using Discord;
//using Discord.Interactions;
//using WarLeague.Core.Domain.Services;
//using WarLeague.Core.Repositories;
//using WarLeague.Discord.Services;

//namespace WarLeague.Discord.Commands;

//[Group("match", "Match reporting commands")]
//public class MatchCommands : InteractionModuleBase<SocketInteractionContext>
//{
//    private readonly MatchService _matchService;
//    private readonly PermissionService _permissionService;
//    private readonly PlayerRepository _playerRepository;
//    private readonly WeekRepository _weekRepository;
//    private readonly FileValidationService _fileValidationService;

//    public MatchCommands(
//        MatchService matchService,
//        PermissionService permissionService,
//        PlayerRepository playerRepository,
//        WeekRepository weekRepository,
//        FileValidationService fileValidationService)
//    {
//        _matchService = matchService;
//        _permissionService = permissionService;
//        _playerRepository = playerRepository;
//        _weekRepository = weekRepository;
//        _fileValidationService = fileValidationService;
//    }

//    [SlashCommand("report-loss", "Report a match loss with replay URL")]
//    public async Task ReportLoss(
//        [Summary("opponent", "Your opponent's Discord user")] IUser opponent,
//        [Summary("replay-url", "URL to the replay")] string replayUrl)
//    {
//        var playerId = await _permissionService.GetPlayerIdAsync(Context.User.Id);
//        if (!playerId.HasValue)
//        {
//            await RespondAsync("You are not registered as a player.", ephemeral: false);
//            return;
//        }

//        if (!_fileValidationService.IsValidReplayUrl(replayUrl))
//        {
//            await RespondAsync("Invalid replay URL. Please provide a valid HTTP/HTTPS URL.", ephemeral: false);
//            return;
//        }

//        var opponentPlayer = await _playerRepository.GetByDiscordUserIdAsync(opponent.Id);
//        if (opponentPlayer == null)
//        {
//            await RespondAsync($"User {opponent.Mention} is not registered as a player.", ephemeral: false);
//            return;
//        }

//        // Find the match between these two players
//        var matches = await _matchService.GetMatchesByPlayerAsync(playerId.Value);
//        var match = matches.SingleOrDefault(m =>
//            (m.Player1Id == playerId.Value && m.Player2Id == opponentPlayer.Id) ||
//            (m.Player2Id == playerId.Value && m.Player1Id == opponentPlayer.Id));

//        if (match == null)
//        {
//            await RespondAsync("No match found between you and this opponent.", ephemeral: false);
//            return;
//        }

//        try
//        {
//            var reportedMatch = await _matchService.ReportMatchResultAsync(
//                match.Id, opponentPlayer.Id, playerId.Value, replayUrl);

//            await RespondAsync($"Match result reported! Winner: {opponent.Mention}", ephemeral: false);
//        }
//        catch (Exception ex)
//        {
//            await RespondAsync($"Error: {ex.Message}", ephemeral: false);
//        }
//    }

//    [SlashCommand("view-replays", "View replays for a week")]
//    public async Task ViewReplays(
//        [Summary("week", "Week number")] int weekNumber)
//    {
//        var week = await _weekRepository.GetByWeekNumberAsync(weekNumber, 1); // Assuming season 1
//        if (week == null)
//        {
//            await RespondAsync($"Week {weekNumber} not found.", ephemeral: false);
//            return;
//        }

//        var matches = await _matchService.GetMatchesByWeekAsync(week.Id);
//        var reportedMatches = matches.Where(m => !string.IsNullOrEmpty(m.ReplayUrl)).ToList();

//        if (reportedMatches.Count == 0)
//        {
//            await RespondAsync($"No replays available for Week {weekNumber}.", ephemeral: false);
//            return;
//        }

//        var replaysText = $"**Replays for Week {weekNumber}**\n\n";
//        foreach (var match in reportedMatches)
//        {
//            var player1 = match.Player1.DiscordUsername;
//            var player2 = match.Player2.DiscordUsername;
//            var winner = match.Winner?.DiscordUsername ?? "TBD";
//            replaysText += $"**{player1} vs {player2}**\n";
//            replaysText += $"Winner: {winner}\n";
//            replaysText += $"Replay: {match.ReplayUrl}\n\n";
//        }

//        await RespondAsync(replaysText, ephemeral: false);
//    }

//    [SlashCommand("view-results", "View match results for a week")]
//    public async Task ViewResults(
//        [Summary("week", "Week number")] int weekNumber)
//    {
//        var week = await _weekRepository.GetByWeekNumberAsync(weekNumber, 1); // Assuming season 1
//        if (week == null)
//        {
//            await RespondAsync($"Week {weekNumber} not found.", ephemeral: false);
//            return;
//        }

//        var matches = await _matchService.GetMatchesByWeekAsync(week.Id);

//        var resultsText = $"**Match Results for Week {weekNumber}**\n\n";
//        foreach (var match in matches)
//        {
//            var player1 = match.Player1.DiscordUsername;
//            var player2 = match.Player2.DiscordUsername;
//            var status = match.Status.ToString();
//            var winner = match.Winner != null ? match.Winner.DiscordUsername : "TBD";

//            resultsText += $"**{player1} vs {player2}**\n";
//            resultsText += $"Status: {status}\n";
//            resultsText += $"Winner: {winner}\n";
//            if (!string.IsNullOrEmpty(match.ReplayUrl))
//            {
//                resultsText += $"Replay: {match.ReplayUrl}\n";
//            }
//            resultsText += "\n";
//        }

//        await RespondAsync(resultsText, ephemeral: false);
//    }
//}
