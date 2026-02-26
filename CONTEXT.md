## WarLeague – High‑level Context

This file summarizes key concepts and structure of the WarLeague solution to make future work faster.

---

### Solution layout

- **WarLeague.Core**: Domain‑level services and models.
  - Season/Week lifecycle (`SeasonService`, `WeekService`).
  - Playoffs and brackets (`PlayoffService`, `PlayoffBracketService`).
  - Standings and tiebreakers (`TeamStandingsService`, `TiebreakerService`).
  - Match/round‑robin matchup orchestration (`MatchService`, `MatchupServiceFactory`, `IMatchupService`).
- **WarLeague.Data**: EF Core entities, repositories, and migrations.
  - Entities: `Season`, `Week`, `Team`, `Player`, `PlayerSeasonTeam`, `DeckSubmission`, `PlayoffMatchup`, etc.
  - Repositories: `SeasonRepository`, `WeekRepository`, `TeamRepository`, `PlayerSeasonTeamRepository`, `PlayoffMatchupRepository`, `TeamStandingsRepository`, `ConferenceRepository`, etc.
- **WarLeague.Discord**: Discord bot and interaction modules.
  - Commands grouped by domain: `SeasonCommands`, `WeekCommands`, `PeepCommands` (info/overview), `TestCommands`, etc.
  - Helpers/services: `DiscordApiHelperService`, `DiscordPlayerService`, permission and context preconditions.
- **WarLeague.Test**: Unit/integration tests that exercise core flows (week lifecycle, playoffs, tiebreakers, etc.).

---

### Core domain concepts

- **Format**
  - Logical competition container (e.g., a league or tournament format).
  - Has many `Season` records.
  - Discord category name is used to resolve the current `Format` in most commands (`DiscordApiHelperService.GetFormatByCategoryNameAsync`).

- **Season**
  - Belongs to a `Format` (`FormatId`/`Format`).
  - Key fields:
    - `SeasonNumber`: human‑facing identifier.
    - `Active`: only one active season per format at a time (enforced by `SeasonCommands.SetActive` + `SeasonRepository.GetActiveSeasonsByFormatAsync`).
    - `Phase`: `RoundRobin` or `Playoffs` (`SeasonPhase` enum). Phase switch is one‑way (`SeasonService.SetPhaseToPlayoffsAsync`).
    - `DisableTeamModification`: when true, captains cannot add/remove players/etc.
    - `MinimumTeamMembers`: used to validate week `SubmissionsRequired`.
  - Related collections: `Conferences`, `Weeks`, `Teams`, `TeamStandings`.

- **Week**
  - Represents one competition week in a season (either round‑robin or playoffs).
  - Fields:
    - `WeekNumber`: sequential within a season.
    - `Status`: `NotOpenYet`, `Open`, `SubmissionsClosed`, `InProgress`, `Completed` (`WeekStatus` enum).
    - Optional dates: `StartDate`, `EndDate`, `SubmissionsClosedDate`.
    - `SubmissionsRequired`: required number of deck submissions (players per team) for that week.
  - Key transitions (all in `WeekService`):
    - **Create**: `CreateAsync` → `Status = NotOpenYet`.
    - **Open**: `TransitionToOpenWeekAsync` (NotOpenYet → Open).
      - Validates there isn’t another `Open` week.
      - Enforces `DisableTeamModification = true` on the season (and may append suggestion text for round‑robin weeks).
      - Ensures team matchups exist for that week via `MatchService.EnsureTeamMatchupsForWeekAsync`.
    - **Close submissions**: `TransitionToCloseSubmissionsAsync` (Open → SubmissionsClosed).
      - Ensures all required teams have exactly `SubmissionsRequired` submissions for the week (phase‑agnostic, but delegates to matchup service for “teams required this week”).
    - **Generate pairings**: `TransitionToInProgressAsync` (SubmissionsClosed → InProgress).
      - Generates player‑level matches via `MatchService.GeneratePairingsAsync` and moves week to `InProgress`.
    - **Close week**: `TransitionToCompletedAsync` (InProgress → Completed).
      - Validates all matches for the week are reported.
      - Delegates to matchup service (`UpdateMatchupWinnersForWeekAsync`) to set team‑level winners (round‑robin or playoffs).
      - Marks the week `Completed`.
  - `WeekCommands` wraps these transitions for Discord via slash commands: `/week create`, `/week open`, `/week close-submissions`, `/week generate-pairings`, `/week close`, `/week list`, etc.

