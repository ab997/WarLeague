
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Text;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Core.Services;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Discord.Autocomplete;
using WarLeague.Discord.Helpers;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using Format = WarLeague.Data.Entities.Format;

namespace WarLeague.Discord.Commands
{
    [Group("peep", "Information and display commands")]
    [InitializeGuildContext]
    public class PeepCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly SeasonRepository _seasonRepository;
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly FormatRepository _formatRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly MatchRepository _matchRepository;
        private readonly DiscordPlayerService _playerService;
        private readonly DiscordApiHelperService _helperService;
        private readonly TeamStandingsService _teamStandingsService;
        private readonly PlayoffBracketService _bracketService;
        private readonly MatchupServiceFactory _matchupServiceFactory;

        public PeepCommands(
            SeasonRepository seasonRepository,
            WeekRepository weekRepository,
            TeamRepository teamRepository,
            PlayerSeasonTeamRepository playerSeasonTeamRepository,
            MatchRepository matchRepository,
            DiscordPlayerService playerService,
            DiscordApiHelperService helperService,
            FormatRepository formatRepository,
            TeamStandingsService teamStandingsService,
            PlayoffBracketService bracketService,
            MatchupServiceFactory matchupServiceFactory)
        {
            _seasonRepository = seasonRepository;
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _matchRepository = matchRepository;
            _playerService = playerService;
            _helperService = helperService;
            _formatRepository = formatRepository;
            _teamStandingsService = teamStandingsService;
            _bracketService = bracketService;
            _matchupServiceFactory = matchupServiceFactory;
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
                    var phaseText = s.Phase == WarLeague.Data.Enums.SeasonPhase.Playoffs ? "Playoffs" : "Round Robin";
                    var activeText = s.Active ? " (active)" : string.Empty;
                    sb.AppendLine($"- Season {s.SeasonNumber}{activeText} • {phaseText}");
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

            var eb = new EmbedBuilder()
                .WithTitle("Format information")
                .WithDescription(sb.ToString().TrimEnd())
                .WithColor(new Color(88, 101, 242));

            await FollowupAsync(embeds: new[] { eb.Build() });
        }

        [SlashCommand("admin-help", "Administrator operational guide")]
        [RequireAppPermission(PermissionType.Admin)]
        public async Task AdminHelpAsync()
        {
            await DeferAsync(ephemeral: false);

            var eb = new EmbedBuilder()
                .WithTitle("Administrator Operational Guide")
                .WithColor(new Color(88, 101, 242));

            eb.AddField("Scope",
                "- Admin-only actions\n- Multiple formats supported\n- Actions executed inside a format category are implicitly scoped to that format",
                inline: false);

            eb.AddField("Formats",
                "- Create a format first\n- Creating a format automatically provisions a category named after the format and an initial channel inside it\n- Perform all actions (seasons, weeks, matches) inside the format's category; actions are scoped to that format" +
                "\n- Alternatively, use a special command to designate a single format per server. Every command will be assumed to reference only that format.",
                inline: false);

            eb.AddField("Seasons",
                "- Seasons are created manually and identified by a number\n- A new season starts inactive\n- An admin must explicitly activate a season\n- Only one season per format can be active at a time",
                inline: false);

            eb.AddField("Weeks",
                "- Weeks are created manually inside a season\n- A season may contain multiple weeks\n- Exactly one active week per season is allowed at any time\n- Active = any status except NotOpenYet\n- Multiple Completed weeks may exist; only one working week (Open, SubmissionsClosed, or InProgress) is allowed\n- Application validations enforce this rule",
                inline: false);

            eb.AddField(
                "Week Statuses and Allowed Actions",
                @"- NotOpenYet
  • Week exists; no actions are allowed yet

- Open
  • Players and captains can prepare lineups; deck submissions are permitted

- SubmissionsClosed
  • All teams have submitted
  • Deck changes are no longer allowed

- InProgress
  • Pairings have been generated
  • Players must report match results
  • Reporting is only allowed during InProgress
  • After pairings are generated, each player may report results only for their own single open match
  • Admins may report on behalf of any player with an open match
  • Players report their own losses

- Completed
  • Week is finished",
                inline: false);

            eb.AddField("Recommended Lifecycle Example",
                "1) Create format\n2) Create season\n3) Activate season\n4) Create weeks\n5) Open a week (enable deck submissions)\n6) Close submissions (lock decks)\n7) Generate pairings (move week to InProgress)\n8) Players report matches (only their own, only losses)\n9) Complete the week\n10) Repeat for the next week",
                inline: false);

            eb.AddField(
                "From Round Robin to Playoffs",
                "1) Finish round-robin: all intended weeks must be **Completed** (check with `/week list` or `/peep overview`).\n" +
                "2) Set playoff slots: in each conference that should send teams to playoffs, set a non-zero `playoff-teams` via `/conference create` or `/conference update` (at least one conference is required).\n" +
                "3) Switch phase: in the format's category, run `/season switch-to-playoffs` (one-way; locks round-robin results, generates standings, and marks playoff qualifiers).\n" +
                "4) Run playoffs: create playoff weeks with `/week create`, then use `/week open` → `/week close-submissions` → `/week generate-pairings` → `/week close`, and view `/peep bracket` for the bracket.",
                inline: false);

            await FollowupAsync(embeds: new[] { eb.Build() });
        }

