using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class DeleteBehDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeckSubmissions_Players_PlayerId",
                table: "DeckSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DeckSubmissions_Weeks_WeekId",
                table: "DeckSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Players_Player1Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Players_Player2Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Weeks_WeekId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSeasonTeams_Players_PlayerId",
                table: "PlayerSeasonTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSeasonTeams_Seasons_SeasonId",
                table: "PlayerSeasonTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSeasonTeams_Teams_TeamId",
                table: "PlayerSeasonTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_Seasons_Formats_FormatId",
                table: "Seasons");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Seasons_SeasonId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Weeks_Seasons_SeasonId",
                table: "Weeks");

            migrationBuilder.AddForeignKey(
                name: "FK_DeckSubmissions_Players_PlayerId",
                table: "DeckSubmissions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeckSubmissions_Weeks_WeekId",
                table: "DeckSubmissions",
                column: "WeekId",
                principalTable: "Weeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Players_Player1Id",
                table: "Matches",
                column: "Player1Id",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Players_Player2Id",
                table: "Matches",
                column: "Player2Id",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Weeks_WeekId",
                table: "Matches",
                column: "WeekId",
                principalTable: "Weeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSeasonTeams_Players_PlayerId",
                table: "PlayerSeasonTeams",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSeasonTeams_Seasons_SeasonId",
                table: "PlayerSeasonTeams",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSeasonTeams_Teams_TeamId",
                table: "PlayerSeasonTeams",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Seasons_Formats_FormatId",
                table: "Seasons",
                column: "FormatId",
                principalTable: "Formats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams",
                column: "CaptainId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Seasons_SeasonId",
                table: "Teams",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Weeks_Seasons_SeasonId",
                table: "Weeks",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeckSubmissions_Players_PlayerId",
                table: "DeckSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DeckSubmissions_Weeks_WeekId",
                table: "DeckSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Players_Player1Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Players_Player2Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Weeks_WeekId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSeasonTeams_Players_PlayerId",
                table: "PlayerSeasonTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSeasonTeams_Seasons_SeasonId",
                table: "PlayerSeasonTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSeasonTeams_Teams_TeamId",
                table: "PlayerSeasonTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_Seasons_Formats_FormatId",
                table: "Seasons");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Seasons_SeasonId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Weeks_Seasons_SeasonId",
                table: "Weeks");

            migrationBuilder.AddForeignKey(
                name: "FK_DeckSubmissions_Players_PlayerId",
                table: "DeckSubmissions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeckSubmissions_Weeks_WeekId",
                table: "DeckSubmissions",
                column: "WeekId",
                principalTable: "Weeks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Players_Player1Id",
                table: "Matches",
                column: "Player1Id",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Players_Player2Id",
                table: "Matches",
                column: "Player2Id",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Weeks_WeekId",
                table: "Matches",
                column: "WeekId",
                principalTable: "Weeks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSeasonTeams_Players_PlayerId",
                table: "PlayerSeasonTeams",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSeasonTeams_Seasons_SeasonId",
                table: "PlayerSeasonTeams",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSeasonTeams_Teams_TeamId",
                table: "PlayerSeasonTeams",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Seasons_Formats_FormatId",
                table: "Seasons",
                column: "FormatId",
                principalTable: "Formats",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams",
                column: "CaptainId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Seasons_SeasonId",
                table: "Teams",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Weeks_Seasons_SeasonId",
                table: "Weeks",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id");
        }
    }
}
