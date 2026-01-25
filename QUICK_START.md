# Quick Start Guide - War League Discord Bot

## Project Overview

A Discord bot for managing Yu-Gi-Oh war league systems with team-based competition, deck submissions, match reporting, and standings tracking. Built with C# (.NET 9) using a clean architecture pattern.

### Key Features

- **Team Management**: Captains can add/drop players, manage rosters
- **Deck Submissions**: Submit .ydk files for each week with format support (HAT, GOAT, Edison, etc.)
- **Match Reporting**: Players report losses with replay URLs
- **Week Management**: Mod-controlled week lifecycle (Open → SubmissionsClosed → Completed)
- **Standings**: Team, individual, and deck format standings with tiebreakers
- **Automated Updates**: Daily standings updates in Discord channels

## Architecture

### Solution Structure

```
WarLeague.sln
├── WarLeague.Core/          # Platform-agnostic domain logic
│   ├── Domain/              # Entities, Enums, Services
│   ├── Data/                # EF Core DbContext and configurations
│   └── Repositories/        # Data access interfaces and implementations
├── WarLeague.Discord/       # Discord bot adapter layer
│   ├── Commands/            # Slash command implementations
│   ├── Services/            # Discord-specific services
│   └── Handlers/            # Command handling infrastructure
└── WarLeague.Core.Tests/    # Unit tests (TDD approach)
```

### Design Principles

- **Separation of Concerns**: Core logic is platform-agnostic (no Discord dependencies)
- **Dependency Injection**: All services use DI for testability
- **Repository Pattern**: Data access abstracted through interfaces
- **State Machines**: Week and Match entities use validated state transitions

## Prerequisites

