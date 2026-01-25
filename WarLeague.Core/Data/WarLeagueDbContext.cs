using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data.Entities;


namespace WarLeague.Core.Data;

public class WarLeagueDbContext : DbContext
{
    public WarLeagueDbContext(DbContextOptions<WarLeagueDbContext> options) : base(options)
    {
    }
    public DbSet<DeckSubmission> DeckSubmissions { get; set; }
    public DbSet<Format> Formats { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Week> Weeks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //--------------------------------------
        // relationships that cannot be established by EF conventions
        //--------------------------------------

        // Player ↔ Team (membership)
        modelBuilder.Entity<Player>()
            .HasOne(p => p.Team)
            .WithMany(t => t.Players)
            .HasForeignKey(p => p.TeamId)
            .IsRequired(false);

        // Team ↔ Player (captain)
        modelBuilder.Entity<Team>()
            .HasOne(t => t.Captain)
            .WithMany() // no inverse navigation
            .HasForeignKey(t => t.CaptainId);

        // Match -> Player1
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player1)
            .WithMany(p => p.MatchesAsPlayer1)
            .HasForeignKey(m => m.Player1Id);

        // Match -> Player2
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player2)
            .WithMany(p => p.MatchesAsPlayer2)
            .HasForeignKey(m => m.Player2Id);

        // Match -> Winner (nullable)
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Winner)
            .WithMany(p => p.MatchesWon)
            .HasForeignKey(m => m.WinnerId);

        //--------------------------------------
        // enums as strings
        //--------------------------------------
        modelBuilder.Entity<Week>()
        .Property(w => w.Status)
        .HasConversion<string>();

        modelBuilder.Entity<Match>()
        .Property(w => w.Status)
        .HasConversion<string>();

        modelBuilder.Entity<Player>()
      .Property(w => w.Role)
      .HasConversion<string>();

        //--------------------------------------
        // unique indexes
        //--------------------------------------
        modelBuilder.Entity<Format>()
            .HasIndex(f => f.Name)
            .IsUnique();

        modelBuilder.Entity<Player>()
            .HasIndex(p => p.DiscordUserId)
            .IsUnique();

        modelBuilder.Entity<Team>()
            .HasIndex(t => t.Name)
            .IsUnique();

        // Season: unique per (FormatId, SeasonNumber)
        modelBuilder.Entity<Season>()
            .HasIndex(s => new { s.FormatId, s.SeasonNumber })
            .IsUnique();

        // Week: unique per (SeasonId, WeekNumber)
        modelBuilder.Entity<Week>()
            .HasIndex(w => new { w.SeasonId, w.WeekNumber })
            .IsUnique();

        //--------------------------------------
        // Ensure only one active Format, Season and Week at a time (filtered unique indexes)
        // Note: .HasFilter uses SQL Server syntax ("[Active] = 1"); adjust if using another provider.
        //--------------------------------------
        modelBuilder.Entity<Format>()
            .HasIndex(f => f.Active)
            .IsUnique()
            .HasFilter("[Active] = 1");

        modelBuilder.Entity<Season>()
            .HasIndex(s => s.Active)
            .IsUnique()
            .HasFilter("[Active] = 1");


        //--------------------------------------
        // global query filters
        //--------------------------------------
        modelBuilder.Entity<Season>()
        .HasQueryFilter(s => s.Format.Active);

        modelBuilder.Entity<Week>()
        .HasQueryFilter(w => w.Season.Active);

        //--------------------------------------
        // disable cascade delete
        //--------------------------------------
        foreach (var fk in modelBuilder.Model
        .GetEntityTypes()
        .SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
