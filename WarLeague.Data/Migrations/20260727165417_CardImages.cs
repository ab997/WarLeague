using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Data.Migrations
{
    /// <inheritdoc />
    public partial class CardImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "Cards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Cards",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Cards");
        }
    }
}