- .NET 9 SDK
- SQL Server (LocalDB for development, or full SQL Server)
- Discord Bot Token (from https://discord.com/developers/applications)
- Entity Framework Core Tools: `dotnet tool install --global dotnet-ef`

## First Time Setup

### 1. Clone and Restore

```bash
git clone <repository-url>
cd wl
dotnet restore
```

### 2. Configure Secrets

**Set Discord Bot Token:**
```bash
dotnet user-secrets set "Discord:Token" "YOUR_DISCORD_BOT_TOKEN" --project WarLeague.Discord
```

**Set Database Connection String:**
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=WarLeagueDb;Trusted_Connection=True;MultipleActiveResultSets=true" --project WarLeague.Discord
```

**Verify secrets:**
```bash
dotnet user-secrets list --project WarLeague.Discord
```

### 3. Run Database Migrations

```bash
dotnet ef database update --project WarLeague.Core --startup-project WarLeague.Discord
```

This creates all database tables (Players, Teams, Matches, Weeks, DeckSubmissions, Formats).

### 4. Seed Initial Data (Optional)

You'll need to add initial formats (HAT, GOAT, Edison) to the database. You can do this via:
- SQL scripts
- A seed data migration
- Direct database inserts

Example SQL:
```sql
INSERT INTO Formats (Name, Rules) VALUES 
('HAT', '{}'),
('GOAT', '{}'),
('Edison', '{}');
```

### 5. Run the Bot

```bash
dotnet run --project WarLeague.Discord
```

The bot will connect to Discord and register slash commands.

## Configuration

### Configuration Sources (Priority Order)

1. `appsettings.json` - Base configuration (safe to commit, has empty placeholders)
2. `appsettings.{Environment}.json` - Environment-specific (optional)
3. **User Secrets** - Development secrets (stored in user profile, never committed)
4. **Environment Variables** - Production secrets (highest priority)

### Development (User Secrets)

User Secrets are stored in your user profile and never committed to git:

```bash
# Set a secret
dotnet user-secrets set "Discord:Token" "YOUR_TOKEN" --project WarLeague.Discord

# List all secrets
dotnet user-secrets list --project WarLeague.Discord

# Remove a secret
dotnet user-secrets remove "Discord:Token" --project WarLeague.Discord
```

### Production (Environment Variables)

Use environment variables for production deployments:

**Windows:**
```powershell
$env:Discord__Token = "YOUR_TOKEN"
$env:ConnectionStrings__DefaultConnection = "YOUR_CONNECTION_STRING"
```

**Linux/Mac:**
```bash
export Discord__Token="YOUR_TOKEN"
export ConnectionStrings__DefaultConnection="YOUR_CONNECTION_STRING"
```

**Note:** Use double underscores (`__`) for nested configuration keys.

### Getting Discord Bot Token

1. Go to https://discord.com/developers/applications
2. Create/select an application
3. Navigate to "Bot" section
4. Click "Reset Token" or "Copy"
5. Enable required intents (Message Content, Server Members)

## Development Workflow

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test WarLeague.Core.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Creating Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project WarLeague.Core --startup-project WarLeague.Discord

# Apply migrations
dotnet ef database update --project WarLeague.Core --startup-project WarLeague.Discord

# Remove last migration (if not applied)
dotnet ef migrations remove --project WarLeague.Core --startup-project WarLeague.Discord
```

### Building

```bash
# Build solution
dotnet build

# Build specific project
dotnet build WarLeague.Discord

# Build in Release mode
dotnet build -c Release
```

## Key Concepts

### Domain Entities

- **Player**: Discord user, can be in one team, has a role (Player/TeamCaptain/Admin)
- **Team**: Has a captain and roster of players
- **Match**: Round-robin match between players from different teams
- **Week**: Represents a week of competition with state (Open/SubmissionsClosed/Completed)
- **DeckSubmission**: One deck per player per week
- **Format**: Extensible format definitions (HAT, GOAT, etc.) with JSON rules

### State Machines

**Week States:**
- `Open` → Submissions open, captains can submit decks
- `SubmissionsClosed` → Submissions closed, matches can be played
- `Completed` → Week finished

**Match States:**
- `Scheduled` → Match created, not yet played
- `Reported` → Result reported by loser
- `Confirmed` → Result confirmed (future enhancement)

### Round-Robin Match Generation

When a week starts, matches are generated between all players from different teams. For each pair of teams (A, B), a match is created for every player in team A against every player in team B.

### Permissions

- **Player**: Basic role, can report matches
- **TeamCaptain**: Can manage team roster and submit decks
- **Admin**: Full access, can manage weeks, view all data, toggle features

## Common Tasks

### Adding a New Command

1. Create a new class in `WarLeague.Discord/Commands/`
2. Inherit from `InteractionModuleBase<SocketInteractionContext>`
3. Use `[SlashCommand]` attributes
4. Register in `CommandHandler` (automatic via assembly scanning)

Example:
```csharp
[Group("example", "Example commands")]
public class ExampleCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("test", "Test command")]
    public async Task TestCommand()
    {
        await RespondAsync("Test!");
    }
}
```

### Adding a New Service

1. Create interface in `WarLeague.Core/Domain/Services/`
2. Implement in `WarLeague.Core/Domain/Services/`
3. Register in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<IService, Service>();
   ```

### Adding a New Entity

1. Create entity in `WarLeague.Core/Domain/Entities/`
2. Create EF Core configuration in `WarLeague.Core/Data/Configurations/`
3. Add DbSet to `WarLeagueDbContext`
4. Create repository interface and implementation
5. Create migration: `dotnet ef migrations add AddNewEntity`

### Testing a Service

Services are designed for TDD. Example test structure:

```csharp
public class MyServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly MyService _service;

    public MyServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new MyService(_repositoryMock.Object);
    }

    [Fact]
    public async Task MyMethod_WhenCondition_ShouldResult()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(1)).ReturnsAsync(new Entity());

        // Act
        var result = await _service.MyMethod(1);

        // Assert
        result.Should().NotBeNull();
    }
}
```

## Discord Commands Reference

### Team Captain Commands

- `/team add-player <player>` - Add player to your team
- `/team drop-player <player>` - Remove player from your team
- `/team submit-deck <week> <format> <file>` - Submit .ydk deck file
- `/team roster` - View your team's roster

### Player Commands

- `/match report-loss <opponent> <replay-url>` - Report match loss (only losers can report)
- `/match view-replays <week>` - View all replays for a week
- `/match view-results <week>` - View match results for a week

### Moderator Commands

- `/mod start-week <week-number> <start-date> <end-date>` - Start new week
- `/mod close-submissions <week>` - Close submissions for week
- `/mod check-submissions <week>` - Check which teams have submitted
- `/mod view-decks <week> <team>` - View submitted deck lists
- `/mod substitute <player1> <player2> <week>` - Make player substitution
- `/mod toggle-captain-actions` - Enable/disable captain actions
- `/mod announce-week-start <week> [channel]` - Post week start announcement

### General Commands

- `/standings team [week]` - Team standings with tiebreakers
- `/standings individual [week]` - Individual player standings
- `/standings deck [week]` - Deck format standings
- `/standings week-progress [week]` - Week progress (matches completed/pending)

## Database Schema

### Key Tables

- **Players**: Discord users, team membership, roles
- **Teams**: Team info, captain reference
- **Matches**: Week, players, winner, status, replay URL
- **Weeks**: Week number, dates, status
- **DeckSubmissions**: Week, player, team, format, deck file URL
- **Formats**: Format name and JSON rules

### Important Constraints

- Player can only be in one team (unique index on TeamId where not null)
- One deck submission per player per week (unique index on WeekId + PlayerId)
- Week state transitions are validated in code

## Troubleshooting

### "Discord token is not configured"

- Verify User Secret is set: `dotnet user-secrets list --project WarLeague.Discord`
- Check environment variable if in production
- Ensure key name is exactly `Discord:Token`

### Database Connection Fails

- Verify connection string is correct
- Check SQL Server/LocalDB is running
- Ensure database exists or can be created
- Try: `dotnet ef database update --project WarLeague.Core --startup-project WarLeague.Discord`

### Commands Not Appearing in Discord

- Commands are registered globally on bot startup
- Wait a few minutes for Discord to propagate
- Check bot has proper permissions in server
- Verify bot is online and connected

### Migration Errors

- If cascade path errors: Check foreign key delete behaviors in entity configurations
- If constraint errors: Verify unique indexes don't conflict
- Remove failed migration: `dotnet ef migrations remove`

### Tests Failing

- Ensure all dependencies are mocked
- Check test data matches expected entity states
- Verify async/await is used correctly
- Run with verbose output: `dotnet test --verbosity detailed`

## Project Structure Details

### Core Project (`WarLeague.Core`)

**Domain/Entities/**: Domain models
- `Player.cs` - Player entity
- `Team.cs` - Team entity
- `Match.cs` - Match entity
- `Week.cs` - Week entity
- `DeckSubmission.cs` - Deck submission
- `Format.cs` - Format definition

**Domain/Enums/**: Enumerations
- `Role.cs` - Player roles
- `MatchStatus.cs` - Match states
- `WeekStatus.cs` - Week states

**Domain/Services/**: Business logic
- `TeamService.cs` - Team management
- `MatchService.cs` - Match operations
- `WeekService.cs` - Week lifecycle
- `DeckSubmissionService.cs` - Deck submissions
- `StandingsService.cs` - Standings calculations

**Repositories/**: Data access
- Interfaces define contracts
- Implementations use EF Core

**Data/**: EF Core setup
- `WarLeagueDbContext.cs` - Database context
- `Configurations/` - Entity configurations

### Discord Project (`WarLeague.Discord`)

**Commands/**: Slash command implementations
- `TeamCommands.cs` - Team captain commands
- `MatchCommands.cs` - Match reporting commands
- `ModCommands.cs` - Moderator commands
- `StandingsCommands.cs` - Standings viewing

**Services/**: Discord-specific services
- `DiscordBotService.cs` - Bot lifecycle
- `PermissionService.cs` - Permission checking
- `MessageService.cs` - Message management
- `FileValidationService.cs` - File validation
- `DailyStandingsService.cs` - Daily standings updates

**Handlers/**: Command handling
- `CommandHandler.cs` - Routes commands to modules

## Extensibility Points

### Adding New Formats

Formats are stored in the database with JSON rules. Add a new format:

```sql
INSERT INTO Formats (Name, Rules) 
VALUES ('NewFormat', '{"banlist": "...", "rules": "..."}');
```

No code changes needed - the system reads formats from the database.

### Customizing Tiebreaker Formula

Edit `StandingsService.GetTeamStandingsAsync()` and replace the placeholder formula:

```csharp
// Current placeholder
standing.TieBreaker = (double)standing.Wins / totalGames;

