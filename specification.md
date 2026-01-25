# War League Discord Bot - Specification

## Original Requirements

### Overview
A Discord bot written in C# that supports a war league system in a Yu-Gi-Oh server.

### Format Support
- Initially supports HAT format
- Must be extensible to support other formats (GOAT, Edison, etc.)

### War League Structure
- War league consists of teams
- Each team has a roster of players
- A player can only be in 1 team

### Roles
- **Player**: Basic player role
- **Team Captain**: Can manage team roster
- **Admin**: Full administrative access

### Team Captain Features
- Can add and drop players from their team
- Can submit deck lists for the team
- Admin can disable captain add/drop functionality

### Deck Submission
- Command for captains to submit deck lists
- TODO functionality to check for legality based on format
- Assume deck type if provided by user in Discord
- Deck submissions are .ydk files

### Match Reporting
- Functionality for loser to report losses of their match
- Submit replay URLs with match results
- Replays and results accessible via commands

### Week Management
- Make an announcement at the start of a week
- Let a mod close submissions and start a new week (which opens submissions for the next one)
- Manual control (not automatic) - mods decide when to close/start weeks
- Mod command to check which teams have submitted and which are missing
- Command for mods to check submitted deck lists
- Allow mods to make a substitution of a player

### Standings
- Either by command or in a separate channel updated daily:
  - Print out Team standings
  - Print out Individual standings
  - Print out Deck standings
  - Show progress of the week
- Team standings use tiebreakers (TB formula to be explained later - use placeholder)
- If using daily printout, delete the outdated message

## Architecture Requirements

### System Overview

The system consists of two tightly coupled but logically independent projects within a single .NET solution:

#### Core Logic Project
- Contains all domain models, rules, validations, state machines, and workflows
- Fully platform-agnostic (no Discord-specific dependencies)
- Developed using Test-Driven Development (TDD)
- Uses Entity Framework Core with code-first migrations
- Persists data to a local Microsoft SQL Server database during development

#### Discord Bot Project
- Acts purely as an adapter/interface layer
- Translates Discord commands, interactions, and events into Core Logic invocations
- Handles permissions, channels, and message lifecycle (posting, updating, deleting)
- Contains no business rules beyond input sanitation and authorization checks

### Technical Requirements

- **.NET Version**: .NET 10 (implemented as .NET 9 due to availability)
- **Database**: Local MS SQL Server
- **ORM**: Entity Framework Core with code-first migrations
- **Testing**: TDD approach for Core logic
- **Discord Library**: Discord.NET

## Implementation Plan (As Executed)

### Phase 1: Foundation ✅
1. Create solution and projects
2. Set up EF Core with SQL Server connection
3. Create initial migration for core entities
4. Set up TDD test project structure

### Phase 2: Core Domain (TDD) ✅
1. Implement Team/Player entities and business rules
2. Implement Match entity and round-robin generation logic
3. Implement Week and submission management
4. Implement Standings calculations (with placeholder TB)

### Phase 3: Discord Integration ✅
1. Set up Discord.NET bot with slash commands
2. Implement permission checking
3. Implement team captain commands
4. Implement player commands
5. Implement mod commands

### Phase 4: Advanced Features ✅
1. Weekly announcement system
2. Daily standings auto-update with message deletion
3. Deck submission validation (TODO placeholder)
4. Replay and results viewing

### Phase 5: Polish & Testing
1. Error handling and validation ✅
2. Integration testing (pending)
3. Documentation ✅

## Command Specifications

### Team Captain Commands
- `/team add-player <player>` - Add player to team
- `/team drop-player <player>` - Remove player from team
- `/team submit-deck <week> <file>` - Submit deck for week (.ydk file)
- `/team roster` - View current team roster

### Player Commands
- `/match report-loss <opponent> <replay-url>` - Report match loss with replay
- `/match view-replays <week>` - View replays for a week
- `/match view-results <week>` - View match results

### Moderator Commands
- `/mod start-week <week-number>` - Start new week (opens submissions)
- `/mod close-submissions <week>` - Close submissions for current week
- `/mod check-submissions <week>` - Check which teams have/haven't submitted
- `/mod view-decks <week> <team>` - View submitted deck lists
- `/mod substitute <player1> <player2> <week>` - Make player substitution
- `/mod toggle-captain-actions` - Enable/disable captain add/drop functionality
- `/mod announce-week-start <week>` - Post week start announcement

### General Commands
- `/standings team <week>` - Team standings with TB
- `/standings individual <week>` - Individual player standings
- `/standings deck <week>` - Deck performance standings
- `/standings week-progress <week>` - Show week progress (matches completed, pending)

## Data Model Requirements

### Entities
- **Team**: Id, Name, CaptainId, Roster, IsActive, CreatedDate
- **Player**: Id, DiscordUserId, DiscordUsername, TeamId (nullable), Role, IsActive
- **Match**: Id, WeekId, Player1Id, Player2Id, WinnerId, Status, ReportedBy, ReportedDate, ReplayUrl
- **DeckSubmission**: Id, WeekId, PlayerId, TeamId, FormatId, DeckFileUrl, SubmittedDate, IsValidated
- **Week**: Id, WeekNumber, SeasonId, StartDate, EndDate, Status, SubmissionsClosedDate
- **Format**: Id, Name, Rules (JSON for extensibility)

### Constraints
- Player can only be in one team (unique constraint on TeamId where not null)
- One deck submission per player per week (unique index on WeekId + PlayerId)
- Week state transitions must be validated

## Business Rules

1. **Player Uniqueness**: A player can only be in one team at a time
2. **Captain Actions**: Can be disabled by admin
3. **Match Reporting**: Only losers can report match results
4. **Deck Submissions**: Only allowed when week status is Open
5. **Week Lifecycle**: Open → SubmissionsClosed → Completed (state machine)
6. **Round-Robin**: Matches generated between all players from different teams

## Extensibility Requirements

1. **Formats**: Format entity stores rules as JSON, allowing new formats without code changes
2. **Tiebreakers**: Placeholder formula in StandingsService, easily replaceable
3. **Commands**: New commands can be added via Discord.NET interaction modules

## Testing Requirements

- Core logic should be developed using TDD
- Test project structure set up with xUnit, Moq, FluentAssertions
- Services use dependency injection for testability

## Deployment Notes

- Local SQL Server for development
- Connection string in appsettings.json
- Discord bot token in appsettings.json
- EF Core migrations for database schema management
