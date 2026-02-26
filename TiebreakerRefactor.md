## Tiebreaker Refactor Notes

**Goal**: Decouple `TeamStandingsService` from internal details of `TiebreakerService` so that callers only see a simple mapping of team to tiebreaker value (decimal), and all ranking logic stays internal to `TiebreakerService`.

### Tasks
- Ensure `RankTeams` in `TiebreakerService` exposes only team–tiebreaker pairs (no internal stats or ranking details).
- Update `TeamStandingsService` to consume only this simplified result and remove any dependencies on internal tiebreaker data structures.
- Keep behavior and ordering of standings consistent with the previous implementation.

### Progress
- Initial review of `TeamStandingsService` and `TiebreakerService` completed.
- Identified that `TeamStandingsService` currently depends on `TiebreakerRankingResult.OrderedTeams`, `TiebreakerByTeamId`, and `StatsByTeamId`.
- Next step: introduce a minimal return shape from `RankTeams` (team–decimal pairs) and adapt both services to use it.

