
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using Format = WarLeague.Core.Data.Entities.Format;

namespace WarLeague.Discord.Commands
{
    [Group("peep", "Peep commands")]
    public class PeepCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly SeasonRepository _seasonRepository;
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly FormatRepository _formatRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly DiscordPlayerService _playerService;
        private readonly DiscordApiHelperService _helperService;

        public PeepCommands(
            SeasonRepository seasonRepository,
            WeekRepository weekRepository,
            TeamRepository teamRepository,
            PlayerSeasonTeamRepository playerSeasonTeamRepository,
            DiscordPlayerService playerService,
            DiscordApiHelperService helperService,
            FormatRepository formatRepository)
        {
            _seasonRepository = seasonRepository;
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _playerService = playerService;
            _helperService = helperService;
            _formatRepository = formatRepository;
        }

        [SlashCommand("format-info", "Shows information about the current format and its seasons")]
        [EnsureChannelIsInFormatCategory]
        public async Task FormatInfoAsync(
            [Summary("include-rules", "Include a snippet of the stored rules JSON")] bool includeRules = false)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            var seasons = await _seasonRepository.GetAllByFormatAsync(format.Id);
            var sb = new StringBuilder();

            sb.AppendLine($"Format: {format.Name}");
            if (seasons.Any())
            {
                sb.AppendLine("Seasons:");
                foreach (var s in seasons.OrderBy(x => x.SeasonNumber))
                {
                    sb.AppendLine($"- Season {s.SeasonNumber}" + (s.Active ? " (active)" : string.Empty));
                }
            }
            else
            {
                sb.AppendLine("Seasons: none");
            }

            if (includeRules)
            {
                var rules = string.IsNullOrWhiteSpace(format.Rules) ? null : format.Rules!;
                if (rules is null)
                {
                    sb.AppendLine("Rules: <none>");
                }
                else
                {
                    // Keep output compact
                    const int max = 800;
                    var trimmed = rules.Length > max ? rules[..max] + " ..." : rules;
                    sb.AppendLine("Rules (snippet):");
                    sb.AppendLine($"```json\n{trimmed}\n```");
                }
            }

            // Include IDs for admins for deeper inspection
            if (_helperService.IsUserAdmin(Context))
            {
                sb.AppendLine();
                sb.AppendLine($"[Admin] FormatId: {format.Id}");
            }

