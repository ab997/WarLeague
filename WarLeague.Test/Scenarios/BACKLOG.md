# Scenario Backlog

## Near-term

- [ ] Format create duplicate should fail with clear message.
- [ ] Format delete existing should remove persisted format.
- [ ] Format update rules should persist JSON payload.
- [ ] Multiple conferences in one season.
- [ ] Transition to InProgress and verify pairings generated.

## Mid-term

- [ ] Season duplicate number guard.
- [ ] Team modification enable/disable behavior.
- [ ] Week completion (all matches reported).
- [ ] Week completion fails (unreported matches).

## Long-term

- [ ] Deck submission requirements per seat.
- [ ] Pairing generation across conferences and playoffs.
- [ ] Match reporting and final standings/tiebreakers.

## Builder methods to add

- [ ] `WithPlayers(int count)` - add non-captain players to teams.
- [ ] `TransitionToInProgress()` - close submissions -> generate pairings -> in progress.
- [ ] `ReportAllMatchWins()` - report results for all matches in current week.
- [ ] `CompleteWeek()` - transition week to completed.

## Notes

- [x] Keep scenarios behavior-oriented and readable.
- [x] Add to `ScenarioBuilder` before adding repeated setup in specs.
- [x] Use `TryX` pattern for builder methods where failure is expected.
