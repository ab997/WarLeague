using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class DisableCascadeDelete : Migration
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
                name: "FK_Matches_Weeks_WeekId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Seasons_Formats_FormatId",
                table: "Seasons");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_CaptainId",
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
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeckSubmissions_Weeks_WeekId",
                table: "DeckSubmissions",
                column: "WeekId",
                principalTable: "Weeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Weeks_WeekId",
                table: "Matches",
                column: "WeekId",
                principalTable: "Weeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Seasons_Formats_FormatId",
                table: "Seasons",
                column: "FormatId",
                principalTable: "Formats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Players_CaptainId",
                table: "Teams",
                column: "CaptainId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Weeks_Seasons_SeasonId",
                table: "Weeks",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
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
                name: "FK_Matches_Weeks_WeekId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Seasons_Formats_FormatId",
                table: "Seasons");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Players_CaptainId",
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
                name: "FK_Matches_Weeks_WeekId",
                table: "Matches",
                column: "WeekId",
                principalTable: "Weeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

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
                name: "FK_Weeks_Seasons_SeasonId",
                table: "Weeks",
                column: "SeasonId",
                principalTable: "Seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
