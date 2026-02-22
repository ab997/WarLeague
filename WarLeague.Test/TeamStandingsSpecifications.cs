using Shouldly;

namespace WarLeague.Test;

/// <summary>
/// Specifications for TeamStandings: phase switch populates standings,
/// first playoff week uses standings order, tiebreaker edits are reflected, guards reject when not allowed.
/// </summary>
public partial class Specifications
{
    #region TeamStandings

    [Fact]
    [Trait("Category", "TeamStandings")]
    public async Task WhenPhaseSwitchToPlayoffs_ThenTeamStandingsPopulated()
    {
        // Arrange: season with two conferences (2 teams each), one round-robin week completed
        var (seasonId, _, _) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekAsync(teamsPerConference: 2, playersPerTeam: 2);

        // Act: already done in helper (SetPhaseToPlayoffsAsync), which calls GenerateStandingsFromRoundRobinAsync

        // Assert: standings exist and have one row per playoff team (4 teams, 2 per conference with PlayoffTeamsCount 1 each = 2 total, or default is 1 per conference in CreateSeasonWithTwoConferencesAndSubmissions)
        var standings = await _teamStandingsService.GetStandingsForSeasonAsync(seasonId);
        standings.ShouldNotBeEmpty();
        standings.Count.ShouldBeGreaterThanOrEqualTo(2);
        standings.ShouldAllBe(s => s.Tiebreaker >= 0);
    }

