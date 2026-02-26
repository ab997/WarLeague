using Shouldly;

namespace WarLeague.Test;

public partial class Specifications
{
    private ScenarioBuilder NewScenario() => new ScenarioBuilder(
        _formatService, _seasonService, _conferenceService,
        _teamService, _weekService, _deckSubmissionService, _matchService,
        _matchupServiceFactory, _substitutionService,
        _playerRepository, _teamRepository, _playerSeasonTeamRepository,
        _seasonRepository);

    #region Scenario: Format lifecycle

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Scenario_CreateFormat_Succeeds()
    {
        var s = await NewScenario().CreateFormat();

        s.FormatId.ShouldBeGreaterThan(0);
        s.FormatName.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Scenario: Format with season, conference and teams

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Scenario_CreateFormatWithSeasonConferenceAndFourTeams_Succeeds()
    {
        var s = await NewScenario()
            .CreateFormat()
            .WithSeason()
            .WithConference("Alpha")
            .WithTeams(4);

        s.FormatId.ShouldBeGreaterThan(0);
        s.SeasonId.ShouldBeGreaterThan(0);
        s.CurrentConference.ShouldBe("Alpha");
        s.TeamIds.Count.ShouldBe(4);
        s.CaptainIds.Count.ShouldBe(4);
    }

    #endregion

    #region Scenario: Week lifecycle (create, open, submit, close submissions)

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Scenario_CreateWeek_OpenIt_SubmitDecks_CloseSubmissions_Succeeds()
    {
        var s = await NewScenario()
            .CreateFormat()
            .WithSeason()
            .WithConference("Alpha")
            .WithTeams(4)
            .WithWeek(weekNumber: 1, submissionsRequired: 1)
            .OpenWeek()
            .SubmitDecksForAllTeams(submissionsPerTeam: 1)
            .CloseSubmissions();

        s.LastResult.ShouldNotBeNull();
        s.LastResult!.Success.ShouldBeTrue(s.LastResult.Message);
    }

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Scenario_CloseSubmissions_WhenNoTeamSubmitted_Fails()
    {
        var s = await NewScenario()
            .CreateFormat()
            .WithSeason()
            .WithConference("Alpha")
            .WithTeams(4)
            .WithWeek(weekNumber: 1, submissionsRequired: 1)
            .OpenWeek()
            .TryCloseSubmissions();

        s.LastResult.ShouldNotBeNull();
        s.LastResult!.Success.ShouldBeFalse();
        s.LastResult.Message.ShouldContain("not all required teams", Case.Insensitive);
    }

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Scenario_CloseSubmissions_WhenOnlySomeTeamsSubmitted_Fails()
    {
        var s = await NewScenario()
            .CreateFormat()
            .WithSeason()
            .WithConference("Alpha")
            .WithTeams(4)
            .WithWeek(weekNumber: 1, submissionsRequired: 1)
            .OpenWeek()
            .SubmitDecksForTeams([0, 1], submissionsPerTeam: 1)
            .TryCloseSubmissions();

        s.LastResult.ShouldNotBeNull();
        s.LastResult!.Success.ShouldBeFalse();
        s.LastResult.Message.ShouldContain("not all required teams", Case.Insensitive);
    }

    #endregion

    #region Scenario: Full week lifecycle (submissions → in-progress → match results → completed)

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Scenario_FullWeekLifecycle_SubmitDecks_MoveToInProgress_ReportAllMatches_CompleteWeek_Succeeds()
    {
        var s = await NewScenario()
            .CreateFormat()
            .WithSeason()
            .WithConference("Alpha")
            .WithTeams(4)
            .WithPlayersPerTeam(3)
            .WithWeek(weekNumber: 1, submissionsRequired: 3)
            .OpenWeek()
            .SubmitDecksForAllTeams(submissionsPerTeam: 3)
            .CloseSubmissions()
            .MoveToInProgress()
            .ReportAllMatchResults()
            .CompleteWeek();

        s.LastResult.ShouldNotBeNull();
        s.LastResult!.Success.ShouldBeTrue(s.LastResult.Message);
        s.Matches.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Scenario_CompleteWeek_WhenNotAllMatchesReported_Fails()
    {
        var s = await NewScenario()
            .CreateFormat()
            .WithSeason()
            .WithConference("Alpha")
            .WithTeams(4)
            .WithPlayersPerTeam(3)
            .WithWeek(weekNumber: 1, submissionsRequired: 3)
            .OpenWeek()
            .SubmitDecksForAllTeams(submissionsPerTeam: 3)
            .CloseSubmissions()
            .MoveToInProgress()
            .ReportMatchResults([0])
            .TryCompleteWeek();

        s.LastResult.ShouldNotBeNull();
        s.LastResult!.Success.ShouldBeFalse();
        s.LastResult.Message.ShouldContain("not reported", Case.Insensitive);
    }

    #endregion

    #region Scenario: Full round-robin season → playoffs with standings

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Scenario_FiveTeams_FullRoundRobin_SetPhaseToPlayoffs_Succeeds()
    {
        var s = await NewScenario()
            .CreateFormat()
            .WithSeason()
            .WithConference("Alpha", playoffTeams: 2)
            .WithTeams(5)
            .WithPlayersPerTeam(3)
            .PlayFullRoundRobin(submissionsPerTeam: 3)
            .SetPhaseToPlayoffs();

        s.LastResult.ShouldNotBeNull();
        s.LastResult!.Success.ShouldBeTrue(s.LastResult.Message);
        s.TotalRoundsPlayed.ShouldBe(5); // 5 teams (odd) → 5 rounds

        var standings = await _teamStandingsService.GetStandingsForSeasonAsync(s.SeasonId);
        standings.Count.ShouldBe(5);
        for (var i = 0; i < standings.Count; i++)
            standings[i].Seed.ShouldBe(i + 1);
    }

    #endregion

    #region Scenario: Substitution reflected in pairings

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Scenario_SubstitutePlayer_PairingsReflectSubstitution_Succeeds()
    {
        var s = await NewScenario()
            .CreateFormat()
            .WithSeason()
            .WithConference("Alpha")
            .WithTeams(4)
            .WithPlayersPerTeam(4)
            .WithWeek(weekNumber: 1, submissionsRequired: 3)
            .OpenWeek()
            .SubmitDecksForAllTeams(submissionsPerTeam: 3)
            .CloseSubmissions()
            .MoveToInProgress()
            .SubstitutePlayer(teamIndex: 0, playerOutSeat: 1);

        s.LastSubstitution.ShouldNotBeNull();
        var (playerOut, playerIn) = s.LastSubstitution!.Value;

        var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(1, s.SeasonId);
        var matches = await _matchRepository.GetByWeekIdAsync(week!.Id);
        matches.ShouldContain(m => m.Player1Id == playerIn || m.Player2Id == playerIn);
        matches.ShouldNotContain(m => m.Player1Id == playerOut || m.Player2Id == playerOut);
    }

    #endregion
}
