using Shouldly;
using WarLeague.Core.Services;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;

namespace WarLeague.Test;

/// <summary>
/// Specifications for TiebreakerService as a black-box:
/// it produces a numeric tiebreaker score per team that can be
/// used for ordering, without asserting any particular algorithm.
/// </summary>
public partial class Specifications
{
    [Fact]
    [Trait("Category", "Tiebreaker")]
    public void WhenOneTeamHasMoreWins_ThenItGetsHigherTiebreaker()
    {
        // Arrange
        var teams = new List<Team>
        {
            new Team { Id = 1, Name = "A" },
            new Team { Id = 2, Name = "B" }
        };

        var matchups = new List<RoundRobinMatchup>
        {
            new RoundRobinMatchup
            {
                Team1Id = 1,
                Team2Id = 2,
                TeamWinnerId = 1,
                MatchupType = MatchupType.Normal,
                WeekId = 1
            }
        };

        var matches = new List<Match>
        {
            new Match
            {
                Team1Id = 1,
                Team2Id = 2,
                WinnerTeamId = 1,
                Player1Wins = 2,
                Player2Wins = 1
            }
        };

        var tiebreaker = new TiebreakerService();

        // Act
        var scores = tiebreaker.RankTeams(teams, matchups, matches);

        // Assert
        scores[1].ShouldBeGreaterThan(scores[2]);
    }

    [Fact]
    [Trait("Category", "Tiebreaker")]
    public void WhenRankingMultipleTeams_ThenReturnsScoreForEachTeam()
    {
        // Arrange: no games played; just ensure we get a score per team.
        var teams = new List<Team>
        {
            new Team { Id = 1, Name = "A" },
            new Team { Id = 2, Name = "B" },
            new Team { Id = 3, Name = "C" }
        };
        var matchups = new List<RoundRobinMatchup>();
        var matches = new List<Match>();
        var tiebreaker = new TiebreakerService();

        // Act
        var scores = tiebreaker.RankTeams(teams, matchups, matches);
        var orderedTeamIds = teams.Select(t => t.Id).OrderBy(id => id).ToList();

        // Assert: every team gets exactly one score.
        scores.Count.ShouldBe(teams.Count);
        scores.Keys.OrderBy(id => id).ShouldBe(orderedTeamIds);
    }
}
