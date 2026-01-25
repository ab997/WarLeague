using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class DeleteActiveConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seasons_Active",
                table: "Seasons");

            migrationBuilder.DropIndex(
                name: "IX_Formats_Active",
                table: "Formats");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "Formats");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Formats",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
    }
}
