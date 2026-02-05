using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class Deck_AddSeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsValidated",
                table: "DeckSubmissions");

            migrationBuilder.AddColumn<int>(
                name: "SeatNumber",
                table: "DeckSubmissions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeatNumber",
                table: "DeckSubmissions");

            migrationBuilder.AddColumn<bool>(
                name: "IsValidated",
                table: "DeckSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
