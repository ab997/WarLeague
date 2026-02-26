# Scenario Progress

## Completed

- [x] Added `ScenarioBuilder` as fluent async-chainable builder class
- [x] `NewScenario()` factory method in `Specifications` wires builder to existing DI services
- [x] Refactored to `public partial class Specifications` pattern
- [x] Builder: `CreateFormat()`, `WithSeason()`, `WithConference()`, `WithTeams()`
- [x] Builder: `WithWeek()`, `OpenWeek()`, `CloseSubmissions()`, `TryCloseSubmissions()`
- [x] Builder: `SubmitDecksForAllTeams()`, `SubmitDecksForTeams()` (partial submission support)
- [x] Builder tracks `LastResult` for asserting success/failure in specs
- [x] Scenario 1: `Scenario_CreateFormat_Succeeds`
- [x] Scenario 2: `Scenario_CreateFormatWithSeasonConferenceAndFourTeams_Succeeds`
- [x] Scenario 3: `Scenario_CreateWeek_OpenIt_SubmitDecks_CloseSubmissions_Succeeds`
- [x] Scenario 4: `Scenario_CloseSubmissions_WhenNoTeamSubmitted_Fails`
- [x] Scenario 5: `Scenario_CloseSubmissions_WhenOnlySomeTeamsSubmitted_Fails`
- [x] Builder: `WithPlayersPerTeam(int)` — adds non-captain players to fill teams
- [x] Builder: `MoveToInProgress()`, `CompleteWeek()`, `TryCompleteWeek()`
- [x] Builder: `ReportAllMatchResults()`, `ReportMatchResults(int[])` (partial reporting support)
- [x] Builder tracks `List<Match> Matches` populated by `MoveToInProgress()`
- [x] Builder wired to `MatchService` for match result reporting
- [x] Scenario 6: `Scenario_FullWeekLifecycle_SubmitDecks_MoveToInProgress_ReportAllMatches_CompleteWeek_Succeeds`
- [x] Scenario 7: `Scenario_CompleteWeek_WhenNotAllMatchesReported_Fails`
- [x] Builder wired to `MatchupServiceFactory` and `SeasonRepository` for round-robin support
- [x] Builder: `PlayFullRoundRobin(int)` — queries suggested rounds, loops full week happy-path for each
- [x] Builder: `SetPhaseToPlayoffs()` — transitions season to Playoffs (generates standings)
- [x] Builder tracks `TotalRoundsPlayed` set by `PlayFullRoundRobin()`
- [x] Scenario 8: `Scenario_FiveTeams_FullRoundRobin_SetPhaseToPlayoffs_Succeeds`
- [x] Builder wired to `SubstitutionService` for player substitution
- [x] Builder tracks `TeamNames` alongside `TeamIds`
- [x] Builder: `SubstitutePlayer(int teamIndex, int playerOutSeat)` — swaps bench player into a match
- [x] Builder tracks `LastSubstitution` for verifying swap in specs
- [x] Scenario 9: `Scenario_SubstitutePlayer_PairingsReflectSubstitution_Succeeds`
- [x] Builder: `TryOpenWeek()`, `TryMoveToInProgress()`, `TrySetPhaseToPlayoffs()` — non-asserting variants
- [x] Scenario 10: `Scenario_MoveToInProgress_WhenAnotherWeekAlreadyInProgress_Fails`
- [x] Scenario 11: `Scenario_OpenWeek_WhenAnotherWeekAlreadyOpen_Fails`
- [x] Scenario 12: `Scenario_SetPhaseToPlayoffs_WhenUnfinishedWeeksExist_Fails`
- [x] Scenario 13: `Scenario_SetPhaseToPlayoffs_WhenNoWeeksExist_Fails`
- [x] Scenario 14: `Scenario_SetPhaseToPlayoffs_WhenPlayoffTeamsCountIsZero_Fails`
- [x] Scenario 15: `Scenario_TwoConferences_FullRoundRobin_SetPhaseToPlayoffs_Succeeds`
- [x] Scenario 16: `Scenario_TwoConferences_Top2PerConference_PlayoffQualifiers_Succeeds`
- [x] Builder wired to `WeekRepository`, `MatchRepository`, `TeamStandingsService`, `PlayoffBracketService`
- [x] Builder: `PlayPlayoffRound(int[], int)` — plays one complete playoff week with specified losers
- [x] Builder: `UpdateTiebreaker(int, int)` / `TryUpdateTiebreaker(int, int)` — manual tiebreaker edits
- [x] Registered `PlayoffBracketService` in `TestServiceProvider`
- [x] Scenario 17: `Scenario_FourTeams_FullPlayoffBracket_SemifinalsAndFinals_Succeeds`
- [x] Scenario 18: `Scenario_FiveTeams_PlayoffBracketWithByes_ToFinals_Succeeds`
- [x] Scenario 19: `Scenario_TwoConferences_PlayoffBracketProgression_Succeeds`
- [x] Scenario 20: `Scenario_UpdateTiebreaker_BeforeFirstPlayoffWeek_ChangesSeeding`
- [x] Scenario 21: `Scenario_UpdateTiebreaker_AfterPlayoffMatchupsExist_Fails`
- [x] Scenario 22: `Scenario_UpdateTiebreaker_BeforePlayoffsPhase_Fails`

## Invariants confirmed

- [x] Scenario layer is service-only.
- [x] No Discord command path is involved.
- [x] Builder methods are composable and reusable for future scenarios.
- [x] Scenario tests follow the same partial-class style as the rest of `WarLeague.Test`.
- [x] Builder is a standalone class; no DI/context lifecycle inside it.
- [x] Failure scenarios use `TryX` builder methods that don't assert, letting specs check `LastResult`.
- [x] Playoff bracket progression verified end-to-end (round-robin → playoffs → semifinals → final → champion).
- [x] BYE handling confirmed: teams with BYEs auto-advance, only non-BYE matchups generate player matches.

## Next session recommended start

- [ ] Add format lifecycle guard scenarios (duplicate create fails, delete, update rules).
- [ ] Add season duplicate number guard scenario.
- [ ] Add roster management scenarios (add extra players, team modification toggle).
