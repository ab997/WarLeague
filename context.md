# War League Discord Bot - Context

## Project Overview

This is a Discord bot written in C# (.NET 9) that manages a Yu-Gi-Oh war league system. The bot supports team-based competition with deck submissions, match reporting, and standings tracking.

## Architecture

The solution follows a clean architecture pattern with clear separation between domain logic and the Discord interface:

### Projects

1. **WarLeague.Core** - Platform-agnostic domain logic
   - Domain entities and enums
   - Business services (Team, Match, Week, DeckSubmission, Standings)
   - Repository interfaces and EF Core implementations
   - Entity Framework Core DbContext and configurations
   - No Discord dependencies

2. **WarLeague.Discord** - Discord bot adapter layer
   - Discord.NET integration
   - Slash command implementations
   - Permission and message management services
   - Background services for daily standings updates
   - Acts purely as an adapter, delegates to Core services

3. **WarLeague.Core.Tests** - Unit tests (TDD approach)
   - Test infrastructure set up with xUnit, Moq, FluentAssertions
   - Ready for TDD implementation

## Key Design Decisions

### Domain Model

- **Player**: Can only be in one team at a time (enforced by unique index on TeamId)
- **Team**: Has a captain (Player) and roster (List<Player>)
- **Match**: Round-robin generation between all players of different teams
- **Week**: State machine (Open → SubmissionsClosed → Completed)
- **DeckSubmission**: One per player per week (enforced by unique index)
- **Format**: Extensible via JSON rules field

### State Management

- **Week Status**: Open (submissions open) → SubmissionsClosed → Completed
- **Match Status**: Scheduled → Reported → Confirmed
- State transitions are validated in services to prevent invalid operations

### Round-Robin Match Generation

The `MatchService.GenerateRoundRobinMatchesAsync` creates matches between all players from different teams. For each pair of teams, it creates a match between every player from team A and every player from team B.

### Standings Calculation

- **Team Standings**: Based on wins/losses, with placeholder tiebreaker formula (currently win rate)
- **Individual Standings**: Player win/loss records with win rate
- **Deck Standings**: Format-based performance tracking
- Tiebreaker formula is a placeholder to be replaced with actual formula later

### Permission System

- **Role Enum**: Player, TeamCaptain, Admin
- Players are assigned roles in the database
- PermissionService maps Discord users to domain roles
- Commands check permissions before execution

### File Handling

- **Deck Submissions**: Accept .ydk file uploads via Discord attachments
- **Replay URLs**: Validated as HTTP/HTTPS URLs
- FileValidationService provides validation logic

## Database

- **Provider**: SQL Server (LocalDB for development)
- **Migrations**: Code-first with EF Core
- **Connection String**: Configured in `appsettings.json`
- **Initial Migration**: `InitialCreate` migration created

## Discord Bot Features

### Commands Implemented

#### Team Captain Commands
- `/team add-player` - Add player to team (requires captain role, captain actions enabled)
- `/team drop-player` - Remove player from team
- `/team submit-deck` - Submit .ydk deck file for a week
- `/team roster` - View current team roster

#### Player Commands
- `/match report-loss` - Report match loss with replay URL (only losers can report)
- `/match view-replays` - View all replays for a week
- `/match view-results` - View match results for a week

#### Moderator Commands
- `/mod start-week` - Start new week (opens submissions)
- `/mod close-submissions` - Close submissions for current week
- `/mod check-submissions` - Check which teams have/haven't submitted
- `/mod view-decks` - View submitted deck lists for a team
- `/mod substitute` - Make player substitution (placeholder - not fully implemented)
- `/mod toggle-captain-actions` - Enable/disable captain add/drop functionality
- `/mod announce-week-start` - Post week start announcement

#### General Commands
- `/standings team` - Team standings with tiebreakers
- `/standings individual` - Individual player standings
- `/standings deck` - Deck format standings
- `/standings week-progress` - Week progress (matches completed/pending)

### Background Services

- **DailyStandingsService**: Runs daily at 9 AM UTC, updates standings in channels with "standings" in the name, deletes previous message before posting new one

## Configuration

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WarLeagueDb;..."
  },
  "Discord": {
    "Token": "YOUR_BOT_TOKEN_HERE"
  },
  "WarLeague": {
    "CaptainActionsEnabled": true
  }
}
```

## Current Implementation Status

### Completed
- ✅ All domain entities and EF Core configurations
- ✅ Repository pattern with EF Core implementations
- ✅ All business services (Team, Match, Week, DeckSubmission, Standings)
- ✅ Discord bot setup and command infrastructure
- ✅ All slash commands implemented
- ✅ Permission system
- ✅ File validation (YDK files, replay URLs)
- ✅ Daily standings auto-update service
- ✅ Weekly announcement system

### Placeholders / TODOs
- ⚠️ **Deck Legality Check**: `IsValidated` field exists but validation logic not implemented (marked as TODO)
- ⚠️ **Tiebreaker Formula**: Currently using win rate as placeholder, needs actual formula
- ⚠️ **Player Substitution**: Command exists but substitution logic not fully implemented
- ⚠️ **Captain Actions Toggle**: Configuration exists but persistence not implemented (in-memory only)

## Dependencies

### Core Project
- Microsoft.EntityFrameworkCore (9.0.0)
- Microsoft.EntityFrameworkCore.SqlServer (9.0.0)
- Microsoft.EntityFrameworkCore.Design (9.0.0)

### Discord Project
- Discord.Net (3.17.0)
- Microsoft.Extensions.Hosting (9.0.0)
- Microsoft.Extensions.Configuration (9.0.0)
- Microsoft.Extensions.Configuration.Json (9.0.0)
- Microsoft.Extensions.Logging (9.0.0)
- Entity Framework Core packages (for DbContext access)

### Tests Project
- xUnit (via template)
- Moq (4.20.72)
- FluentAssertions (6.12.1)

## Development Notes

### Running the Bot

1. Configure `appsettings.json` with Discord token and database connection
2. Run migrations: `dotnet ef database update --project WarLeague.Core --startup-project WarLeague.Discord`
3. Seed initial data (formats, etc.)
4. Run: `dotnet run --project WarLeague.Discord`

### Testing

The test project is set up but tests haven't been written yet. The architecture supports TDD - all services use dependency injection and repository interfaces, making them easily testable.

### Extensibility

- **Formats**: New formats can be added to the database without code changes (rules stored as JSON)
- **Standings**: Tiebreaker formula can be replaced in `StandingsService.GetTeamStandingsAsync`
- **Commands**: New commands can be added by creating new interaction modules

## Known Limitations

1. Season management is hardcoded to season 1 in several places
2. Daily standings service looks for channels with "standings" in the name (could be configurable)
3. Captain actions toggle is not persisted (in-memory only)
4. Player substitution logic needs full implementation
5. No automatic week completion - must be done manually by mods

## Future Enhancements

- Implement deck legality validation
- Add season management
- Persist captain actions toggle in database
- Complete player substitution logic
- Add match confirmation workflow
- Add more detailed error messages and validation
- Implement unit tests following TDD approach
- Add logging and monitoring
- Add configuration for standings channel selection
