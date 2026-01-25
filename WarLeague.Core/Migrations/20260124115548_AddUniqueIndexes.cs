using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Weeks_SeasonId",
                table: "Weeks");

            migrationBuilder.DropIndex(
                name: "IX_Seasons_FormatId",
                table: "Seasons");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Formats",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_SeasonId_WeekNumber",
                table: "Weeks",
                columns: new[] { "SeasonId", "WeekNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                table: "Teams",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_FormatId_SeasonNumber",
                table: "Seasons",
                columns: new[] { "FormatId", "SeasonNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_DiscordUserId",
                table: "Players",
                column: "DiscordUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Formats_Name",
                table: "Formats",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Weeks_SeasonId_WeekNumber",
                table: "Weeks");

            migrationBuilder.DropIndex(
                name: "IX_Teams_Name",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Seasons_FormatId_SeasonNumber",
                table: "Seasons");

            migrationBuilder.DropIndex(
                name: "IX_Players_DiscordUserId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Formats_Name",
                table: "Formats");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Formats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_SeasonId",
                table: "Weeks",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_FormatId",
                table: "Seasons",
                column: "FormatId");
        }
    }
}