            await FollowupAsync(sb.ToString());
        }

        [SlashCommand("season-current", "Shows the current active season in this format")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task SeasonCurrentAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            var sb = new StringBuilder();
            sb.AppendLine($"Active Season: {season.SeasonNumber}");
            sb.AppendLine($"Team modifications enabled: {(!season.DisableTeamModification ? "Yes" : "No")}");

            if (_helperService.IsUserAdmin(Context))
            {
                sb.AppendLine();
                sb.AppendLine($"[Admin] SeasonId: {season.Id} | FormatId: {season.Format.Id}");
            }

            await FollowupAsync(sb.ToString());
        }

        [SlashCommand("week", "Shows details about a specific week of the active season")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task WeekAsync(
            [Summary("week-number", "Week number")] int weekNumber)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, season.Id);
            if (week is null)
            {
                await FollowupAsync($"Week with number {weekNumber} does not exist.");
                return;
            }

            string fmt(DateTime d) => d.ToString("yyyy-MM-dd");
            var sb = new StringBuilder();
            sb.AppendLine($"Week {week.WeekNumber}");
            sb.AppendLine($"Start: {fmt(week.StartDate)}");
            sb.AppendLine($"End: {fmt(week.EndDate)}");
            sb.AppendLine($"Submissions close: {fmt(week.SubmissionsClosedDate!.Value)}");
            sb.AppendLine($"Status: {week.Status}");

            if (_helperService.IsUserAdmin(Context))
            {
                sb.AppendLine();
                sb.AppendLine($"[Admin] SeasonId: {season.Id}");
            }

            await FollowupAsync(sb.ToString());
        }

        [SlashCommand("my-team", "Shows your team (if any) in the active season")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task MyTeamAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
            Player you = await _playerService.EnsurePlayerExistsAsync(Context.User);

            var pst = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(you.Id, season.Id);
            if (pst is null || pst.Team is null)
            {
                await FollowupAsync("You are not a member of any team this season.");
                return;
            }

            await FollowupAsync(await BuildTeamDetailsAsync(season, pst.Team));
        }

        [SlashCommand("team", "Shows a team's details in the active season")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task TeamAsync(
            [Summary("team-name", "Name of the team")] string teamName)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, season.Id);
            if (team is null)
            {
                await FollowupAsync($"Team with name '{teamName}' not found.");
                return;
            }

            await FollowupAsync(await BuildTeamDetailsAsync(season, team));
        }

        [SlashCommand("rules", "Shows a snippet of the format rules JSON")]
        [EnsureChannelIsInFormatCategory]
        public async Task RulesAsync(
            [Summary("max-chars", "Max characters to show (50-2000)")] int maxChars = 800)
        {
            await DeferAsync(ephemeral: false);

            if (maxChars < 50) maxChars = 50;
            if (maxChars > 2000) maxChars = 2000;

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);
            if (string.IsNullOrWhiteSpace(format.Rules))
            {
                await FollowupAsync("No rules set for this format.");
                return;
            }

            var rules = format.Rules!;
            var trimmed = rules.Length > maxChars ? rules[..maxChars] + " ..." : rules;

            var sb = new StringBuilder();
            sb.AppendLine($"Format: {format.Name}");
            sb.AppendLine("Rules (snippet):");
            sb.AppendLine($"```json\n{trimmed}\n```");

            if (_helperService.IsUserAdmin(Context))
            {
                sb.AppendLine();
                sb.AppendLine($"[Admin] FormatId: {format.Id}");
            }

            await FollowupAsync(sb.ToString());
        }

        [SlashCommand("overview", "Shows all formats, seasons, teams and the players in each team")]
        public async Task OverviewAsync()
        {
            await DeferAsync(ephemeral: false);

            // Load everything we need with minimal roundtrips
            var formats = await _formatRepository.GetAllAsync();

            if (formats.Count == 0)
            {
                await FollowupAsync("No formats found.");
                return;
            }

            var embeds = new List<Embed>();

            foreach (var format in formats.OrderBy(f => f.Name))
            {
                var seasons = await _seasonRepository.GetAllByFormatAsync(format.Id);
                var orderedSeasons = seasons.OrderBy(s => s.SeasonNumber).ToList();

                if (orderedSeasons.Count == 0)
                {
                    embeds.Add(new EmbedBuilder()
                        .WithTitle($"Format: {format.Name}")
                        .WithDescription("No seasons yet.")
                        .WithColor(new Color(120, 120, 120))
                        .Build());
                    continue;
                }

                // We may need multiple embeds per format due to field/size limits.
                EmbedBuilder NewEmbedForFormat(int page) => new EmbedBuilder()
                    .WithTitle(page == 1 ? $"Format: {format.Name}" : $"Format: {format.Name} (page {page})")
                    .WithColor(new Color(88, 101, 242)); // Discord blurple

                var pageNumber = 1;
                var eb = NewEmbedForFormat(pageNumber);

                foreach (var season in orderedSeasons)
                {
                    var teams = await _teamRepository.GetBySeasonAsync(season.Id);
                    var memberships = await _playerSeasonTeamRepository.GetBySeasonAsync(season.Id);
                    var weeks = await _weekRepository.GetBySeasonAsync(season.Id);

                    var membersByTeamId = memberships
                        .Where(m => m.Team is not null)
                        .GroupBy(p => p.Team!.Id)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.Player).ToList());

                    var seasonTitle = season.Active
                        ? $"Season {season.SeasonNumber} • Active"
                        : $"Season {season.SeasonNumber}";

                    string seasonBody;
                    var lines = new List<string>(capacity: Math.Max(teams.Count + 3, 3));

                    // Weeks overview (compact to avoid embed limits)
                    if (weeks.Count == 0)
                    {
                        lines.Add("**Weeks:** _none_");
                    }
                    else
                    {
                        static string fmt(DateTime d) => d.ToString("MM-dd");

                        // Example: W1(Open 01-01→01-07), W2(SubmissionsClosed 01-08→01-14)
                        var weekParts = weeks
                            .OrderBy(w => w.WeekNumber)
                            .Select(w => $"W{w.WeekNumber}({w.Status} {fmt(w.StartDate)}→{fmt(w.EndDate)})")
                            .ToList();

                        // Keep it readable: if the joined string is too long, show a truncated prefix.
                        var joined = string.Join(", ", weekParts);
                        const int maxWeekLineChars = 950; // leave headroom for markdown and chunking
                        if (joined.Length > maxWeekLineChars)
                        {
                            var sbWeeks = new StringBuilder();
                            for (int wi = 0; wi < weekParts.Count; wi++)
                            {
                                var next = (sbWeeks.Length == 0 ? "" : ", ") + weekParts[wi];
                                if (sbWeeks.Length + next.Length > maxWeekLineChars)
                                {
                                    sbWeeks.Append(sbWeeks.Length == 0 ? "…" : ", …");
                                    break;
                                }
                                sbWeeks.Append(next);
                            }
                            joined = sbWeeks.ToString();
                        }

                        lines.Add($"**Weeks:** {joined}");
                    }

                    lines.Add(""); // spacer

                    if (teams.Count == 0)
                    {
                        lines.Add("_No teams yet._");
                    }
                    else
                    {
                        foreach (var team in teams.OrderBy(t => t.Name))
                        {
                            if (!membersByTeamId.TryGetValue(team.Id, out var players) || players.Count == 0)
                            {
                                lines.Add($"**{team.Name}** — _no players_");
                                continue;
                            }

                            var playerMentions = players
                                .Where(p => p is not null)
                                .Select(p => $"<@{p.DiscordUserId}>")
                                .Distinct()
                                .ToList();

                            lines.Add($"**{team.Name}** ({playerMentions.Count}): {string.Join(", ", playerMentions)}");
                        }
                    }

                    seasonBody = string.Join('\n', lines).Trim();

                    // Field values must be <= 1024 chars. Split the season into multiple fields if needed.
                    var fieldChunks = SplitIntoFieldChunks(seasonBody, maxChars: 1024);
                    for (int i = 0; i < fieldChunks.Count; i++)
                    {
                        var fieldName = i == 0 ? seasonTitle : $"{seasonTitle} (cont.)";
                        var fieldValue = fieldChunks[i];

                        // Discord embed limit: max 25 fields per embed.
                        if (eb.Fields.Count >= 25)
                        {
                            embeds.Add(eb.Build());
                            pageNumber++;
                            eb = NewEmbedForFormat(pageNumber);
                        }

                        eb.AddField(fieldName, fieldValue, inline: false);
                    }
                }

                embeds.Add(eb.Build());
            }

            await SendEmbedsInBatchesAsync(embeds);
        }

       

        private static List<string> SplitIntoFieldChunks(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string> { "_<empty>_" };
            }

            if (text.Length <= maxChars)
            {
                return new List<string> { text };
            }

            var chunks = new List<string>();
            int start = 0;
            while (start < text.Length)
            {
                int len = Math.Min(maxChars, text.Length - start);
                int end = start + len;

                // Prefer to break on newline so we don't split a team line.
                int lastNewLine = text.LastIndexOf('\n', end - 1, len);
                if (lastNewLine > start)
                {
                    end = lastNewLine + 1;
                }

                var part = text.Substring(start, end - start).Trim();
                if (part.Length == 0)
                {
                    part = "_…_";
                }

                chunks.Add(part);
                start = end;
            }

            return chunks;
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

        private async Task<string> BuildTeamDetailsAsync(Season season, Team team)
        {
            var memberships = await _playerSeasonTeamRepository.GetBySeasonAsync(season.Id);
            var players = memberships
                .Where(x => x.TeamId == team.Id)
                .Select(x => x.Player)
                .OrderBy(p => p.UserName)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Team: {team.Name}");
            sb.AppendLine($"Captain: <@{team.Captain.DiscordUserId}>");
            sb.AppendLine($"Created: {team.CreatedDate:yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine($"Players ({players.Count}):");

            if (players.Count == 0)
            {
                sb.AppendLine("- <none>");
            }
            else
            {
                foreach (var p in players)
                {
                    // Prefer stored username; include a mention for convenience
                    var name = string.IsNullOrWhiteSpace(p.UserName) ? $"Player #{p.Id}" : p.UserName;
                    sb.AppendLine($"- {name} (<@{p.DiscordUserId}>)");
                }
            }

            if (_helperService.IsUserAdmin(Context))
            {
                sb.AppendLine();
                sb.AppendLine($"[Admin] TeamId: {team.Id} | CaptainId: {team.CaptainId} | SeasonId: {season.Id}");
            }

            return sb.ToString();
        }
    }
}
