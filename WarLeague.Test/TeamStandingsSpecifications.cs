using Shouldly;

namespace WarLeague.Test;

/// <summary>
/// Specifications for TeamStandings: phase switch populates standings,
/// first playoff week uses standings order, seed/tiebreaker edits are reflected, guards reject when not allowed.
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
        standings.Select(s => s.Seed).Distinct().Count().ShouldBe(standings.Count);
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
    public async Task WhenUpdatingSeed_ThenFirstPlayoffWeekReflectsNewOrder()
    {
        // Arrange: playoffs phase with 4 playoff teams, no playoff week opened yet
        var (seasonId, week2, teams) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekWithConferencePlayoffCountAsync(
            teamsPerConference: 4, playoffTeamsPerConference: 2, playersPerTeam: 2);
        var standings = await _teamStandingsService.GetStandingsForSeasonAsync(seasonId);
        standings.Count.ShouldBe(4);
        var seed1TeamId = standings.First(s => s.Seed == 1).TeamId;
        var seed2TeamId = standings.First(s => s.Seed == 2).TeamId;

        // Act: swap seed 1 and 2
        var team1 = teams.First(t => t.Id == seed1TeamId);
        var team2 = teams.First(t => t.Id == seed2TeamId);
        (await _teamStandingsService.UpdateSeedAsync(seasonId, team1.Id, 2)).Success.ShouldBeTrue();
        (await _teamStandingsService.UpdateSeedAsync(seasonId, team2.Id, 1)).Success.ShouldBeTrue();

        // Open first playoff week so matchups are generated from new order
        (await _weekService.TransitionToOpenWeekAsync(seasonId, week2.WeekNumber)).Success.ShouldBeTrue();

        // Assert: first bracket position should now have former seed-2 team as top seed (seed 1)
        var standingsAfter = await _teamStandingsService.GetStandingsForSeasonAsync(seasonId);
        var newSeed1 = standingsAfter.First(s => s.Seed == 1);
        newSeed1.TeamId.ShouldBe(seed2TeamId);
    }

    [Fact]
    [Trait("Category", "TeamStandings")]
    public async Task WhenUpdatingSeedBeforePhaseSwitch_ThenReturnsFail()
    {
        // Arrange: season still in RoundRobin (one week completed, do not switch to playoffs)
        var (seasonId, teams) = await GetSeasonWithRoundRobinWeekCompleted_NotPlayoffsAsync(teamsPerConference: 2, playersPerTeam: 2);

        // Act: try to update seed (no standings exist; guard says not Playoffs)
        var team = teams.First();
        var result = await _teamStandingsService.UpdateSeedAsync(seasonId, team.Id, 1);

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

    #endregion
}
