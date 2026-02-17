using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Data.Migrations
{
    /// <inheritdoc />
    public partial class RoundRobinMatchups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoundRobinMatchups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeekId = table.Column<int>(type: "int", nullable: false),
                    Team1Id = table.Column<int>(type: "int", nullable: false),
                    Team2Id = table.Column<int>(type: "int", nullable: false),
                    MatchupType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundRobinMatchups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoundRobinMatchups_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundRobinMatchups_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundRobinMatchups_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_Team1Id",
                table: "RoundRobinMatchups",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_Team2Id",
                table: "RoundRobinMatchups",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_WeekId",
                table: "RoundRobinMatchups",
                column: "WeekId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoundRobinMatchups");
        }
    }
}
