using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WarLeague.Core.Services;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;

namespace WarLeague.Test;

/// <summary>
/// Specifications for TiebreakerService: H2H among tied teams, series diff, multi-team tie, final fallback (Team.Id).
/// Uses simple inputs: teams, matchups, matches.
/// </summary>
public partial class Specifications
{
    [Fact]
    [Trait("Category", "Tiebreaker")]
    public async Task WhenTwoTeamsSameWlAndDifferentH2H_ThenHigherH2HRanksFirst()
    {
        var (_, teams) = await CreateFourTeamsTwoConferencesAsync();
        teams = teams.OrderBy(t => t.Id).ToList();
        var (a, b, c, d) = (teams[0], teams[1], teams[2], teams[3]);
        var matchups = new List<RoundRobinMatchup>
        {
            M(a.Id, b.Id, a.Id), M(a.Id, c.Id, a.Id), M(b.Id, c.Id, b.Id),
            M(a.Id, d.Id, d.Id), M(b.Id, d.Id, b.Id), M(c.Id, d.Id, c.Id)
        };
        var matches = new List<Match>(); // H2H alone determines order
        var tiebreaker = _serviceProvider.GetRequiredService<TiebreakerService>();

        var result = tiebreaker.RankTeams(teams, matchups, matches);

        result.OrderedTeams[0].Id.ShouldBe(a.Id);
        result.OrderedTeams[1].Id.ShouldBe(b.Id);
        result.OrderedTeams[2].Id.ShouldBe(c.Id);
        result.OrderedTeams[3].Id.ShouldBe(d.Id);
    }

    [Fact]
    [Trait("Category", "Tiebreaker")]
    public async Task WhenTwoTeamsSameWlAndH2HButDifferentSeriesDiff_ThenHigherSeriesDiffRanksFirst()
    {
        var (_, teams) = await CreateFourTeamsTwoConferencesAsync();
        teams = teams.OrderBy(t => t.Id).ToList();
        var (a, b, c, d) = (teams[0], teams[1], teams[2], teams[3]);
        var matchups = new List<RoundRobinMatchup> { M(a.Id, c.Id, a.Id), M(b.Id, d.Id, b.Id) };
        // A: 3 series W, 1 L (+2). B: 2 W, 1 L (+1).
        var matches = new List<Match>
        {
            MatchResult(a.Id, c.Id, a.Id), MatchResult(a.Id, c.Id, a.Id), MatchResult(a.Id, c.Id, a.Id), MatchResult(a.Id, c.Id, c.Id),
            MatchResult(b.Id, d.Id, b.Id), MatchResult(b.Id, d.Id, b.Id), MatchResult(b.Id, d.Id, d.Id)
        };
        var tiebreaker = _serviceProvider.GetRequiredService<TiebreakerService>();

        var result = tiebreaker.RankTeams(new[] { a, b }, matchups, matches);

        result.OrderedTeams[0].Id.ShouldBe(a.Id);
        result.OrderedTeams[1].Id.ShouldBe(b.Id);
    }

    [Fact]
    [Trait("Category", "Tiebreaker")]
    public async Task WhenTwoTeamsIdenticalOnAllMetrics_ThenLowerTeamIdRanksFirst()
    {
        var (_, teams) = await CreateFourTeamsTwoConferencesAsync();
        var (a, b) = (teams[0], teams[1]);
        var matchups = new List<RoundRobinMatchup>();
        var matches = new List<Match>();
        var tiebreaker = _serviceProvider.GetRequiredService<TiebreakerService>();

        var result = tiebreaker.RankTeams(new[] { a, b }, matchups, matches);

        result.OrderedTeams[0].Id.ShouldBe(a.Id);
        result.OrderedTeams[1].Id.ShouldBe(b.Id);
    }

    [Fact]
    [Trait("Category", "Tiebreaker")]
    public async Task WhenMultiTeamTie_ThenH2HSplitsThenSeriesDiffThenFallback()
    {
        var (_, teams) = await CreateFourTeamsTwoConferencesAsync();
        teams = teams.OrderBy(t => t.Id).ToList();
        var (a, b, c) = (teams[0], teams[1], teams[2]);
        var matchups = new List<RoundRobinMatchup>
        {
            M(a.Id, b.Id, a.Id), M(b.Id, c.Id, b.Id), M(a.Id, c.Id, c.Id)
        };
        // Series: A 2W 1L (+1), B 1W 1L (0), C 0W 2L (-1). A-B: B wins 1; A-C: A 2 C 0; B-C: C 1 B 0.
        var matches = new List<Match>
        {
            MatchResult(a.Id, b.Id, b.Id),
            MatchResult(a.Id, c.Id, a.Id), MatchResult(a.Id, c.Id, a.Id),
            MatchResult(b.Id, c.Id, c.Id)
        };
        var tiebreaker = _serviceProvider.GetRequiredService<TiebreakerService>();

        var result = tiebreaker.RankTeams(new[] { a, b, c }, matchups, matches);

        result.OrderedTeams[0].Id.ShouldBe(a.Id);
        result.OrderedTeams[1].Id.ShouldBe(b.Id);
        result.OrderedTeams[2].Id.ShouldBe(c.Id);
    }

    private static RoundRobinMatchup M(int t1, int t2, int winnerId)
    {
        return new RoundRobinMatchup
        {
            Team1Id = t1,
            Team2Id = t2,
            TeamWinnerId = winnerId,
            MatchupType = MatchupType.Normal,
            WeekId = 0
        };
    }

    private static Match MatchResult(int team1Id, int team2Id, int winnerTeamId)
    {
        return new Match
        {
            Team1Id = team1Id,
            Team2Id = team2Id,
            WinnerTeamId = winnerTeamId,
            Player1Wins = 2,
            Player2Wins = 1
        };
    }

    private async Task<(int seasonId, List<Team> teams)> CreateFourTeamsTwoConferencesAsync()
    {
        var (_, seasonId) = await CreateFormatAndSeason();
        await EnsureConferenceAsync(seasonId, "Default");
        var pid = 8000u;
        for (var i = 0; i < 4; i++)
        {
            var captain = await CreatePlayer(pid + (ulong)(i * 100));
            await CreateTeam(seasonId, $"T{i + 1}", captain.Id);
        }
        var teams = (await GetTeamsAsync(seasonId)).OrderBy(t => t.Id).ToList();
        return (seasonId, teams);
    }
}
