//using Discord.WebSocket;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using WarLeague.Core.Domain.Services;
//using WarLeague.Core.Repositories;
//using WarLeague.Discord.Services;
//using static WarLeague.Core.Domain.Services.StandingsService;

//namespace WarLeague.Discord.HostedService;

//public class DailyStandingsService : BackgroundService
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly ILogger<DailyStandingsService> _logger;
//    private readonly DiscordSocketClient _client;

//    public DailyStandingsService(
//        IServiceProvider serviceProvider,
//        ILogger<DailyStandingsService> logger,
//        DiscordSocketClient client)
//    {
//        _serviceProvider = serviceProvider;
//        _logger = logger;
//        _client = client;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                // Run daily at a specific time (e.g., 9 AM UTC)
//                var now = DateTime.UtcNow;
//                var nextRun = now.Date.AddDays(1).AddHours(9); // 9 AM next day
//                if (nextRun <= now)
//                {
//                    nextRun = nextRun.AddDays(1);
//                }

//                var delay = nextRun - now;
//                _logger.LogInformation("Daily standings update scheduled for {NextRun}", nextRun);

//                await Task.Delay(delay, stoppingToken);

//                if (!stoppingToken.IsCancellationRequested)
//                {
//                    await UpdateStandingsAsync();
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                // Expected when cancellation is requested
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in daily standings update");
//                // Wait 1 hour before retrying on error
//                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
//            }
//        }
//    }

//    private async Task UpdateStandingsAsync()
//    {
//        using var scope = _serviceProvider.CreateScope();
//        var standingsService = scope.ServiceProvider.GetRequiredService<StandingsService>();
//        var weekRepository = scope.ServiceProvider.GetRequiredService<WeekRepository>();
//        var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();

//        var currentWeek = await weekRepository.GetCurrentWeekAsync();
//        if (currentWeek == null)
//        {
//            _logger.LogInformation("No current week found, skipping standings update");
//            return;
//        }

//        // Get all text channels the bot has access to
//        // In a real implementation, you'd want to configure which channels to post to
//        foreach (var guild in _client.Guilds)
//        {
//            // Find a channel named "standings" or use a configured channel
//            var standingsChannel = guild.TextChannels.SingleOrDefault(c => c.Name.Contains("standings", StringComparison.OrdinalIgnoreCase));
//            if (standingsChannel == null)
//            {
//                continue;
//            }

//            try
//            {
//                var teamStandings = await standingsService.GetTeamStandingsAsync(currentWeek.Id);
//                var individualStandings = await standingsService.GetIndividualStandingsAsync(currentWeek.Id);
//                var deckStandings = await standingsService.GetDeckStandingsAsync(currentWeek.Id);
//                var progress = await standingsService.GetWeekProgressAsync(currentWeek.Id);

//                var content = FormatStandingsMessage(currentWeek.WeekNumber, teamStandings, individualStandings, deckStandings, progress);

//                await messageService.UpdateOrDeleteStandingsMessageAsync(standingsChannel, content);
//                _logger.LogInformation("Updated standings for Week {WeekNumber} in {Channel}", currentWeek.WeekNumber, standingsChannel.Name);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating standings in channel {Channel}", standingsChannel.Name);
//            }
//        }
//    }

//    private string FormatStandingsMessage(
//        int weekNumber,
//        List<TeamStanding> teamStandings,
//        List<IndividualStanding> individualStandings,
//        List<DeckStanding> deckStandings,
//        WeekProgress progress)
//    {
//        var message = $"# Daily Standings Update - Week {weekNumber}\n";
//        message += $"*Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*\n\n";

//        message += "## Team Standings (Top 10)\n";
//        message += "```\n";
//        message += "Rank | Team           | W  | L  | TB\n";
//        message += "-----|----------------|----|----|----\n";
//        foreach (var standing in teamStandings.Take(10))
//        {
//            message += $"{standing.Rank,4} | {standing.TeamName,-14} | {standing.Wins,2} | {standing.Losses,2} | {standing.TieBreaker:F3}\n";
//        }
//        message += "```\n\n";

//        message += "## Individual Standings (Top 10)\n";
//        message += "```\n";
//        message += "Rank | Player         | W  | L  | WR\n";
//        message += "-----|----------------|----|----|----\n";
//        foreach (var standing in individualStandings.Take(10))
//        {
//            message += $"{standing.Rank,4} | {standing.PlayerName,-14} | {standing.Wins,2} | {standing.Losses,2} | {standing.WinRate:F2}\n";
//        }
//        message += "```\n\n";

//        message += "## Deck Format Standings\n";
//        message += "```\n";
//        message += "Rank | Format    | W  | L  | WR\n";
//        message += "-----|-----------|----|----|----\n";
//        foreach (var standing in deckStandings)
//        {
//            message += $"{standing.Rank,4} | {standing.FormatName,-9} | {standing.Wins,2} | {standing.Losses,2} | {standing.WinRate:F2}\n";
//        }
//        message += "```\n\n";

//        message += $"## Week Progress\n";
//        message += $"**Matches:** {progress.CompletedMatches}/{progress.TotalMatches} ({progress.CompletionPercentage:F1}%)\n";

//        return message;
//    }
//}
