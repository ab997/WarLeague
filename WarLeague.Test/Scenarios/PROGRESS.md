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

## Invariants confirmed

- [x] Scenario layer is service-only.
- [x] No Discord command path is involved.
- [x] Builder methods are composable and reusable for future scenarios.
- [x] Scenario tests follow the same partial-class style as the rest of `WarLeague.Test`.
- [x] Builder is a standalone class; no DI/context lifecycle inside it.
- [x] Failure scenarios use `TryX` builder methods that don't assert, letting specs check `LastResult`.

## Next session recommended start

- [ ] Add week transition to InProgress (generates pairings).
- [ ] Add match reporting scenarios.
- [ ] Add week completion scenarios.
