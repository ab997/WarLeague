# GitHub Copilot Instructions for WarLeague

Purpose
- Guide Copilot to generate code that fits this solution’s architecture, naming, and practices.
- Prefer minimal, composable changes that integrate cleanly with existing layers.

Scope
- Language: C# targeting .NET 10
- Solution: `WarLeague.Core`, `WarLeague.Discord`

Architecture and responsibilities
- `WarLeague.Core`
  - `Data/Entities/*`: EF Core entities (POCOs). No business logic. Initialize required navigations or use `= null!;`.
  - `Data/Enums/*`: Domain enums. Persist as strings via conversions.
  - `Repositories/*`: Thin data access over `WarLeagueDbContext`. No business logic, always async.
  - `Domain/Services/*`: Business rules and workflows. Coordinate repositories. Return `Result`-like models.
- `WarLeague.Discord`
  - `Commands/*`: Thin shell for Discord. Parse inputs, call services, format responses/embeds.
  - Preconditions and helpers live in dedicated classes/services.

General conventions
- Use `async`/`await` end-to-end; never block.
- Suffix async methods with `Async`.
- Keep command handlers short; move rules and validation into services.
- Use `DateTime.UtcNow` for server timestamps; parse dates from inputs using `DateTime.TryParse` and validate ordering.
- Avoid adding dependencies unless necessary.
- Favor immutability and explicit initialization for collections.

Entity Framework Core
- `WarLeagueDbContext`
  - Configure enums as strings: `Property(x => x.Status).HasConversion<string>();`
  - Define explicit relationships for non-conventional navigations (e.g., `Match.Player1`, `Match.Player2`, `Match.Winner`).
  - Create unique and filtered indexes to enforce invariants (e.g., only one active week per season for certain statuses).
  - Disable cascade delete globally by setting `DeleteBehavior.Restrict` on all FKs.
- Queries
  - Use `SingleOrDefaultAsync` when expecting 0..1. Catch `InvalidOperationException` in services and return a failure `Result` with a clear message.
  - Include needed navigations in repositories when services depend on them (e.g., include `DeckSubmissions` for week checks).

Repositories
- Only CRUD and query composition; no business logic.
- Always use async EF APIs (`AddAsync`, `ToListAsync`, `SingleOrDefaultAsync`).
- Mutators save inside the repository (`SaveChangesAsync`).
- Do not return `IQueryable` from repositories; return concrete results (`T?`, `List<T>`).

Domain services
- Return `Result`/custom result models with `Success`, `Message`, and payload as needed.
- Convert data inconsistencies (e.g., duplicates) into safe user messages; do not leak exceptions to commands.
- Use transactions for multi-entity workflows (e.g., pairing generation) and commit once.

Discord commands
- Derive from `InteractionModuleBase<SocketInteractionContext>`.
- Apply preconditions: `[RequireRole("Admin")]`, `[EnsureSingleActiveSeason]`, `[EnsureChannelIsInFormatCategory]` if appropriate.
- Pattern:
  - `await DeferAsync(ephemeral: false);`
  - Parse and validate inputs quickly (dates, ranges, enums).
  - Delegate to services; `await FollowupAsync(result.Message);`
- Embeds and output:
  - Max 25 fields per embed, 10 embeds per message; send in batches.
  - Truncate field values to Discord limits.

Error handling and messages
- Prefer returning `Result` to represent expected failures; exceptions are for truly exceptional states.
- Keep Discord responses short and actionable; avoid exceeding platform limits.

Add a new command
- Group under an existing module when possible; otherwise create a new `[Group]`.
- `DeferAsync` then `FollowupAsync`.
- Validate inputs early; delegate to services.

Do / Don’t
- Do keep command methods small and declarative.
- Do use EF async methods everywhere.
- Do avoid cascade deletes and respect filtered indexes.
- Don’t add business logic to repositories or commands.
- Don’t generate pairings if matches already exist for the week.