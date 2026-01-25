# War League Discord Bot

A Discord bot for managing Yu-Gi-Oh war league systems, built with C# and .NET 9.

## Architecture

The solution consists of three projects:

- **WarLeague.Core**: Platform-agnostic domain logic, entities, services, and repositories
- **WarLeague.Discord**: Discord bot adapter layer using Discord.NET
- **WarLeague.Core.Tests**: Unit tests for core logic (TDD approach)

## Features

### Team Management
- Team captains can add/drop players from their teams
- Admin can enable/disable captain actions
- Players can only be in one team at a time

### Deck Submissions
- Team captains submit decks (.ydk files) for each week
- Supports multiple formats (HAT, GOAT, Edison, etc.) - extensible via JSON rules
- Deck legality validation placeholder (TODO)

### Match Reporting
- Players report losses with replay URLs
- Round-robin match generation
- View replays and results by week

### Week Management
- Mods start new weeks (opens submissions)
- Mods close submissions manually
- Week state machine: Open → SubmissionsClosed → Completed

### Standings
- Team standings with tiebreaker (placeholder formula)
- Individual player standings
- Deck format standings
- Week progress tracking
- Daily automatic standings updates (deletes/updates previous message)

### Commands

#### Team Captain Commands
- `/team add-player <player>` - Add player to team
- `/team drop-player <player>` - Remove player from team
- `/team submit-deck <week> <format> <file>` - Submit deck for week
- `/team roster` - View team roster

#### Player Commands
- `/match report-loss <opponent> <replay-url>` - Report match loss
- `/match view-replays <week>` - View replays for a week
- `/match view-results <week>` - View match results

#### Moderator Commands
- `/mod start-week <week-number> <start-date> <end-date>` - Start new week
- `/mod close-submissions <week>` - Close submissions for week
- `/mod check-submissions <week>` - Check which teams have submitted
- `/mod view-decks <week> <team>` - View submitted deck lists
- `/mod substitute <player1> <player2> <week>` - Make player substitution
- `/mod toggle-captain-actions` - Enable/disable captain actions
- `/mod announce-week-start <week> [channel]` - Post week start announcement

#### General Commands
- `/standings team [week]` - Team standings
- `/standings individual [week]` - Individual standings
- `/standings deck [week]` - Deck format standings
- `/standings week-progress [week]` - Week progress

## Setup

1. **Configure Database**
   - Update connection string in `WarLeague.Discord/appsettings.json`
   - Run migrations: `dotnet ef database update --project WarLeague.Core --startup-project WarLeague.Discord`

2. **Configure Discord Bot**
   - Get bot token from Discord Developer Portal
   - Add token to `WarLeague.Discord/appsettings.json` under `Discord:Token`

3. **Seed Initial Data**
   - Add formats (HAT, GOAT, Edison) to the database
   - Register players and teams

4. **Run the Bot**
   - `dotnet run --project WarLeague.Discord`

## Database

Uses Entity Framework Core with SQL Server (LocalDB for development). Code-first migrations are set up.

## Notes

- Deck legality checking is a TODO placeholder
- Tiebreaker formula is a placeholder (to be replaced with actual formula)
- Player substitution logic needs to be implemented
- Daily standings service looks for channels with "standings" in the name

## Dependencies

- .NET 9
- Discord.NET 3.17.0
- Entity Framework Core 9.0.0
- SQL Server
