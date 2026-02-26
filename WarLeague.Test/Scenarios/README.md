# Scenario Testing (Service Layer)

This folder tracks the long-running scenario testing effort for `WarLeague.Test`.

## Scope

- [x] Test all core service logic end-to-end from the service perspective.
- [x] Exclude Discord command/interface behavior in `WarLeague.Discord/Commands`.
- [x] Build scenarios incrementally from simple to complex.
- [x] Reuse existing scenario building blocks instead of duplicating setup logic.

## Files

- [x] `ROADMAP.md` - ordered rollout plan for scenarios.
- [x] `PROGRESS.md` - completed work and immediate next steps.
- [x] `BACKLOG.md` - pending scenarios and ideas that are not yet scheduled.

## Current entry point

- [x] `ScenarioSpecifications` in `WarLeague.Test/ScenarioSpecifications.cs` (`public partial class Specifications`)
- [x] `ScenarioBuilder` in `WarLeague.Test/ScenarioBuilder.cs` (fluent async-chainable builder, no DI/context lifecycle)

## Architecture

- `ScenarioBuilder` is a standalone class holding builder methods and tracked state (FormatId, SeasonId, TeamIds, etc.).
- `ScenarioBuilderExtensions` provides `Task<ScenarioBuilder>` extension methods for fluent async chaining.
- `Specifications.NewScenario()` factory wires the builder to existing DI-resolved services.
- Scenario specs use `public partial class Specifications` like all other test files.

## Why so much async / extension boilerplate in ScenarioBuilder?

Every builder method calls service/repository methods that hit the database, so they are
genuinely `async`. Since each method returns `Task<ScenarioBuilder>`, you cannot call
`.NextMethod()` directly on it — C# does not allow chaining on `Task<T>`.

The `ScenarioBuilderExtensions` class exists purely to bridge this: each extension awaits
the previous `Task<ScenarioBuilder>`, then calls the next builder method. This means every
builder method needs a matching one-liner extension, which is mechanical duplication but
enables the clean fluent syntax in specs:

```csharp
await NewScenario()
    .CreateFormat()
    .WithSeason()
    .WithConference("Alpha")
    .WithTeams(4);
```

Without extensions the same code would be:

```csharp
var s = await NewScenario().CreateFormat();
s = await s.WithSeason();
s = await s.WithConference("Alpha");
s = await s.WithTeams(4);
```

This is a deliberate trade-off: readable scenario specs at the cost of extension boilerplate.
