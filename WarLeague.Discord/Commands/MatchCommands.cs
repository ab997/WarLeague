using Discord;
using Discord.Interactions;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
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
    private readonly WeekRepository _weekRepository;
    private readonly TeamRepository _teamRepository;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    private readonly MatchRepository _matchRepository;

    public MatchCommands(
        DiscordApiHelperService helperService,
        WeekRepository weekRepository,
        TeamRepository teamRepository,
        PlayerSeasonTeamRepository playerSeasonTeamRepository,
        MatchRepository matchRepository)
    {
        _helperService = helperService;
        _weekRepository = weekRepository;
        _teamRepository = teamRepository;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _matchRepository = matchRepository;
    }

    [SlashCommand("generate-pairings", "Generate random player-vs-player pairings for the current week based on submitted decks")]
    public async Task GeneratePairingsAsync()
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        // Find the single week in SubmissionsClosed state for this season.
        Week? week;
        try
        {
            week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(season.Id, WeekStatus.SubmissionsClosed);
        }
        catch (InvalidOperationException)
        {
            await FollowupAsync("There are multiple weeks with status 'SubmissionsClosed' for the active season. Please ask an Admin to fix the week statuses.");
            return;
        }

        if (week is null)
        {
            await FollowupAsync("There is no week with status 'SubmissionsClosed' for the active season.");
            return;
        }

        var teams = await _teamRepository.GetBySeasonAsync(season.Id);
        if (teams.Count < 2)
        {
            await FollowupAsync("Need at least 2 teams to generate pairings.");
            return;
        }

        // Resolve the team-vs-team matchups for this week (deterministic round-robin based on WeekNumber).
        var teamMatchups = GetRoundRobinTeamMatchupsForWeek(teams, week.WeekNumber);
        if (teamMatchups.Count == 0)
        {
            await FollowupAsync("No team matchups available for this week (did everyone get a bye?).");
            return;
        }

        // Build quick lookup: player -> team for this season.
        var memberships = await _playerSeasonTeamRepository.GetBySeasonAsync(season.Id);
        var membershipByPlayerId = memberships
            .GroupBy(m => m.PlayerId)
            .ToDictionary(g => g.Key, g => g.First());

        // Determine which players submitted for this week, then group those by team.
        var submittedPlayerIds = week.DeckSubmissions
            .Select(ds => ds.PlayerId)
            .Distinct()
            .ToHashSet();

        var submittedMemberships = memberships
            .Where(m => submittedPlayerIds.Contains(m.PlayerId))
            .ToList();

        var submittedByTeamId = submittedMemberships
            .GroupBy(m => m.TeamId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Player).ToList());

        // Safety: don't generate duplicates if matches already exist for this week.
        var existingMatches = await _matchRepository.GetByWeekIdAsync(week.Id);
        if (existingMatches.Count > 0)
        {
            await FollowupAsync($"Matches already exist for week {week.WeekNumber}. Refusing to generate new pairings to avoid duplicates.");
            return;
        }

        var rng = new Random();
        var createdMatches = new List<Match>();

        // We will also build output per matchup.
        var matchupOutputs = new List<(Team a, Team b, List<(Player aPlayer, Player bPlayer)> pairs, List<Player> unpairedA, List<Player> unpairedB)>();

        foreach (var (teamA, teamB) in teamMatchups)
        {
            var listA = submittedByTeamId.TryGetValue(teamA.Id, out var aPlayers) ? aPlayers.ToList() : new List<Player>();
            var listB = submittedByTeamId.TryGetValue(teamB.Id, out var bPlayers) ? bPlayers.ToList() : new List<Player>();

            ShuffleInPlace(listA, rng);
            ShuffleInPlace(listB, rng);

            int pairCount = Math.Min(listA.Count, listB.Count);
            var pairs = new List<(Player, Player)>(capacity: pairCount);

            for (int i = 0; i < pairCount; i++)
            {
                var p1 = listA[i];
                var p2 = listB[i];
                pairs.Add((p1, p2));

                createdMatches.Add(new Match
                {
                    WeekId = week.Id,
                    Player1Id = p1.Id,
                    Player2Id = p2.Id,
                    Status = MatchStatus.Scheduled
                });
            }

            var unpairedA = listA.Skip(pairCount).ToList();
            var unpairedB = listB.Skip(pairCount).ToList();

            matchupOutputs.Add((teamA, teamB, pairs, unpairedA, unpairedB));
        }

        if (createdMatches.Count == 0)
        {
            await FollowupAsync("No pairings generated. Likely missing deck submissions for the teams playing this week.");
            return;
        }

        await _matchRepository.AddRangeAsync(createdMatches);

        // Move week to InProgress now that pairings are generated.
        week.Status = WeekStatus.InProgress;
        await _weekRepository.UpdateAsync(week);

        // Build embeds (Discord limits: 25 fields/embed, 10 embeds/message).
        var embeds = BuildPairingsEmbeds(season, week, matchupOutputs, createdMatches.Count);
        await SendEmbedsInBatchesAsync(embeds);
    }

    private static List<(Team a, Team b)> GetRoundRobinTeamMatchupsForWeek(IReadOnlyList<Team> teams, int weekNumber)
    {
        // Deterministic ordering so reruns yield same team matchups.
        var ordered = teams
            .OrderBy(t => t.Id)
            .ToList();

        // Round-robin "circle method". If odd, add a BYE.
        var bye = new Team { Id = -1, Name = "BYE" };
        if (ordered.Count % 2 == 1)
        {
            ordered.Add(bye);
        }

        int n = ordered.Count;
        if (n < 2) return new List<(Team, Team)>();

        int rounds = n - 1;
        int roundIndex = ((weekNumber - 1) % rounds + rounds) % rounds; // safe modulo

        // Start with round 0 arrangement, rotate to requested round.
        var arr = ordered.ToList();
        for (int r = 0; r < roundIndex; r++)
        {
            RotateRoundRobinInPlace(arr);
        }

        var matchups = new List<(Team, Team)>(capacity: n / 2);
        for (int i = 0; i < n / 2; i++)
        {
            var a = arr[i];
            var b = arr[n - 1 - i];
            if (a.Id == bye.Id || b.Id == bye.Id) continue;
            matchups.Add((a, b));
        }

        return matchups;
    }

    // Circle method rotation: keep index 0 fixed, rotate the rest.
    // Example [A, B, C, D] -> [A, D, B, C]
    private static void RotateRoundRobinInPlace(List<Team> arr)
    {
        if (arr.Count <= 2) return;

        var last = arr[^1];
        for (int i = arr.Count - 1; i >= 2; i--)
        {
            arr[i] = arr[i - 1];
        }
        arr[1] = last;
    }

    private static void ShuffleInPlace<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static List<Embed> BuildPairingsEmbeds(
        Season season,
        Week week,
        IReadOnlyList<(Team a, Team b, List<(Player aPlayer, Player bPlayer)> pairs, List<Player> unpairedA, List<Player> unpairedB)> matchupOutputs,
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

        foreach (var (teamA, teamB, pairs, unpairedA, unpairedB) in matchupOutputs
                     .OrderBy(x => x.a.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(x => x.b.Name, StringComparer.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();

            if (pairs.Count == 0)
            {
                sb.AppendLine("_No pairings (missing submissions on one or both teams)._");
            }
            else
            {
                for (int i = 0; i < pairs.Count; i++)
                {
                    var (p1, p2) = pairs[i];
                    sb.AppendLine($"{i + 1}. <@{p1.DiscordUserId}> vs <@{p2.DiscordUserId}>");
                }
            }

            if (unpairedA.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Unpaired ({teamA.Name}): {string.Join(", ", unpairedA.Select(p => $"<@{p.DiscordUserId}>"))}");
            }

            if (unpairedB.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Unpaired ({teamB.Name}): {string.Join(", ", unpairedB.Select(p => $"<@{p.DiscordUserId}>"))}");
            }

            string fieldValue = TrimToMaxChars(sb.ToString().Trim(), 1024);

            if (eb.Fields.Count >= 25)
            {
                embeds.Add(eb.Build());
                pageNumber++;
                eb = NewEmbed(pageNumber);
            }

            eb.AddField($"{teamA.Name} vs {teamB.Name}", fieldValue, inline: false);
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
