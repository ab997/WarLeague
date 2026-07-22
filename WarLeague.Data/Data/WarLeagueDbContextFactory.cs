using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WarLeague.Data;
/// <summary>
/// Its purpose is design-time DbContext creation for EF Core tools.
/// </summary>
//public class WarLeagueDbContextFactory : IDesignTimeDbContextFactory<WarLeagueDbContext>
//{
//    public WarLeagueDbContext CreateDbContext(string[] args)
//    {
//        var optionsBuilder = new DbContextOptionsBuilder<WarLeagueDbContext>();
//        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WarLeagueDb;Trusted_Connection=True;MultipleActiveResultSets=true");

//        return new WarLeagueDbContext(optionsBuilder.Options);
//    }
//}
public class WarLeagueDbContextFactory
    : IDesignTimeDbContextFactory<WarLeagueDbContext>
{
    public WarLeagueDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<WarLeagueDbContext>();

        optionsBuilder.UseNpgsql();

        return new WarLeagueDbContext(optionsBuilder.Options);
    }
}