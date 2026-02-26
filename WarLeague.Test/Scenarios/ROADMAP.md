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
- [ ] Conference assignment paths (multiple conferences)

### 4) Week lifecycle

- [x] Create week
- [x] Open week
- [x] Submit decks for all teams (happy path)
- [x] Close submissions (happy path)
- [x] Close submissions fails when no teams submitted
- [x] Close submissions fails when only some teams submitted
- [ ] Transition to InProgress (generates pairings)
- [ ] Transition guard failures (e.g. already InProgress week exists)

### 5) Players and membership

- [ ] Add extra players to teams
- [ ] Roster management

### 6) Deck submissions and pairings

- [x] Submit decks via builder (all teams / partial teams)
- [ ] Pair generation and duplicate guards

### 7) Match reporting

- [ ] Report win/result flows
- [ ] Invalid replay/invalid player edge cases

### 8) Standings, tiebreakers, playoffs

- [ ] Standings updates
- [ ] Tiebreaker cases
- [ ] Playoff bracket progression

## Reuse expectations

- [x] Prefer new builder methods for shared setup and transitions.
- [x] Scenario tests should read like behavior scripts via fluent chaining.
- [x] Avoid direct database manipulation in specs unless verifying persistence/output.
