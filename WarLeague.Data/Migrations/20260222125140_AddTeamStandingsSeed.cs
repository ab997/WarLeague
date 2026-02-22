using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamStandingsSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Seed",
                table: "TeamStandings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Backfill Seed so it equals tiebreaker-ordered standing per season (1 = first, 2 = second, ...)
            migrationBuilder.Sql(@"
                WITH Ordered AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY SeasonId ORDER BY Tiebreaker DESC, TeamId) AS rn
                    FROM TeamStandings
                )
                UPDATE t SET t.Seed = o.rn
                FROM TeamStandings t
                INNER JOIN Ordered o ON t.Id = o.Id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Seed",
                table: "TeamStandings");
        }
    }
}
