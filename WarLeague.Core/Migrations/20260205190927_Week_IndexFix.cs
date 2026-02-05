using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class Week_IndexFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Weeks_SeasonId",
                table: "Weeks");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Weeks",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_SeasonId_Status",
                table: "Weeks",
                columns: new[] { "SeasonId", "Status" },
                unique: true,
                filter: "[Status] <> 'Completed' and [Status] <> 'NotOpenYet'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Weeks_SeasonId_Status",
                table: "Weeks");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Weeks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_SeasonId",
                table: "Weeks",
                column: "SeasonId",
                unique: true,
                filter: "[Status] <> 'Completed' and [Status] <> 'NotOpenYet'");
        }
    }
}
