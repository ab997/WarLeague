using Discord;
using Discord.Interactions;
using System.Text;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Autocomplete;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("match", "Match commands")]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
[InitializeGuildContext]
public class MatchCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly MatchRepository _matchRepository;
    private readonly WeekRepository _weekRepository;
    private readonly DiscordApiHelperService _helperService;

    public MatchCommands(
        MatchRepository matchRepository,
        WeekRepository weekRepository,
        DiscordApiHelperService helperService)
    {
        _matchRepository = matchRepository;
        _weekRepository = weekRepository;
        _helperService = helperService;
    }

    [SlashCommand("list", "List all matches for a week (default: current in-progress week)")]
    public async Task ListAsync(
        [Summary("week-number", "Week number (default: current in-progress week)")]
        [Autocomplete(typeof(WeekNumberAutocompleteHandler))]
        int? weekNumber = null)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Week? week;

        if (weekNumber.HasValue)
        {
            week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber.Value, season.Id);
            if (week is null)
            {
                await FollowupAsync($"Week {weekNumber.Value} not found in this season.");
                return;
            }
        }
        else
        {
            week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(season.Id, WeekStatus.InProgress);
            if (week is null)
            {
                await FollowupAsync("No in-progress week. Specify a week number to list matches for that week.");
                return;
            }
        }

        var matches = await _matchRepository.GetByWeekIdAsync(week.Id);
        if (matches.Count == 0)
        {
            await FollowupAsync($"Week {week.WeekNumber}: no matches scheduled.");
            return;
        }

        var played = matches.Where(m => m.Status == MatchStatus.Reported).ToList();
        var pending = matches.Where(m => m.Status != MatchStatus.Reported).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"**Week {week.WeekNumber} — Matches**");
        sb.AppendLine();

        sb.AppendLine($"Played ({played.Count}):");
        if (played.Count == 0)
        {
            sb.AppendLine("- <none>");
        }
        else
        {
            foreach (var m in played)
            {
                var p1 = m.Player1 is null ? $"P#{m.Player1Id}" : $"<@{m.Player1.DiscordUserId}>";
                var p2 = m.Player2 is null ? $"P#{m.Player2Id}" : $"<@{m.Player2.DiscordUserId}>";
                var win = m.Winner is null ? "<unknown>" : $"<@{m.Winner.DiscordUserId}>";
                var score = m.Player1Wins.HasValue && m.Player2Wins.HasValue ? $" {m.Player1Wins}-{m.Player2Wins} " : " ";
                var replay = string.IsNullOrWhiteSpace(m.ReplayUrl) ? "" : $" | Replay: {m.ReplayUrl}";
                sb.AppendLine($"- {p1} vs {p2} →{score}Winner: {win}{replay}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Pending ({pending.Count}):");
        if (pending.Count == 0)
        {
            sb.AppendLine("- <none>");
        }
        else
        {
            foreach (var m in pending)
            {
                var p1 = m.Player1 is null ? $"P#{m.Player1Id}" : $"<@{m.Player1.DiscordUserId}>";
                var p2 = m.Player2 is null ? $"P#{m.Player2Id}" : $"<@{m.Player2.DiscordUserId}>";
                sb.AppendLine($"- {p1} vs {p2}");
            }
        }

        await FollowupAsync(sb.ToString(), flags: MessageFlags.SuppressEmbeds);
    }
}
