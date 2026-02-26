# Scenario Roadmap

## Guiding principle

Every complex scenario must reuse smaller, already-proven building blocks from `ScenarioBuilder`.

## Ordered implementation phases

### 1) Format lifecycle

- [x] Create format (happy path)
- [ ] Create format (duplicate fails)
- [ ] Update rules
- [ ] Delete format

### 2) Season lifecycle

- [x] Create season and activate
- [ ] Season duplicate number guard
- [ ] Team-modification toggle

### 3) Conference and teams

- [x] Create conference in season
- [x] Create teams with auto-generated captains
- [x] Two-conference round-robin to playoffs with standings verification

### 4) Week lifecycle

- [x] Create week
- [x] Open week
- [x] Submit decks for all teams (happy path)
- [x] Close submissions (happy path)
- [x] Close submissions fails when no teams submitted
- [x] Close submissions fails when only some teams submitted
- [x] Transition to InProgress (generates pairings)
- [x] Complete week (all matches reported)
- [x] Complete week fails (not all matches reported)
- [x] MoveToInProgress fails when another week already InProgress
- [x] OpenWeek fails when another week already Open

### 5) Players and membership

- [ ] Add extra players to teams
- [x] Substitution: bench player swaps into a match, verified in pairings
- [ ] Roster management

### 6) Deck submissions and pairings

- [x] Submit decks via builder (all teams / partial teams)
- [ ] Pair generation and duplicate guards

### 7) Match reporting

- [x] Report win/result flows (via builder ReportAllMatchResults / ReportMatchResults)
- [ ] Invalid replay/invalid player edge cases

### 8) Full round-robin season

- [x] Full round-robin with N weeks (5 teams, 5 rounds via GetSuggestedRoundsAsync)
- [x] Transition to Playoffs phase (generates standings from round-robin results)
- [x] Verify standings seeding after playoffs transition
- [x] SetPhaseToPlayoffs fails when unfinished weeks exist
- [x] SetPhaseToPlayoffs fails when no weeks exist
- [x] SetPhaseToPlayoffs fails when PlayoffTeamsCount is 0

### 9) Standings, tiebreakers, playoffs

- [ ] Tiebreaker cases
- [ ] Playoff bracket progression scenarios

## Reuse expectations

- [x] Prefer new builder methods for shared setup and transitions.
- [x] Scenario tests should read like behavior scripts via fluent chaining.
- [x] Avoid direct database manipulation in specs unless verifying persistence/output.
