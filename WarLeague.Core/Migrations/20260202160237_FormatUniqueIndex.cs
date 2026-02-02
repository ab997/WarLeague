using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class FormatUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SingleFormatMode",
                table: "Formats",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Formats_SingleFormatMode",
                table: "Formats",
                column: "SingleFormatMode",
                unique: true,
                filter: "[SingleFormatMode] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Formats_SingleFormatMode",
                table: "Formats");

            migrationBuilder.DropColumn(
                name: "SingleFormatMode",
                table: "Formats");
        }
    }
}
