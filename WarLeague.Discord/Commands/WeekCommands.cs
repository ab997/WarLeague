using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Core.Services;
using WarLeague.Discord.Constants;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using static WarLeague.Discord.Helpers.ResultHelper;

namespace WarLeague.Discord.Commands
{
    [Group("week", "Week commands")]
    [RequireRole(DiscordRoleConstants.Admin)]
    [EnsureChannelIsInFormatCategory]
    [EnsureSingleActiveSeason]
    [EnsureValidTeams]
    public class WeekCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly WeekService _weekService;
        private readonly DiscordApiHelperService _helperService;
        private readonly MatchService _matchService;
        public WeekCommands(
            DiscordApiHelperService helperService,
            WeekService weekService,
            MatchService matchService)
        {
            _helperService = helperService;
            _weekService = weekService;
            _matchService = matchService;
        }
        [SlashCommand("create", "1 -> Creates a week (Status: null -> NotOpenYet)")]
        public async Task Create(int weekNumber,
            [Summary("submissions-required", "Number of submissions (players per team) required for the week")] int submissionsRequired, 
            [Summary("start-date", "Start date (YYYY-MM-DD)")] string startDateStr,
            [Summary("end-date", "End date (YYYY-MM-DD)")] string endDateStr,
            [Summary("submissions-close-date", "Submissions close date (YYYY-MM-DD)")] string subCloseDateStr
            )
        {
            await DeferAsync(ephemeral: false);

            if (!DateTime.TryParse(startDateStr, out var startDate) ||
                !DateTime.TryParse(endDateStr, out var endDate) ||
                !DateTime.TryParse(subCloseDateStr, out var subCloseDate))
            {
                await FollowupAsync("Invalid date format. Use YYYY-MM-DD.");
                return;
            }

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            BaseResult result = await _weekService.CreateAsync(season.Id, weekNumber, startDate, endDate, subCloseDate, submissionsRequired);

            await FollowupAsync(Stringify(result));
        }
        [SlashCommand("delete", "Deletes a week")]
        public async Task DeleteAsync(int weekNumber)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            BaseResult result = await _weekService.DeleteAsync(season.Id, weekNumber);

            await FollowupAsync(Stringify(result));
        }



        [SlashCommand("open", "2 -> Opens the current week by allowing deck submissions (Status: NotOpenYet -> Open)")]
        public async Task OpenAsync(int weekNumber)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            BaseResult result = await _weekService.TransitionToOpenWeekAsync(season.Id, weekNumber);

            await FollowupAsync(Stringify(result));
        }
        [SlashCommand("close-submissions", "3 -> Closes submissions for the week (Status: Open -> SubmissionsClosed)")]
        public async Task CloseSubmissionsAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            BaseResult result = await _weekService.TransitionToCloseSubmissionsAsync(season.Id);

            await FollowupAsync(Stringify(result));
        }

        [SlashCommand("generate-pairings", "4 -> Generate pairings (Status: SubmissionsClosed -> InProgress)")]
        public async Task GeneratePairingsAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            GeneratePairingsResult result = await _weekService.TransitionToInProgressAsync(season.Id);

            if (!result.Success || result.Week is null || result.WeeklyMatchups is null || result.CreatedMatches is null)
            {
                await FollowupAsync(result.Message);
                return;
            }

            // Build embeds (Discord limits: 25 fields/embed, 10 embeds/message).
            var embeds = BuildPairingsEmbeds(season, result.Week, result.WeeklyMatchups, result.CreatedMatches.Count);
            await SendEmbedsInBatchesAsync(embeds);
        }


        [SlashCommand("close", "5 -> Closes the current week after all matches are reported (Status: InProgress -> Completed)")]
        public async Task CloseAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            BaseResult result = await _weekService.TransitionToCompletedAsync(season.Id);

            await FollowupAsync(Stringify(result));
        }

        [SlashCommand("ping-players", "Tags players who need to finish their matches for the current week")]
        public async Task PingPlayersAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            var lines = await _weekService.GetPendingMatchPairsAsync(season.Id);

            if (lines.Count == 0)
            {
                await FollowupAsync("All matches are confirmed. No players to ping.");
                return;
            }

            var message = "Players needing to finish matches:\n" + string.Join("\n", lines);

            await FollowupAsync(message);
        }

        [SlashCommand("update", "Updates a week")]
        public async Task Update(int weekNumber,
           [Summary("submissions-required", "Number of submissions (players per team) required for the week")] int? submissionsRequired = null,
           [Summary("start-date", "Start date (YYYY-MM-DD)")] string? startDateStr = null,
           [Summary("end-date", "End date (YYYY-MM-DD)")] string? endDateStr = null,
           [Summary("submissions-close-date", "Submissions close date (YYYY-MM-DD)")] string? subCloseDateStr = null,
           [Summary("status")] WeekStatus? status = null
           )
        {
            try
            {
                await DeferAsync(ephemeral: false);

                // Parse provided dates (only when provided)
                DateTime? startDate = null;
                if (startDateStr is not null)
                {
                    if (!DateTime.TryParse(startDateStr, out var parsedStart))
                    {
                        await FollowupAsync("Invalid start date format. Use YYYY-MM-DD.");
                        return;
                    }
                    startDate = parsedStart;
                }

                DateTime? endDate = null;
                if (endDateStr is not null)
                {
                    if (!DateTime.TryParse(endDateStr, out var parsedEnd))
                    {
                        await FollowupAsync("Invalid end date format. Use YYYY-MM-DD.");
                        return;
                    }
                    endDate = parsedEnd;
                }

                DateTime? subCloseDate = null;
                if (subCloseDateStr is not null)
                {
                    if (!DateTime.TryParse(subCloseDateStr, out var parsedSubClose))
                    {
                        await FollowupAsync("Invalid submissions close date format. Use YYYY-MM-DD.");
                        return;
                    }
                    subCloseDate = parsedSubClose;
                }

                Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

                BaseResult result = await _weekService.UpdateAsync(season.Id, weekNumber, startDate, endDate, subCloseDate, status, submissionsRequired);

                await FollowupAsync(Stringify(result));
            }
            catch (DbUpdateException)
            {
                await FollowupAsync($"No but seriously, you are not allowed to put two different weeks of a single season to same status, " +
                    $"unless the status is {WeekStatus.Completed} or {WeekStatus.NotOpenYet}. Use /peep overview to get some help.");
            }
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
                    ? $"Week {week.WeekNumber} Pairings � Season {season.SeasonNumber}"
                    : $"Week {week.WeekNumber} Pairings � Season {season.SeasonNumber} (page {page})")
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
            return s[..Math.Max(0, maxChars - 4)] + " �";
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
}