- **Team / Player / Membership**
  - `Team`:
    - Fields: `Id`, `Name`, `CaptainId`, `SeasonId`, `ConferenceId`, optional `DiscordRoleId`, `CreatedDate`.
    - One captain (`Player`), belongs to one `Season` and one `Conference`.
  - `Player`:
    - Fields: `Id`, `DiscordUserId`, `UserName`.
    - Navigation: `PlayerSeasonTeams`, `DeckSubmissions`.
  - `PlayerSeasonTeam`:
    - Join entity linking `Player`, `Season`, `Team`.
    - `PlayerSeasonTeamRepository` handles membership lookups and lists (e.g., `GetBySeasonAsync`, `GetPlayerIdsByTeamAndSeasonAsync`).
  - `TeamRepository`:
    - Access by season/name/captain, enumerate teams by season, etc.
  - `TeamService` (Core) enforces `DisableTeamModification` flag for mutating operations.

---

### Round‑robin standings and tiebreakers

- **Standings data model**
  - `TeamStandings` table stores snapshot standings per team (round‑robin).
  - `TeamStandingsService`:
    - Can generate standings from matches (`GenerateStandingsFromRoundRobinAsync`).
    - Provides views for Discord display (`GetStandingsForSeasonAsync`, `GetDisplayStatsForSeasonAsync`, `GetRoundRobinStandingsForDisplayAsync`).

- **Tiebreaker logic**
  - Implemented in `TiebreakerService.RankTeams`:

```8:175:WarLeague.Core/Services/TiebreakerService.cs
public class TiebreakerService
{
    public IReadOnlyDictionary<int, decimal> RankTeams(
        IEnumerable<Team> teams,
        IReadOnlyList<RoundRobinMatchup> matchups,
        IReadOnlyList<Match> matches)
    {
        // Computes tiebreaker scores per teamId based on:
        // - Overall W/L from round-robin matchups (byes treated as wins)
        // - Head-to-head matrix among tied teams
        // - Series wins/losses from match results
        // - Game wins/losses using Player1Wins / Player2Wins
        // Then normalizes into dense tiebreaker values (higher = better).
    }
}
```

  - Priorities:
    - Primary: overall record (wins − losses across round‑robin matchups, counting byes as wins).
    - Secondary: head‑to‑head among tied teams.
    - Tertiary: series diff (series wins − series losses).
    - Quaternary: game diff (per‑game wins − losses from `Player1Wins/Player2Wins`).
  - Output: a dictionary `teamId → decimal tiebreaker` where higher means better; these values are used in standings snapshots and for determining playoff seeds.

---

### Playoffs and single‑elimination bracket

- **Playoff entities and repositories**
  - `PlayoffMatchup` (`WarLeague.Data/Data/Entities/PlayoffMatchup.cs`):

```6:20:WarLeague.Data/Data/Entities/PlayoffMatchup.cs
public class PlayoffMatchup
{
    public int Id { get; set; }
    public int WeekId { get; set; }
    public Week Week { get; set; } = null!;
    public int Team1Id { get; set; }
    public Team Team1 { get; set; } = null!;
    public int Team2Id { get; set; }
    public Team Team2 { get; set; } = null!;
    public MatchupType MatchupType { get; set; }
    public int? TeamWinnerId { get; set; }
    public Team? TeamWinner { get; set; }
    public int Round { get; set; }
    public int BracketPosition { get; set; }
}
```

  - `PlayoffMatchupRepository`:
    - `GetByWeekIdAsync`, `GetBySeasonIdAsync` (includes `Week`), `AddRangeAsync`, `UpdateRangeAsync`.

