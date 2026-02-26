# Scenario Backlog

## Near-term

- [ ] Format create duplicate should fail with clear message.
- [ ] Format delete existing should remove persisted format.
- [ ] Format update rules should persist JSON payload.
- [x] Multiple conferences in one season.
- [x] Transition to InProgress and verify pairings generated.

## Mid-term

- [ ] Season duplicate number guard.
- [ ] Team modification enable/disable behavior.
- [x] Week completion (all matches reported).
- [x] Week completion fails (unreported matches).

## Long-term

- [ ] Deck submission requirements per seat.
- [x] Pairing generation across conferences and playoffs (two-conference playoff qualifiers).
- [x] Full round-robin season with standings generation (5 teams, 5 rounds).
- [x] Tiebreaker edge cases and manual tiebreaker updates.
- [x] Playoff bracket progression (single-conference, multi-conference, BYEs).

## Builder methods to add

- [x] `WithPlayersPerTeam(int totalPerTeam)` - add non-captain players to teams.
- [x] `MoveToInProgress()` - close submissions -> generate pairings -> in progress.
- [x] `ReportAllMatchResults()` / `ReportMatchResults(int[])` - report results for all/partial matches.
- [x] `CompleteWeek()` / `TryCompleteWeek()` - transition week to completed (assert/try pattern).
- [x] `PlayFullRoundRobin(int)` - query suggested rounds and loop full week happy-path.
- [x] `SetPhaseToPlayoffs()` - transition season to Playoffs phase.
- [x] `SubstitutePlayer(int teamIndex, int playerOutSeat)` - swap bench player into a match.
- [x] `TryOpenWeek()` - non-asserting open week variant.
- [x] `TryMoveToInProgress()` - non-asserting move-to-in-progress variant.
- [x] `TrySetPhaseToPlayoffs()` - non-asserting set-phase-to-playoffs variant.
- [x] `PlayPlayoffRound(int[], int)` - play one playoff week with specified loser teams.
- [x] `UpdateTiebreaker(int, int)` / `TryUpdateTiebreaker(int, int)` - manual tiebreaker edits.

## Notes

- [x] Keep scenarios behavior-oriented and readable.
- [x] Add to `ScenarioBuilder` before adding repeated setup in specs.
- [x] Use `TryX` pattern for builder methods where failure is expected.
