using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands
{
    [Group("peep", "Peep commands")]
    public class PeepCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly SeasonRepository _seasonRepository;
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly PlayerService _playerService;
        private readonly DiscordApiHelperService _helperService;

        public PeepCommands(
            SeasonRepository seasonRepository,
            WeekRepository weekRepository,
            TeamRepository teamRepository,
            PlayerSeasonTeamRepository playerSeasonTeamRepository,
            PlayerService playerService,
            DiscordApiHelperService helperService)
        {
            _seasonRepository = seasonRepository;
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _playerService = playerService;
            _helperService = helperService;
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
            sb.AppendLine($"Submissions close: {fmt(week.SubmissionsClosedDate)}");
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

            Team team = pst.Team;

            var sb = new StringBuilder();
            sb.AppendLine($"Team: {team.Name}");
            sb.AppendLine($"Captain set: {(team.CaptainId != 0 ? "Yes" : "No")}");
            sb.AppendLine($"Created: {team.CreatedDate:yyyy-MM-dd}");

            if (_helperService.IsUserAdmin(Context))
            {
                sb.AppendLine();
                sb.AppendLine($"[Admin] TeamId: {team.Id} | CaptainId: {team.CaptainId} | SeasonId: {season.Id}");
            }

            await FollowupAsync(sb.ToString());
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

            var sb = new StringBuilder();
            sb.AppendLine($"Team: {team.Name}");
            sb.AppendLine($"Captain set: {(team.CaptainId != 0 ? "Yes" : "No")}");
            sb.AppendLine($"Created: {team.CreatedDate:yyyy-MM-dd}");

            if (_helperService.IsUserAdmin(Context))
            {
                sb.AppendLine();
                sb.AppendLine($"[Admin] TeamId: {team.Id} | CaptainId: {team.CaptainId} | SeasonId: {season.Id}");
            }

            await FollowupAsync(sb.ToString());
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
            var formats = await _context.Formats
                .AsNoTracking()
                .OrderBy(f => f.Name)
                .ToListAsync();

            if (formats.Count == 0)
            {
                await FollowupAsync("No formats found.");
                return;
            }

            var sb = new StringBuilder();

            foreach (var format in formats)
            {
                sb.AppendLine($"Format: {format.Name}");

                var seasons = await _seasonRepository.GetAllByFormatAsync(format.Id);
                if (seasons.Count == 0)
                {
                    sb.AppendLine("  Seasons: none");
                    sb.AppendLine();
                    continue;
                }

                foreach (var season in seasons.OrderBy(s => s.SeasonNumber))
                {
                    sb.AppendLine($"  Season {season.SeasonNumber}" + (season.Active ? " (active)" : string.Empty));

                    // Teams in this season
                    var teams = await _context.Teams
                        .AsNoTracking()
                        .Where(t => t.Season.Id == season.Id)
                        .OrderBy(t => t.Name)
                        .ToListAsync();

                    // Memberships in this season (group by team)
                    var memberships = await _context.PlayerSeasonTeams
                        .AsNoTracking()
                        .Where(pst => pst.Season.Id == season.Id)
                        .Include(pst => pst.Team)
                        .Include(pst => pst.Player)
                        .ToListAsync();

                    var membersByTeamId = memberships
                        .GroupBy(p => p.Team.Id)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.Player).ToList());

                    if (teams.Count == 0)
                    {
                        sb.AppendLine("    Teams: none");
                        continue;
                    }

                    foreach (var team in teams)
                    {
                        sb.AppendLine($"    Team: {team.Name}");

                        if (!membersByTeamId.TryGetValue(team.Id, out var players) || players.Count == 0)
                        {
                            sb.AppendLine("      Players: none");
                        }
                        else
                        {
                            // We don't assume a display field on Player; list by Player.Id to be schema-safe
                            var playerList = string.Join(", ", players.Select(p => $"#{p.Id}"));
                            sb.AppendLine($"      Players ({players.Count}): {playerList}");
                        }
                    }

                    sb.AppendLine();
                }
            }

            await SendInChunksAsync(sb.ToString());
        }

        private async Task SendInChunksAsync(string content, int chunkSize = 1800)
        {
            if (string.IsNullOrEmpty(content))
            {
                await FollowupAsync("<empty>");
                return;
            }

            int start = 0;
            bool first = true;
            while (start < content.Length)
            {
                int length = Math.Min(chunkSize, content.Length - start);
                // Try to break on newline to keep things tidy
                int lastNewLine = content.LastIndexOf('\n', start + length - 1, length);
                if (lastNewLine > start && lastNewLine - start < length)
                {
                    length = lastNewLine - start + 1;
                }

                var part = content.Substring(start, length);
                if (first)
                {
                    await FollowupAsync(part);
                    first = false;
                }
                else
                {
                    await FollowupAsync(part);
                }

                start += length;
            }
        }
    }
}
