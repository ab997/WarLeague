using Discord;
using Discord.Interactions;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("match", "Match commands")]
[RequireRole("Admin")]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
public class MatchCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordApiHelperService _helperService;
    private readonly MatchService _matchService;

    public MatchCommands(
        DiscordApiHelperService helperService,
        MatchService matchService)
    {
        _helperService = helperService;
        _matchService = matchService;
    }

    [SlashCommand("generate-pairings", "Generate random player-vs-player pairings for the current week based on submitted decks")]
    public async Task GeneratePairingsAsync()
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        GeneratePairingsResult result = await _matchService.GeneratePairingsAsync(season.Id);

        if (!result.Success || result.Week is null || result.WeeklyMatchups is null || result.CreatedMatches is null)
        {
            await FollowupAsync(result.Message);
            return;
        }

        // Build embeds (Discord limits: 25 fields/embed, 10 embeds/message).
        var embeds = BuildPairingsEmbeds(season, result.Week, result.WeeklyMatchups, result.CreatedMatches.Count);
        await SendEmbedsInBatchesAsync(embeds);
    }

    

    

    private static List<Embed> BuildPairingsEmbeds(
        Season season,
        Week week,
        IReadOnlyList<WeeklyMatchup> matchupOutputs,
        int totalMatchesCreated)
    {
        var embeds = new List<Embed>();

        EmbedBuilder NewEmbed(int page) => new EmbedBuilder()
            .WithTitle(page == 1
                ? $"Week {week.WeekNumber} Pairings • Season {season.SeasonNumber}"
                : $"Week {week.WeekNumber} Pairings • Season {season.SeasonNumber} (page {page})")
            .WithColor(new Color(88, 101, 242))
            .WithDescription($"Generated {totalMatchesCreated} matches. Pairings are random among deck submitters.");

        int pageNumber = 1;
        var eb = NewEmbed(pageNumber);

        foreach (WeeklyMatchup wm in matchupOutputs
                     .OrderBy(x => x.TeamA.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(x => x.TeamB.Name, StringComparer.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();

            if (wm.Pairs.Count == 0)
            {
                sb.AppendLine("_No pairings (missing submissions on one or both teams)._");
            }
            else
            {
                for (int i = 0; i < wm.Pairs.Count; i++)
                {
                    var (p1, p2) = wm.Pairs[i];
                    sb.AppendLine($"{i + 1}. <@{p1.DiscordUserId}> vs <@{p2.DiscordUserId}>");
                }
            }

            if (wm.UnpairedA.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Unpaired ({wm.TeamA.Name}): {string.Join(", ", wm.UnpairedA.Select(p => $"<@{p.DiscordUserId}>"))}");
            }

            if (wm.UnpairedB.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Unpaired ({wm.TeamB.Name}): {string.Join(", ", wm.UnpairedB.Select(p => $"<@{p.DiscordUserId}>"))}");
            }

            string fieldValue = TrimToMaxChars(sb.ToString().Trim(), 1024);

            if (eb.Fields.Count >= 25)
            {
                embeds.Add(eb.Build());
                pageNumber++;
                eb = NewEmbed(pageNumber);
            }

            eb.AddField($"{wm.TeamA.Name} vs {wm.TeamB.Name}", fieldValue, inline: false);
        }

        embeds.Add(eb.Build());
        return embeds;
    }

    private static string TrimToMaxChars(string s, int maxChars)
    {
        if (string.IsNullOrEmpty(s)) return "_<empty>_";
        if (s.Length <= maxChars) return s;
        return s[..Math.Max(0, maxChars - 4)] + " …";
    }

    private async Task SendEmbedsInBatchesAsync(IReadOnlyList<Embed> embeds)
    {
        if (embeds.Count == 0)
        {
            await FollowupAsync("Nothing to show.");
            return;
        }

        // Discord allows up to 10 embeds per message.
        const int batchSize = 10;
        for (int i = 0; i < embeds.Count; i += batchSize)
        {
            var batch = embeds.Skip(i).Take(batchSize).ToArray();
            await FollowupAsync(embeds: batch);
        }
    }
}