// Replace with your formula
standing.TieBreaker = CalculateTieBreaker(standing, matches);
```

### Adding New Commands

Commands are automatically discovered via assembly scanning. Just create a new class inheriting from `InteractionModuleBase<SocketInteractionContext>` with `[SlashCommand]` attributes.

## Production Deployment

### Environment Variables

Set these environment variables:

- `Discord__Token` - Discord bot token
- `ConnectionStrings__DefaultConnection` - Database connection string
- `ASPNETCORE_ENVIRONMENT` - Set to "Production"

### Database

1. Run migrations on production database
2. Seed initial data (formats)
3. Ensure connection string is correct

### Running as Service

**Windows (NSSM):**
```bash
nssm install WarLeagueBot "C:\path\to\dotnet.exe" "C:\path\to\WarLeague.Discord.dll"
```

**Linux (systemd):**
Create `/etc/systemd/system/warleaguebot.service`:
```ini
[Unit]
Description=War League Discord Bot

[Service]
ExecStart=/usr/bin/dotnet /path/to/WarLeague.Discord.dll
Restart=always
Environment=Discord__Token=YOUR_TOKEN
Environment=ConnectionStrings__DefaultConnection=YOUR_CONNECTION_STRING

[Install]
WantedBy=multi-user.target
```

## Additional Resources

- **Specification**: See `specification.md` for original requirements
- **Context**: See `context.md` for implementation details and design decisions
- **Setup Guide**: See `SETUP.md` for detailed configuration instructions

## Getting Help

- Check logs for error messages
- Verify configuration with `dotnet user-secrets list`
- Test database connection independently
- Review entity configurations for constraint issues
- Check Discord bot permissions and intents
