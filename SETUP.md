# Setup Guide - War League Discord Bot

## Configuration Setup

This project uses secure configuration management to protect sensitive data like Discord tokens and database connection strings.

### Development Setup (User Secrets)

For local development, use .NET User Secrets (stored in your user profile, never committed to git):

1. **Set Discord Token:**
   ```bash
   dotnet user-secrets set "Discord:Token" "YOUR_DISCORD_BOT_TOKEN" --project WarLeague.Discord
   ```

2. **Set Database Connection String:**
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=WarLeagueDb;Trusted_Connection=True;MultipleActiveResultSets=true" --project WarLeague.Discord
   ```

3. **Verify secrets are set:**
   ```bash
   dotnet user-secrets list --project WarLeague.Discord
   ```

### Production Setup (Environment Variables)

For production deployments, use environment variables:

**Windows:**
```powershell
$env:Discord__Token = "YOUR_DISCORD_BOT_TOKEN"
$env:ConnectionStrings__DefaultConnection = "YOUR_CONNECTION_STRING"
```

**Linux/Mac:**
```bash
export Discord__Token="YOUR_DISCORD_BOT_TOKEN"
export ConnectionStrings__DefaultConnection="YOUR_CONNECTION_STRING"
```

**Docker:**
```yaml
environment:
  - Discord__Token=YOUR_DISCORD_BOT_TOKEN
  - ConnectionStrings__DefaultConnection=YOUR_CONNECTION_STRING
```

### Configuration Priority

The configuration system loads settings in this order (later sources override earlier ones):

1. `appsettings.json` (base configuration, can be empty for sensitive values)
2. `appsettings.{Environment}.json` (optional, environment-specific)
3. User Secrets (development only)
4. Environment Variables (highest priority)

### Getting Your Discord Bot Token

1. Go to https://discord.com/developers/applications
2. Create a new application or select an existing one
3. Go to the "Bot" section
4. Click "Reset Token" or "Copy" to get your bot token
5. Make sure to enable "Message Content Intent" if needed

### Database Connection String

For local development with SQL Server LocalDB:
```
Server=(localdb)\mssqllocaldb;Database=WarLeagueDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

For SQL Server:
```
Server=localhost;Database=WarLeagueDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True
```

### Security Notes

- ✅ User Secrets are stored in your user profile (`%APPDATA%\Microsoft\UserSecrets\` on Windows)
- ✅ User Secrets are never committed to git
- ✅ Environment variables are the recommended approach for production
- ✅ `appsettings.json` should not contain real secrets (use empty strings or placeholders)
- ✅ `appsettings.Development.json.example` is a template - copy it to `appsettings.Development.json` if needed (and add to .gitignore)

### Troubleshooting

**"Discord token is not configured" error:**
- Make sure you've set the User Secret or environment variable
- Check the key name matches exactly: `Discord:Token`

**Database connection fails:**
- Verify your connection string is correct
- Make sure SQL Server/LocalDB is running
- Check that the database exists or can be created
