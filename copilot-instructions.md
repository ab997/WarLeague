# Copilot Instructions

You are contributing to the WarLeague solution. Follow these rules strictly.

## General Guidelines
- Prefer small, focused changes with clear rationale.
- Maintain backward compatibility and avoid breaking public contracts.
- Favor readability and explicitness over cleverness.
- Keep cross‑cutting utilities in shared services to avoid duplication.
- Always use async/await and cancellation where applicable.

## Code Style
- Language: C# targeting .NET 10; enable nullable reference types and treat warnings as errors.
- Naming:
  - Classes/Methods: PascalCase
  - Parameters/Locals/Fields: camelCase; private fields `_camelCase`
  - Async methods end with `Async`
- Formatting:
  - Use expression-bodied members sparingly; prioritize clarity.
  - Prefer `readonly` fields and immutability where feasible.
- Nullability:
  - Avoid `null` returns unless explicitly documented; prefer `Result`/Option patterns.
  - Validate all external input; fail fast with helpful messages.
- Exceptions & Errors:
  - Use domain `Result` for expected failures; reserve exceptions for exceptional states.
  - Return actionable messages for Discord interactions; avoid leaking internal details.

## Project-Specific Rules
- Discord Commands:
  - Keep command modules thin; delegate business logic to domain services.
  - Always `await DeferAsync(ephemeral: false)` early for slash commands.
  - Use `Summary` attributes for parameters with clear, user-facing descriptions.
  - Validate input (dates, enums, IDs) and provide consistent error messages.
- Helper Utilities:
  - Place common Discord helper utilities (e.g., `IsUserAdmin`, category/season resolution) in `DiscordApiHelperService` for reuse; prefer role-check helper methods instead of inline role checks.
  - Do not perform raw role checks in command modules; call helper methods.
- Date & Time:
  - Accept dates as ISO `YYYY-MM-DD` strings; parse using `DateTime.TryParseExact("yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, ...)`.
  - Validate logical ordering (start ≤ end; submissions close within range when applicable).
- Services:
  - Domain services (e.g., `WeekService`) must be stateless, deterministic, and DI‑friendly.
  - Return `Result` with `Message` for user-facing feedback; include `Success`/`Error` details when needed.
- Security:
  - Enforce preconditions via attributes (e.g., `[RequireRole("Admin")]`, `[EnsureSingleActiveSeason]`, `[EnsureChannelIsInFormatCategory]`).
  - Avoid exposing internal IDs; prefer user handles/mentions when responding.

## Documentation
- Add XML comments to public members that are part of the domain surface.
- Keep command descriptions concise and consistent; update summaries when parameters change.
- Document assumptions and side effects in services (e.g., state transitions in weeks).