- **What `BracketPosition` means**
  - Integer index **within a playoff week** (0‑based) indicating the vertical ordering of matchups in the bracket.
  - Used for:
    - **Display ordering** (top‑to‑bottom) when showing brackets (`PlayoffBracketService` and `/peep bracket`).
    - **Business logic** to derive next‑round matchups in `PlayoffService`:
      - Winners are collected from the previous week **ordered by `BracketPosition`**.
      - Next round pairs winners from the top and bottom of that ordered list to preserve bracket structure (1 vs 4, 2 vs 3, etc. for a 4‑team round).

- **PlayoffService: core responsibilities**
  - Implements `IMatchupService` for playoff weeks.
  - Key methods:
    - `GetTeamMatchupsAsync(weekId)`:
      - If **no playoff matchups yet** for the season → creates first‑round bracket from standings.
      - If **matchups already exist for that week** → returns them projected back to `(Team a, Team b)` pairs.
      - If **later playoff week with no matchups yet** → derives matchups from previous week’s winners, ordered by `BracketPosition`.
    - `SaveTeamMatchupsAsync(weekId, teamMatchups)`:
      - Persists `(Team a, Team b)` pairs into `PlayoffMatchup` records for that week.
      - Sets `Round` via `CalculateRoundNumber`, and `BracketPosition` from enumeration index.
      - For BYEs, uses `MatchupType.Bye` and `Team1Id == Team2Id` (team vs itself).
    - `UpdateMatchupWinnersForWeekAsync(weekId, matches)`:
      - For each `PlayoffMatchup` in the week:
        - If `MatchupType.Bye` → winner is `Team1Id`.
        - Else, counts series wins from the `Match` table (per team) and sets `TeamWinnerId` to the team with more series wins. Fails if tied.
    - `GetByeTeamsForPairingsDisplayAsync(weekId)` / `GetTeamIdsRequiredForSubmissionsAsync(weekId)`:
      - Utility methods for deck‑submission validation and Discord display.

- **Bracket generation for arbitrary team counts**
  - First‑round seeding:
    - `GetFirstPlayoffWeekMatchupsAndPlayoffTeamsAsync`:
      - Fetches global standings and conferences.
      - Uses `GetPlayoffQualifiersFromStandings` to select top N seed teams per conference based on `Seed`.
      - Orders playoff qualifiers globally by seed.
      - Calls `GenerateBracketMatchups` to create first‑round `(Team a, Team b)` pairs.
  - `GenerateBracketMatchups(List<Team> teams, int round)` algorithm:
    - Let `count = teams.Count` (seeded teams list is ordered by seed ascending).
    - Compute **bracket size** as `nextPowerOfTwo >= count` (2,4,8,16,...).
    - **BYE count** = `nextPowerOfTwo − count`.
    - Assign BYEs to the **top seeds**: for `i = 0..byeCount-1`, create matchup `(teams[i], teams[i])` with `MatchupType.Bye`.
    - Remaining teams (`teams[byeCount..count-1]`) are paired:
      - Let `playingTeamsStart = byeCount`, `playingTeamsEnd = count − 1`.
      - For `i` from `playingTeamsStart` to `playingTeamsStart + (count − byeCount)/2 − 1`:
        - Pair `teams[i]` vs `teams[playingTeamsEnd − (i − playingTeamsStart)]`.
    - This yields a valid **single‑elimination bracket** for any `count >= 2`:
      - 4 teams → standard 4‑team bracket.
      - 5 teams → treated as top‑8 with 3 byes for top seeds.
      - 10 teams → treated as top‑16 with 6 byes, etc.
  - Later rounds:
    - `GetTeamMatchupsAsync` with existing `PlayoffMatchup`s:
      - For week W>first playoff week:
        - Gets previous week’s `PlayoffMatchup`s (weekNumber − 1), ordered by `BracketPosition`.
        - Builds `winners` list in that order (BYE winners are auto‑winners, others require `TeamWinnerId`).
        - Pairs `winners[i]` vs `winners[n − 1 − i]` to form next‑round bracket (top vs bottom, etc.).

