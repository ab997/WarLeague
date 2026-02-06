using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;

namespace WarLeague.Test;

public class DatabaseFixtureSeeded : IDisposable
{
    public WarLeagueDbContext DbContext { get; }
    public string ConnectionString { get; }

    public DatabaseFixtureSeeded()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
            .Build();

        ConnectionString = configuration.GetConnectionString("TestConnection")
            ?? throw new InvalidOperationException("Test connection string not found in appsettings.Test.json");

        var optionsBuilder = new DbContextOptionsBuilder<WarLeagueDbContext>();
        optionsBuilder.UseSqlServer(ConnectionString);

        DbContext = new WarLeagueDbContext(optionsBuilder.Options);

        RecreateDatabase();
    }

    /// <summary>
    /// Creates a new DbContext instance for isolated test operations.
    /// Use this with transactions to ensure test isolation.
    /// </summary>
    public WarLeagueDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<WarLeagueDbContext>();
        optionsBuilder.UseSqlServer(ConnectionString);
        return new WarLeagueDbContext(optionsBuilder.Options);
    }

    private void RecreateDatabase()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
    }

    

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}


