using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class EnsureSingleActiveEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Weeks_Active",
                table: "Weeks",
                column: "Active",
                unique: true,
                filter: "[Active] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_Active",
                table: "Seasons",
                column: "Active",
                unique: true,
                filter: "[Active] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Formats_Active",
                table: "Formats",
                column: "Active",
                unique: true,
                filter: "[Active] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Weeks_Active",
                table: "Weeks");

            migrationBuilder.DropIndex(
                name: "IX_Seasons_Active",
                table: "Seasons");

            migrationBuilder.DropIndex(
                name: "IX_Formats_Active",
                table: "Formats");
        }
    }
}