- **PlayoffBracketService and bracket display**
  - `PlayoffBracketService.GetBracketAsync(seasonId)`:
    - Loads all `PlayoffMatchup`s for the season, includes team names, determines BYE status.
    - Orders by `WeekNumber` then `BracketPosition` and maps into `PlayoffBracketMatchupDisplay` DTO.
  - `/peep bracket` (`PeepCommands.BracketAsync`):
    - Groups matchups by `(Round, WeekNumber)` and builds embeds.
    - Uses the overall bracket size (rounded to next power of 2 from all unique teams in the season’s playoff bracket) to label embeds as “Top N bracket”, “Top 4 bracket”, or “Finals bracket” per playoff week.
    - Handles BYEs when constructing lines (`IsBye` set by `PlayoffBracketService`).

---

### Discord commands – key entry points

- **`PeepCommands`** (information and display):
  - `/peep format-info`: shows format + seasons (statuses and phases).
  - `/peep admin-help`: administrator operational guide (high‑level instructions and lifecycle).
  - `/peep standings-round-robin`: shows W‑L standings table, optionally grouped by conference.
  - `/peep bracket`: shows playoff bracket by round/week with winners and BYEs.
  - `/peep week-results`, `/peep all-results`: per‑week and full‑season match results (played vs pending).
  - `/peep overview`: hierarchical overview of formats, seasons, weeks, teams, and players.
  - `/peep season-current`, `/peep week`, `/peep team`, `/peep my-team`, `/peep rules`: current state/context queries.

- **`WeekCommands`** (admin lifecycle control for weeks):
  - `/week create`, `/week delete`, `/week update`, `/week list`.
  - `/week open`, `/week close-submissions`, `/week generate-pairings`, `/week close`.
  - `/week suggest-round-robin`, `/week generate-round-robin-schedule` for automated round‑robin scheduling.
  - `/week ping-players` to tag players with pending matches in the current week.

- **`SeasonCommands`** (admin lifecycle for seasons):
  - `/season create`, `/season delete`.
  - `/season set-active`: marks one season active, others inactive, per format.
  - `/season admin-set-team-modifications`: toggles captain add/drop permissions (`DisableTeamModification`).
  - `/season switch-to-playoffs`: validates all round‑robin weeks completed and playoff configuration, sets phase to Playoffs, generates standings and identifies playoff qualifiers.
  - `/season list`: lists seasons for the current format with phase and active status.

---

### Miscellaneous notes

- **Preconditions and context initialization**
  - Attributes like `EnsureChannelIsInFormatCategory`, `EnsureSingleActiveSeason`, `EnsureValidTeams`, and `InitializeGuildContext` run before commands to enforce invariants and set guild/format context.
  - `EnsureSingleActiveSeason` ensures exactly one active season exists for the format before using many commands.

- **Team modification lock**
  - Automatically enabled when opening the first `Open` week of a season via `WeekService.TransitionToOpenWeekAsync` (sets `DisableTeamModification = true`). This prevents mid‑season roster manipulation unless explicitly re‑enabled by admins.

- **Season close behavior (finals)**
  - When closing a week via `/week close`, the underlying logic in `WeekService.TransitionToCompletedAsync` updates playoff winners for that week (if in playoffs).
  - Additional Discord‑side logic can detect that the final playoff week has just completed, derive the champion from `PlayoffMatchup` winners, congratulate the team and its members, and mark the season `Active = false` to indicate the season has concluded.

