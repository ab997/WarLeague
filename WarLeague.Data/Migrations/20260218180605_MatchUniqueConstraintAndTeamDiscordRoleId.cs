using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchUniqueConstraintAndTeamDiscordRoleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoundRobinMatchups_WeekId",
                table: "RoundRobinMatchups");

            migrationBuilder.DropIndex(
                name: "IX_Matches_WeekId",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Match_NoSelfPlay",
                table: "Matches");

            // Normalize existing matches to canonical order (Player1Id < Player2Id) before adding new constraint
            migrationBuilder.Sql(@"
UPDATE Matches SET
  Player1Id = Player2Id,
  Player2Id = Player1Id,
  WinnerId = CASE
    WHEN WinnerId = Player1Id THEN Player2Id
    WHEN WinnerId = Player2Id THEN Player1Id
    ELSE WinnerId
  END
WHERE Player1Id > Player2Id;
");

            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "Seasons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PlayoffTeamsCount",
                table: "Conferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlayoffMatchups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeekId = table.Column<int>(type: "int", nullable: false),
                    Team1Id = table.Column<int>(type: "int", nullable: false),
                    Team2Id = table.Column<int>(type: "int", nullable: false),
                    MatchupType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeamWinnerId = table.Column<int>(type: "int", nullable: true),
                    Round = table.Column<int>(type: "int", nullable: false),
                    BracketPosition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffMatchups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffMatchups_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayoffMatchups_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayoffMatchups_Teams_TeamWinnerId",
                        column: x => x.TeamWinnerId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayoffMatchups_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SeasonId_DiscordRoleId",
                table: "Teams",
                columns: new[] { "SeasonId", "DiscordRoleId" },
                unique: true,
                filter: "[DiscordRoleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_WeekId_Team1Id_Team2Id",
                table: "RoundRobinMatchups",
                columns: new[] { "WeekId", "Team1Id", "Team2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WeekId_Player1Id_Player2Id",
                table: "Matches",
                columns: new[] { "WeekId", "Player1Id", "Player2Id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Match_CanonicalOrder",
                table: "Matches",
                sql: "[Player1Id] < [Player2Id]");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_Team1Id",
                table: "PlayoffMatchups",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_Team2Id",
                table: "PlayoffMatchups",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_TeamWinnerId",
                table: "PlayoffMatchups",
                column: "TeamWinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_WeekId_BracketPosition",
                table: "PlayoffMatchups",
                columns: new[] { "WeekId", "BracketPosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_WeekId_Team1Id_Team2Id",
                table: "PlayoffMatchups",
                columns: new[] { "WeekId", "Team1Id", "Team2Id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoffMatchups");

            migrationBuilder.DropIndex(
                name: "IX_Teams_SeasonId_DiscordRoleId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_RoundRobinMatchups_WeekId_Team1Id_Team2Id",
                table: "RoundRobinMatchups");

            migrationBuilder.DropIndex(
                name: "IX_Matches_WeekId_Player1Id_Player2Id",
                table: "Matches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Match_CanonicalOrder",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Phase",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "PlayoffTeamsCount",
                table: "Conferences");

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_WeekId",
                table: "RoundRobinMatchups",
                column: "WeekId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WeekId",
                table: "Matches",
                column: "WeekId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Match_NoSelfPlay",
                table: "Matches",
                sql: "[Player1Id] <> [Player2Id]");
        }
    }
}