        [SlashCommand("standings-round-robin", "Show current round-robin standings (W–L per team)")]
        [RequireAppPermission(PermissionType.Admin)]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task StandingsRoundRobinAsync(
            [Summary("per-conference", "Group standings by conference")] bool perConference = false)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
            var standings = await _teamStandingsService.GetStandingsForSeasonAsync(season.Id);
        
            if (standings.Count == 0)
            {
                var embed = BuildInfoEmbed(
                    "No standings snapshot",
                    "No standings snapshot exists for this season. Complete the round-robin weeks and run `/standings generate` first.");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }
        
            var statsByTeamId = await _teamStandingsService.GetDisplayStatsForSeasonAsync(season.Id);
            var liveEntries = await _teamStandingsService.GetRoundRobinStandingsForDisplayAsync(season.Id);
            var liveByTeamId = liveEntries.ToDictionary(e => e.TeamId);
        
            var combinedEntries = standings.Select(s =>
            {
                liveByTeamId.TryGetValue(s.TeamId, out var live);
                var stats = statsByTeamId.GetValueOrDefault(s.TeamId);
                return new RoundRobinStandingsEntry
                {
                    TeamId = s.TeamId,
                    TeamName = s.Team?.Name ?? live?.TeamName ?? $"Team #{s.TeamId}",
                    ConferenceName = live?.ConferenceName ?? string.Empty,
                    Tiebreaker = s.Tiebreaker,
                    Wins = stats.Wins,
                    Losses = stats.Losses
                };
            }).ToList();
        
            var entries = combinedEntries
                .OrderByDescending(e => e.Tiebreaker)
                .ToList();

            if (entries.Count == 0)
            {
                var embed = BuildInfoEmbed(
                    "No round-robin standings",
                    "No teams or no completed round-robin weeks yet. Standings will appear after weeks are completed.");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }

            const int maxFieldValueLength = 1024;
            var embeds = new List<Embed>();

            if (perConference)
            {
                var byConference = entries
                    .Where(e => !string.IsNullOrEmpty(e.ConferenceName))
                    .GroupBy(e => e.ConferenceName)
                    .OrderBy(g => g.Key)
                    .ToList();

                if (byConference.Count == 0)
                {
                    var body = FormatStandingsTable(entries);
                    var eb = new EmbedBuilder()
                        .WithTitle("Round-robin standings")
                        .WithDescription("All teams (no conference set)")
                        .WithColor(new Color(88, 101, 242));
                    if (body.Length <= maxFieldValueLength)
                        eb.AddField("W–L", body, inline: false);
                    else
                        SplitStandingsField(eb, "W–L", body, maxFieldValueLength);
                    embeds.Add(eb.Build());
                }
                else
                {
                    foreach (var group in byConference)
                    {
                        var list = group.ToList();
                        var body = FormatStandingsTable(list);
                        var eb = new EmbedBuilder()
                            .WithTitle($"Standings — {group.Key}")
                            .WithColor(new Color(88, 101, 242));
                        if (body.Length <= maxFieldValueLength)
                            eb.AddField("W–L", body, inline: false);
                        else
                            SplitStandingsField(eb, "W–L", body, maxFieldValueLength);
                        embeds.Add(eb.Build());
                    }
                }
            }
            else
            {
                var body = FormatStandingsTable(entries);
                var eb = new EmbedBuilder()
                    .WithTitle("Round-robin standings")
                    .WithColor(new Color(88, 101, 242));
                if (body.Length <= maxFieldValueLength)
                    eb.AddField("W–L", body, inline: false);
                else
                    SplitStandingsField(eb, "W–L", body, maxFieldValueLength);
                embeds.Add(eb.Build());
            }