    [Fact]
    [Trait("Category", "TeamStandings")]
    public async Task WhenFirstPlayoffWeekGenerated_ThenMatchupsFollowTeamStandingsOrder()
    {
        // Arrange: playoffs phase with standings (from phase switch)
        var (seasonId, week2, teams) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekAsync(teamsPerConference: 2, playersPerTeam: 2);
        var standingsBefore = await _teamStandingsService.GetStandingsForSeasonAsync(seasonId);
        standingsBefore.Count.ShouldBeGreaterThanOrEqualTo(2);

        // Act: open first playoff week (creates matchups from TeamStandings order)
        var openResult = await _weekService.TransitionToOpenWeekAsync(seasonId, week2.WeekNumber);

        // Assert: opening week succeeds and pairings are created from standings
        openResult.Success.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "TeamStandings")]
    public async Task WhenUpdatingTiebreaker_ThenFirstPlayoffWeekReflectsNewOrder()
    {
        // Arrange: playoffs phase with 4 playoff teams, no playoff week opened yet
        var (seasonId, week2, teams) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekWithConferencePlayoffCountAsync(
            teamsPerConference: 4, playoffTeamsPerConference: 2, playersPerTeam: 2);
        var standings = await _teamStandingsService.GetStandingsForSeasonAsync(seasonId);
        standings.Count.ShouldBe(4);
        var firstTeamId = standings[0].TeamId;
        var secondTeamId = standings[1].TeamId;
        var team1 = teams.First(t => t.Id == firstTeamId);
        var team2 = teams.First(t => t.Id == secondTeamId);

        // Act: set team2's tiebreaker higher than team1's so team2 becomes first in order
        (await _teamStandingsService.UpdateTiebreakerAsync(seasonId, team2.Id, 999_999_999)).Success.ShouldBeTrue();
        (await _teamStandingsService.UpdateTiebreakerAsync(seasonId, team1.Id, 0)).Success.ShouldBeTrue();

        // Open first playoff week so matchups are generated from new order
        (await _weekService.TransitionToOpenWeekAsync(seasonId, week2.WeekNumber)).Success.ShouldBeTrue();

        // Assert: first position in standings should now be the former second team
        var standingsAfter = await _teamStandingsService.GetStandingsForSeasonAsync(seasonId);
        standingsAfter[0].TeamId.ShouldBe(secondTeamId);
    }

    [Fact]
    [Trait("Category", "TeamStandings")]
    public async Task WhenUpdatingTiebreakerBeforePhaseSwitch_ThenReturnsFail()
    {
        // Arrange: season still in RoundRobin (one week completed, do not switch to playoffs)
        var (seasonId, teams) = await GetSeasonWithRoundRobinWeekCompleted_NotPlayoffsAsync(teamsPerConference: 2, playersPerTeam: 2);

        // Act: try to update tiebreaker (no standings exist; guard says not Playoffs)
        var team = teams.First();
        var result = await _teamStandingsService.UpdateTiebreakerAsync(seasonId, team.Id, 1);

        // Assert: standings cannot be edited when not in Playoffs
        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("cannot be edited", Case.Insensitive);
    }

    [Fact]
    [Trait("Category", "TeamStandings")]
    public async Task WhenUpdatingTiebreakerAfterPlayoffMatchupsExist_ThenReturnsFail()
    {
        // Arrange: playoffs phase, first playoff week opened (matchups saved)
        var (seasonId, week2, teams) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekAsync(teamsPerConference: 2, playersPerTeam: 2);
        (await _weekService.TransitionToOpenWeekAsync(seasonId, week2.WeekNumber)).Success.ShouldBeTrue();
        var team = teams.First();

        // Act: try to update tiebreaker
        var result = await _teamStandingsService.UpdateTiebreakerAsync(seasonId, team.Id, 99);

        // Assert
        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("cannot be edited", Case.Insensitive);
        result.Message.ShouldContain("playoff matchups", Case.Insensitive);
    }

    [Fact]
    [Trait("Category", "TeamStandings")]
    public async Task WhenGeneratingStandingsAfterPlayoffMatchupsExist_ThenReturnsFail()
    {
        // Arrange: playoffs phase, first playoff week opened
        var (seasonId, _, _) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekAsync(teamsPerConference: 2, playersPerTeam: 2);
        var week2 = await _weekRepository.GetByWeekNumberAndSeasonAsync(2, seasonId);
        (await _weekService.TransitionToOpenWeekAsync(seasonId, week2!.WeekNumber)).Success.ShouldBeTrue();

        // Act: try to regenerate standings
        var result = await _teamStandingsService.GenerateStandingsFromRoundRobinAsync(seasonId);

        // Assert
        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("playoff matchups already exist", Case.Insensitive);
    }

    [Fact]
    [Trait("Category", "TeamStandings")]
    public async Task WhenOnlyThreePlayoffSlotsAndTwoTeamsTied_ThenTiebreakerDecidesWhoMakesPlayoffs()
    {
        // Arrange: 4 teams, 1 week round-robin played; 2 teams win, 2 teams lose.
        // Conference allows only 3 teams into playoffs. Tiebreaker = lower Team.Id.
        var (seasonId, week2, teams) = await GetSeasonFourTeamsThreePlayoffSlotsTiebreakerScenarioAsync(playersPerTeam: 2);
        var allTeamIds = teams.Select(t => t.Id).ToHashSet();

        // Assert: standings (from phase switch) have exactly 3 teams
        var standings = await _teamStandingsService.GetStandingsForSeasonAsync(seasonId);
        standings.Count.ShouldBe(3);
        var playoffTeamIds = standings.Select(s => s.TeamId).ToHashSet();
        var excludedTeamId = allTeamIds.Single(id => !playoffTeamIds.Contains(id));
        var lastPlayoffSpotTeamId = standings[2].TeamId;
        // Among the two tied (0-win) teams, the one with better tiebreaker (lower Id) must have made it
        lastPlayoffSpotTeamId.ShouldBeLessThan(excludedTeamId,
            "The team that got the last playoff spot (position 3) must have better tiebreaker (lower Team.Id) than the excluded tied team.");

        // Act: proceed to playoffs (open first playoff week so bracket is built from standings)
        var openResult = await _weekService.TransitionToOpenWeekAsync(seasonId, week2.WeekNumber);

        // Assert: opening week succeeds; the team that made it is still the one with better tiebreaker
        openResult.Success.ShouldBeTrue();
        var standingsAfter = await _teamStandingsService.GetStandingsForSeasonAsync(seasonId);
        standingsAfter[2].TeamId.ShouldBe(lastPlayoffSpotTeamId,
            "Among the two tied teams, the one with better tiebreakers must have made it to playoffs.");
    }

    #endregion
}
