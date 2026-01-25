using Discord.Interactions;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("standings", "View standings")]
public class StandingsCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly StandingsService _standingsService;
    private readonly WeekRepository _weekRepository;

    public StandingsCommands(
        StandingsService standingsService,
        WeekRepository weekRepository)
    {
        _standingsService = standingsService;
        _weekRepository = weekRepository;
    }

    [SlashCommand("team", "View team standings")]
    public async Task TeamStandings(
        [Summary("week", "Week number (optional, uses current week if not specified)")] int? weekNumber = null)
    {
        var week = weekNumber.HasValue
            ? await _weekRepository.GetByWeekNumberAsync(weekNumber.Value, 1) // Assuming season 1
            : await _weekRepository.GetCurrentWeekAsync();

        if (week == null)
        {
            await RespondAsync("Week not found.", ephemeral: true);
            return;
        }

        var standings = await _standingsService.GetTeamStandingsAsync(week.Id);

        var response = $"**Team Standings - Week {week.WeekNumber}**\n\n";
        response += "```\n";
        response += "Rank | Team           | W  | L  | TB\n";
        response += "-----|----------------|----|----|----\n";

        foreach (var standing in standings.Take(20)) // Limit to top 20
        {
            response += $"{standing.Rank,4} | {standing.TeamName,-14} | {standing.Wins,2} | {standing.Losses,2} | {standing.TieBreaker:F3}\n";
        }

        response += "```";

        await RespondAsync(response, ephemeral: false);
    }

    [SlashCommand("individual", "View individual player standings")]
    public async Task IndividualStandings(
        [Summary("week", "Week number (optional, uses current week if not specified)")] int? weekNumber = null)
    {
        var week = weekNumber.HasValue
            ? await _weekRepository.GetByWeekNumberAsync(weekNumber.Value, 1) // Assuming season 1
            : await _weekRepository.GetCurrentWeekAsync();

        if (week == null)
        {
            await RespondAsync("Week not found.", ephemeral: true);
            return;
        }

        var standings = await _standingsService.GetIndividualStandingsAsync(week.Id);

        var response = $"**Individual Standings - Week {week.WeekNumber}**\n\n";
        response += "```\n";
        response += "Rank | Player         | Team           | W  | L  | WR\n";
        response += "-----|----------------|----------------|----|----|----\n";

        foreach (var standing in standings.Take(20)) // Limit to top 20
        {
            response += $"{standing.Rank,4} | {standing.PlayerName,-14} | {standing.TeamName,-14} | {standing.Wins,2} | {standing.Losses,2} | {standing.WinRate:F2}\n";
        }

        response += "```";

        await RespondAsync(response, ephemeral: false);
    }

    [SlashCommand("deck", "View deck format standings")]
    public async Task DeckStandings(
        [Summary("week", "Week number (optional, uses current week if not specified)")] int? weekNumber = null)
    {
        var week = weekNumber.HasValue
            ? await _weekRepository.GetByWeekNumberAsync(weekNumber.Value, 1) // Assuming season 1
            : await _weekRepository.GetCurrentWeekAsync();

        if (week == null)
        {
            await RespondAsync("Week not found.", ephemeral: true);
            return;
        }

        var standings = await _standingsService.GetDeckStandingsAsync(week.Id);

        var response = $"**Deck Format Standings - Week {week.WeekNumber}**\n\n";
        response += "```\n";
        response += "Rank | Format    | W  | L  | WR\n";
        response += "-----|-----------|----|----|----\n";

        foreach (var standing in standings)
        {
            response += $"{standing.Rank,4} | {standing.FormatName,-9} | {standing.Wins,2} | {standing.Losses,2} | {standing.WinRate:F2}\n";
        }

        response += "```";

        await RespondAsync(response, ephemeral: false);
    }

    [SlashCommand("week-progress", "View week progress")]
    public async Task WeekProgress(
        [Summary("week", "Week number (optional, uses current week if not specified)")] int? weekNumber = null)
    {
        var week = weekNumber.HasValue
            ? await _weekRepository.GetByWeekNumberAsync(weekNumber.Value, 1) // Assuming season 1
            : await _weekRepository.GetCurrentWeekAsync();

        if (week == null)
        {
            await RespondAsync("Week not found.", ephemeral: true);
            return;
        }

        var progress = await _standingsService.GetWeekProgressAsync(week.Id);

        var response = $"**Week {week.WeekNumber} Progress**\n\n";
        response += $"**Total Matches:** {progress.TotalMatches}\n";
        response += $"**Completed:** {progress.CompletedMatches}\n";
        response += $"**Pending:** {progress.PendingMatches}\n";
        response += $"**Completion:** {progress.CompletionPercentage:F1}%\n";

        await RespondAsync(response, ephemeral: false);
    }
}
