using Microsoft.EntityFrameworkCore;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;


namespace WarLeague.Data;

public class WarLeagueDbContext : DbContext
{
    public WarLeagueDbContext(DbContextOptions<WarLeagueDbContext> options) : base(options)
    {
    }
    public DbSet<Conference> Conferences { get; set; }
    public DbSet<DeckSubmission> DeckSubmissions { get; set; }
    public DbSet<Format> Formats { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Week> Weeks { get; set; }
    public DbSet<PlayerSeasonTeam> PlayerSeasonTeams { get; set; }
    public DbSet<RolePermissionMapping> RolePermissionMappings { get; set; }
    public DbSet<RoundRobinMatchup> RoundRobinMatchups { get; set; }
    public DbSet<PlayoffMatchup> PlayoffMatchups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //--------------------------------------
        // relationships that cannot be established by EF conventions
        //--------------------------------------

        // Team ↔ Player (captain)
        modelBuilder.Entity<Team>()
            .HasOne(t => t.Captain)
            .WithMany() // no inverse navigation
            .HasForeignKey(t => t.CaptainId);

        // Match -> Player1
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player1)
            .WithMany()
            .HasForeignKey(m => m.Player1Id);

        // Match -> Player2
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Player2)
            .WithMany()
            .HasForeignKey(m => m.Player2Id);

        // Match -> Winner (nullable)
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Winner)
            .WithMany()
            .HasForeignKey(m => m.WinnerId);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Team1)
            .WithMany()
            .HasForeignKey(m => m.Team1Id);

         modelBuilder.Entity<Match>()
            .HasOne(m => m.Team2)
            .WithMany()
            .HasForeignKey(m => m.Team2Id);

        modelBuilder.Entity<Match>()
           .HasOne(m => m.WinnerTeam)
           .WithMany()
           .HasForeignKey(m => m.WinnerTeamId);

        modelBuilder.Entity<RoundRobinMatchup>()
           .HasOne(m => m.Team1)
           .WithMany()
           .HasForeignKey(m => m.Team1Id);

        modelBuilder.Entity<RoundRobinMatchup>()
           .HasOne(m => m.Team2)
           .WithMany()
           .HasForeignKey(m => m.Team2Id);

        modelBuilder.Entity<RoundRobinMatchup>()
          .HasOne(m => m.TeamWinner)
          .WithMany()
          .HasForeignKey(m => m.TeamWinnerId);

        modelBuilder.Entity<PlayoffMatchup>()
           .HasOne(m => m.Team1)
           .WithMany()
           .HasForeignKey(m => m.Team1Id);

        modelBuilder.Entity<PlayoffMatchup>()
           .HasOne(m => m.Team2)
           .WithMany()
           .HasForeignKey(m => m.Team2Id);

        modelBuilder.Entity<PlayoffMatchup>()
          .HasOne(m => m.TeamWinner)
          .WithMany()
          .HasForeignKey(m => m.TeamWinnerId);

        //--------------------------------------
        // check constraints
        //--------------------------------------
        // Match: prevent self-play
        modelBuilder.Entity<Match>()
            .ToTable(t => t.HasCheckConstraint("CK_Match_NoSelfPlay", "[Player1Id] <> [Player2Id]"));

        //--------------------------------------
        // enums as strings
        //--------------------------------------
        modelBuilder.Entity<Week>()
            .Property(w => w.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Match>()
            .Property(w => w.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Match>()
            .Property(w => w.MatchResultType)
            .HasConversion<string>();

        modelBuilder.Entity<RolePermissionMapping>()
            .Property(w => w.PermissionType)
            .HasConversion<string>();

        modelBuilder.Entity<RoundRobinMatchup>()
            .Property(w => w.MatchupType)
            .HasConversion<string>();

        modelBuilder.Entity<PlayoffMatchup>()
            .Property(p => p.MatchupType)
            .HasConversion<string>();

        modelBuilder.Entity<Season>()
            .Property(s => s.Phase)
            .HasConversion<string>();

        //--------------------------------------
        // unique indexes
        //--------------------------------------
        modelBuilder.Entity<Format>()
            .HasIndex(f => new { f.GuildId, f.Name })
            .IsUnique();

        modelBuilder.Entity<Format>()
          .HasIndex(f => new { f.GuildId, f.SingleFormatMode })
          .IsUnique()
          .HasFilter("[SingleFormatMode] = 1");

        modelBuilder.Entity<Player>()
            .HasIndex(p => p.DiscordUserId)
            .IsUnique();

        modelBuilder.Entity<Team>()
            .HasIndex(t => new { t.SeasonId, t.Name })
            .IsUnique();

        modelBuilder.Entity<Team>()
            .HasIndex(t => new { t.SeasonId, t.CaptainId })
            .IsUnique();

        modelBuilder.Entity<Season>()
            .HasIndex(s => new { s.FormatId, s.SeasonNumber })
            .IsUnique();

        modelBuilder.Entity<Conference>()
            .HasIndex(c => new { c.SeasonId, c.Name })
            .IsUnique();

        modelBuilder.Entity<Season>()
            .HasIndex(s => new { s.FormatId, s.Active })
            .IsUnique()
            .HasFilter("[Active] = 1");

        modelBuilder.Entity<Week>()
            .HasIndex(w => new { w.SeasonId, w.WeekNumber })
            .IsUnique();

        // Week: only one non-Completed and non-NotOpenYet per SeasonId
        // Status is stored as string via HasConversion<string>() above, so we filter on string values
        modelBuilder.Entity<Week>()
            .HasIndex(w => new { w.SeasonId, w.Status })
            .IsUnique()
            .HasFilter("[Status] <> 'Completed' and [Status] <> 'NotOpenYet'");

        // PlayerSeasonTeam: unique per (PlayerId, SeasonId) -> a player can only be in one team per season
        modelBuilder.Entity<PlayerSeasonTeam>()
            .HasIndex(x => new { x.PlayerId, x.SeasonId })
            .IsUnique();

        modelBuilder.Entity<DeckSubmission>()
            .HasIndex(x => new { x.PlayerId, x.WeekId })
            .IsUnique();

        modelBuilder.Entity<RolePermissionMapping>()
            .HasIndex(w => new { w.GuildId, w.PermissionType })
            .IsUnique();

        // RoundRobinMatchup: prevent duplicate team matchups per week
        // Note: BYE matchups (Team1Id == Team2Id) are allowed, but normal matchups must be unique
        // We ensure Team1Id <= Team2Id in application code, so this index prevents duplicates
        modelBuilder.Entity<RoundRobinMatchup>()
            .HasIndex(r => new { r.WeekId, r.Team1Id, r.Team2Id })
            .IsUnique();

        // PlayoffMatchup: unique bracket position per week
        modelBuilder.Entity<PlayoffMatchup>()
            .HasIndex(p => new { p.WeekId, p.BracketPosition })
            .IsUnique();

        // PlayoffMatchup: prevent duplicate team matchups per week
        // Note: BYE matchups (Team1Id == Team2Id) are allowed, but normal matchups must be unique
        // We ensure Team1Id <= Team2Id in application code, so this index prevents duplicates
        modelBuilder.Entity<PlayoffMatchup>()
            .HasIndex(p => new { p.WeekId, p.Team1Id, p.Team2Id })
            .IsUnique();

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