            await _helperService.SendEmbedsInBatchesAsync(Context, embeds);
        }

        [SlashCommand("bracket", "Show playoff bracket (matchups, winners, advancement)")]
        [RequireAppPermission(PermissionType.Admin)]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task BracketAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
            var matchups = await _bracketService.GetBracketAsync(season.Id);

            if (matchups.Count == 0)
            {
                var embed = BuildInfoEmbed(
                    "No playoff bracket data",
                    "No playoff bracket data for this season. Switch to playoffs and generate pairings for at least one playoff week to see the bracket.");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }

            var byRound = matchups
                .GroupBy(m => (m.Round, m.WeekNumber))
                .OrderBy(g => g.Key.WeekNumber)
                .ThenBy(g => g.Key.Round)
                .ToList();

            // Determine overall bracket size (top cut) from all unique teams,
            // then step it down per playoff week (Top 8 -> Top 4 -> Finals).
            var uniqueTeamsCount = matchups
                .SelectMany(m => new[] { m.Team1Name, m.Team2Name })
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            int bracketSize = 1;
            while (bracketSize < uniqueTeamsCount)
                bracketSize *= 2;

            const int maxFieldValueLength = 1024;
            var embeds = new List<Embed>();
            int remainingBracketSize = bracketSize;

            foreach (var group in byRound)
            {
                string title = remainingBracketSize switch
                {
                    >= 8 => $"Top {remainingBracketSize} bracket",
                    4 => "Top 4 bracket",
                    2 => "Finals bracket",
                    _ => "Playoff bracket"
                };

                var roundLabel = $"Round {group.Key.Round} (Week {group.Key.WeekNumber})";
                var lines = new List<string>();
                foreach (var m in group.OrderBy(x => x.BracketPosition))
                {
                    if (m.IsBye)
                        lines.Add($"• **{m.Team1Name}** (BYE)" + (m.WinnerName != null ? $" → **{m.WinnerName}**" : ""));
                    else
                        lines.Add($"• **{m.Team1Name}** vs **{m.Team2Name}**" + (m.WinnerName != null ? $" → **{m.WinnerName}**" : ""));
                }
                var body = string.Join("\n", lines);
                var eb = new EmbedBuilder()
                    .WithTitle(title)
                    .WithColor(new Color(88, 101, 242));
                if (body.Length <= maxFieldValueLength)
                    eb.AddField(roundLabel, body, inline: false);
                else
                    SplitStandingsField(eb, roundLabel, body, maxFieldValueLength);
                embeds.Add(eb.Build());

                if (remainingBracketSize > 2)
                    remainingBracketSize /= 2;
            }

            await _helperService.SendEmbedsInBatchesAsync(Context, embeds);
        }

        [SlashCommand("week-results", "Shows played and pending matches for the current week")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task WeekResultsAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            // Prefer InProgress; fallback to SubmissionsClosed
            Week? week = await GetActiveOrClosedWeekAsync(season.Id);
            if (week is null)
            {
                var embed = BuildInfoEmbed(
                    "No active week",
                    "No active week found (neither InProgress nor SubmissionsClosed).");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }

            var body = await BuildWeekResultsAsync(week.Id, week.WeekNumber);

            if (_helperService.IsUserAdmin(Context))
            {
                body += $"\n[Admin] SeasonId: {season.Id} | WeekId: {week.Id}";
            }

            var eb = new EmbedBuilder()
                .WithTitle($"Week {week.WeekNumber} results")
                .WithDescription(body.TrimEnd())
                .WithColor(new Color(88, 101, 242));

            await FollowupAsync(embeds: new[] { eb.Build() });
        }

        [SlashCommand("team-pairings", "Shows team-vs-team pairings (and byes) for a week or all weeks")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task TeamPairingsAsync(
            [Summary("week-number", "Week number to inspect (default: all weeks)")]
            [Autocomplete(typeof(WeekNumberAutocompleteHandler))]
            int? weekNumber = null)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
            var teams = await _teamRepository.GetBySeasonAsync(season.Id);
            if (teams.Count < 2)
            {
                var embed = BuildInfoEmbed(
                    "Not enough teams",
                    "Not enough teams in this season to have pairings.");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }

            var matchupService = _matchupServiceFactory.GetMatchupService(season);
            var sb = new StringBuilder();

            if (weekNumber.HasValue)
            {
                var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber.Value, season.Id);
                if (week is null)
                {
                    var embed = BuildInfoEmbed(
                        "Week not found",
                        $"Week {weekNumber.Value} not found in this season.");
                    await FollowupAsync(embeds: new[] { embed });
                    return;
                }

                var conferenceGroups = teams.GroupBy(x => x.Conference.Name);
                foreach (var group in conferenceGroups)
                {
                    sb.AppendLine($"** Conference {group.Key} **");
                    await AppendPairingsForWeekAsync(sb, week, group.ToList(), matchupService);
                }

                if (sb.Length == 0)
                {
                    var embed = BuildInfoEmbed(
                        "No team pairings",
                        $"No team-vs-team pairings exist for week {week.WeekNumber}. If this is a round-robin week, open the week or generate the round-robin schedule first.");
                    await FollowupAsync(embeds: new[] { embed });
                    return;
                }

                var eb = new EmbedBuilder()
                    .WithTitle($"Week {week.WeekNumber} — Team pairings")
                    .WithDescription(sb.ToString().TrimEnd())
                    .WithColor(new Color(88, 101, 242));

                await FollowupAsync(embeds: new[] { eb.Build() });
                return;
            }

            var conferenceGroups2 = teams.GroupBy(x => x.Conference.Name);
            foreach (var group in conferenceGroups2)
            {
                sb.AppendLine($"** Conference {group.Key} **");
                var weeks = await _weekRepository.GetBySeasonAsync(season.Id);
                foreach (var week in weeks.OrderBy(w => w.WeekNumber))
                {
                    var before = sb.Length;
                    await AppendPairingsForWeekAsync(sb, week, group.ToList(), matchupService);
                    if (sb.Length > before)
                    {
                        sb.AppendLine();
                    }
                }
            }

           

            if (sb.Length == 0)
            {
                var embed = BuildInfoEmbed(
                    "No team pairings",
                    "No team-vs-team pairings exist for any week in this season.");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }

            var allEb = new EmbedBuilder()
                .WithTitle("Team pairings — all weeks")
                .WithDescription(sb.ToString().TrimEnd())
                .WithColor(new Color(88, 101, 242));

            await FollowupAsync(embeds: new[] { allEb.Build() });
        }

        [SlashCommand("season-current", "Shows the current active season in this format")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task SeasonCurrentAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            var sb = new StringBuilder();
            var phaseText = season.Phase == WarLeague.Data.Enums.SeasonPhase.Playoffs ? "Playoffs" : "Round Robin";
            sb.AppendLine($"Active Season: {season.SeasonNumber} • {phaseText}");
            sb.AppendLine($"Team modifications enabled: {(!season.DisableTeamModification ? "Yes" : "No")}");

            if (_helperService.IsUserAdmin(Context))
            {
                sb.AppendLine();
                sb.AppendLine($"[Admin] SeasonId: {season.Id} | FormatId: {season.Format.Id}");
            }

            var eb = new EmbedBuilder()
                .WithTitle("Current season")
                .WithDescription(sb.ToString().TrimEnd())
                .WithColor(new Color(88, 101, 242));

            await FollowupAsync(embeds: new[] { eb.Build() });
        }

        [SlashCommand("week", "Shows details about a specific week of the active season")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task WeekAsync(
            [Summary("week-number", "Week number")][Autocomplete(typeof(WeekNumberAutocompleteHandler))] int weekNumber)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, season.Id);
            if (week is null)
            {
                var embed = BuildInfoEmbed(
                    "Week not found",
                    $"Week with number {weekNumber} does not exist.");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }

            string fmt(DateTime? d) => d.HasValue ? d.Value.ToString("yyyy-MM-dd") : "—";
            var sb = new StringBuilder();
            sb.AppendLine($"Week {week.WeekNumber}");
            sb.AppendLine($"Start: {fmt(week.StartDate)}");
            sb.AppendLine($"End: {fmt(week.EndDate)}");
            sb.AppendLine($"Submissions close: {fmt(week.SubmissionsClosedDate)}");
            sb.AppendLine($"Status: {week.Status}");

            if (_helperService.IsUserAdmin(Context))
            {
                sb.AppendLine();
                sb.AppendLine($"[Admin] SeasonId: {season.Id}");
            }

            var eb = new EmbedBuilder()
                .WithTitle($"Week {week.WeekNumber}")
                .WithDescription(sb.ToString().TrimEnd())
                .WithColor(new Color(88, 101, 242));

            await FollowupAsync(embeds: new[] { eb.Build() });
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
                var embed = BuildInfoEmbed(
                    "No team found",
                    "You are not a member of any team this season.");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }

            var body = await BuildTeamDetailsAsync(season, pst.Team);
            var eb = new EmbedBuilder()
                .WithTitle($"Team: {pst.Team.Name}")
                .WithDescription(body.TrimEnd())
                .WithColor(new Color(88, 101, 242));

            await FollowupAsync(embeds: new[] { eb.Build() });
        }

        [SlashCommand("team", "Shows a team's details in the active season")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task TeamAsync(
            [Summary("team-name", "Name of the team")][Autocomplete(typeof(TeamAutocompleteHandler))] string teamName)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, season.Id);
            if (team is null)
            {
                var embed = BuildInfoEmbed(
                    "Team not found",
                    $"Team with name '{teamName}' not found.");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }

            var body = await BuildTeamDetailsAsync(season, team);
            var eb = new EmbedBuilder()
                .WithTitle($"Team: {team.Name}")
                .WithDescription(body.TrimEnd())
                .WithColor(new Color(88, 101, 242));

            await FollowupAsync(embeds: new[] { eb.Build() });
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
                var embed = BuildInfoEmbed(
                    "No rules set",
                    "No rules set for this format.");
                await FollowupAsync(embeds: new[] { embed });
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

            var eb = new EmbedBuilder()
                .WithTitle("Format rules")
                .WithDescription(sb.ToString().TrimEnd())
                .WithColor(new Color(88, 101, 242));

            await FollowupAsync(embeds: new[] { eb.Build() });
        }

        [SlashCommand("overview", "Shows all formats, seasons, teams and the players in each team")]
        public async Task OverviewAsync()
        {
            await DeferAsync(ephemeral: false);

            // Load everything we need with minimal roundtrips
            var formats = await _formatRepository.GetAllAsync();

            if (formats.Count == 0)
            {
                var embed = BuildInfoEmbed(
                    "No formats found",
                    "No formats found. Use the appropriate admin command to create a format first.");
                await FollowupAsync(embeds: new[] { embed });
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

                    var phaseText = season.Phase == WarLeague.Data.Enums.SeasonPhase.Playoffs ? "Playoffs" : "Round Robin";
                    var seasonTitle = season.Active
                        ? $"Season {season.SeasonNumber} • Active • {phaseText}"
                        : $"Season {season.SeasonNumber} • {phaseText}";

                    string seasonBody;
                    var lines = new List<string>(capacity: Math.Max(teams.Count + 3, 3));

                    // Weeks overview (compact to avoid embed limits)
                    if (weeks.Count == 0)
                    {
                        lines.Add("**Weeks:** _none_");
                    }
                    else
                    {
                        static string fmt(DateTime? d) => d.HasValue ? d.Value.ToString("MM-dd") : "—";

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
                                .Select(p => $"{p.UserName}")
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

        [SlashCommand("all-results", "Shows all results for the active season")]
        [EnsureChannelIsInFormatCategory]
        [EnsureSingleActiveSeason]
        public async Task AllResultsAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
            var weeks = await _weekRepository.GetBySeasonAsync(season.Id);

            if (weeks.Count == 0)
            {
                var embed = BuildInfoEmbed(
                    "No weeks found",
                    "No weeks found for the active season.");
                await FollowupAsync(embeds: new[] { embed });
                return;
            }

            var orderedWeeks = weeks.OrderBy(w => w.WeekNumber).ToList();
            var embeds = new List<Embed>();

            for (int i = 0; i < orderedWeeks.Count; i++)
            {
                var w = orderedWeeks[i];
                var body = await BuildWeekResultsAsync(w.Id, w.WeekNumber);

                if (i == orderedWeeks.Count - 1 && _helperService.IsUserAdmin(Context))
                {
                    body += $"\n[Admin] SeasonId: {season.Id}";
                }

                var eb = new EmbedBuilder()
                    .WithTitle($"Week {w.WeekNumber} results")
                    .WithDescription(body.TrimEnd())
                    .WithColor(new Color(88, 101, 242));

                embeds.Add(eb.Build());
            }

            await SendEmbedsInBatchesAsync(embeds);

        }

        private static async Task AppendPairingsForWeekAsync(
            StringBuilder sb,
            Week week,
            IReadOnlyList<Team> teams,
            IMatchupService matchupService)
        {
            List<(Team a, Team b)>? teamMatchups = await matchupService.GetExistingTeamMatchupsAsync(week.Id);

            if (teamMatchups is null || teamMatchups.Count == 0)
                return;

            teamMatchups = teamMatchups
                .Where(x => teams.Select(t => t.Id).Contains(x.a.Id) || teams.Select(t => t.Id).Contains(x.b.Id))
                .ToList();




            var byeTeams = await matchupService.GetByeTeamsForPairingsDisplayAsync(week.Id);

            sb.AppendLine($"Week {week.WeekNumber} — Team pairings");
            sb.AppendLine();

            foreach ((Team? a, Team? b) in teamMatchups)
            {
                if (a.Id == b.Id)
                    continue;

                sb.AppendLine($"- {a.Name} vs {b.Name}");
            }

            if (byeTeams.Count > 0)
            {
                var byeNames = string.Join(", ",
                    byeTeams
                        .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(t => t.Name));
                sb.AppendLine();
                sb.AppendLine($"Byes (auto-advance): {byeNames}");
            }

            sb.AppendLine();
        }

        private async Task<string> BuildWeekResultsAsync(int weekId, int weekNumber)
        {
            List<Match> matches = await _matchRepository.GetByWeekIdAsync(weekId);

            if (matches.Count == 0)
            {
                return "No matches scheduled for this week.";
            }

            IEnumerable<IGrouping<string, Match>> conferenceGroups = matches.GroupBy(x => x.Team1.Conference.Name);
            var sb = new StringBuilder();
            foreach (IGrouping<string, Match> conferenceGroup in conferenceGroups)
            {
                var played = conferenceGroup.Where(m => m.Status == MatchStatus.Reported).ToList();
                var pending = conferenceGroup.Where(m => m.Status != MatchStatus.Reported).ToList();

                sb.AppendLine($"** Conference {conferenceGroup.Key}**");
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
                        var replay = string.IsNullOrWhiteSpace(m.ReplayUrl) ? "" : $" | Replay: {m.ReplayUrl}";
                        sb.AppendLine($"- {p1} vs {p2} → Winner: {win}{replay}");
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
            }

            

            return sb.ToString().TrimEnd();
        }

        private async Task<Week?> GetActiveOrClosedWeekAsync(int seasonId)
        {
            Week? week;
            try
            {
                week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            if (week is null)
            {
                try
                {
                    week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.SubmissionsClosed);
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }

            return week;
        }

        private static string FormatStandingsTable(IReadOnlyList<RoundRobinStandingsEntry> entries)
        {
            var lines = entries.Select((e, i) =>
            {
                var rank = (i + 1).ToString();
                return $"{rank}. **{e.TeamName}** — {e.Wins}–{e.Losses} (TB: {e.Tiebreaker})";
            });
            return string.Join("\n", lines);
        }

        private static void SplitStandingsField(EmbedBuilder eb, string fieldName, string body, int maxLen)
        {
            var chunk = new List<char>(maxLen);
            var chunks = new List<string>();
            foreach (var line in body.Split('\n'))
            {
                var lineWithNewline = line + "\n";
                if (chunk.Count + lineWithNewline.Length > maxLen && chunk.Count > 0)
                {
                    chunks.Add(new string(chunk.ToArray()).TrimEnd());
                    chunk.Clear();
                }
                foreach (var c in lineWithNewline)
                    chunk.Add(c);
            }
            if (chunk.Count > 0)
                chunks.Add(new string(chunk.ToArray()).TrimEnd());
            for (int i = 0; i < chunks.Count; i++)
                eb.AddField(i == 0 ? fieldName : $"{fieldName} (cont.)", chunks[i], inline: false);
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

        private static Embed BuildInfoEmbed(string title, string description)
        {
            return new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(new Color(88, 101, 242))
                .Build();
        }

        private static Embed BuildErrorEmbed(string title, string description)
        {
            return new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(new Color(240, 71, 71))
                .Build();
        }

        private async Task SendEmbedsInBatchesAsync(IReadOnlyList<Embed> embeds)
        {
            await _helperService.SendEmbedsInBatchesAsync(Context, embeds);
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

            return sb.ToString().TrimEnd();
        }
    }
}
