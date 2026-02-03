# GitHub Copilot Instructions — WarLeague

## Purpose
- Generate code aligned with solution architecture, naming, and practices.
- Prefer minimal, composable changes.

## Architecture
### WarLeague.Core
- `Data/Entities/*`: EF Core POCOs only. No logic. Init navigations or `= null!;`.
- `Data/Enums/*`: Domain enums. Persist as strings.
- `Repositories/*`: Thin async data access. No business logic.
- `Domain/Services/*`: Business rules/workflows. Coordinate repos. Return `Result`-like models.

### WarLeague.Discord
- `Commands/*`: Thin Discord shell. Parse inputs, call services, format output.
- Preconditions/helpers in dedicated classes.

## General Conventions
- Async/await end-to-end; no blocking.
- Async methods end with `Async`.
- Keep commands short; move rules/validation to services.
- Use `DateTime.UtcNow`.
- Parse dates with `DateTime.TryParse`; validate ordering.
- Avoid new dependencies.
- Favor immutability and explicit collection init.

## Entity Framework Core
- Enums as strings: `HasConversion<string>()`.
- Explicit relationships for non-conventional navs.
- Use unique/filtered indexes for invariants.
- Disable cascade delete globally (`DeleteBehavior.Restrict`).

### Queries
- Use `SingleOrDefaultAsync` for 0..1.
- Catch `InvalidOperationException` in services; return failure `Result`.
- Repos include required navigations.

## Repositories
- CRUD/query composition only.
- Always async EF APIs.
- Save inside repository.
- Return concrete results, not `IQueryable`.

## Domain Services
- Return `Result` with `Success`, `Message`, payload.
- Convert inconsistencies to user-safe messages.
- Use transactions for multi-entity workflows; single commit.

## Discord Commands
- NEVER place business logic here directly to command file, ALWAYS delegate to services.

## Error Handling
- Use `Result` for expected failures.
- Exceptions only for exceptional states.
- Responses short and actionable.

## Adding Commands
- Group with existing module when possible.
- `DeferAsync` → `FollowupAsync`.
- Validate early; delegate to services.

## Do / Don’t
- Do: keep commands declarative; async everywhere; respect indexes; avoid cascades.
- Don’t: add business logic to repos/commands; generate pairings if matches exist.
