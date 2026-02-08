using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Data.Migrations
{
    /// <inheritdoc />
    public partial class MissingCOnstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeckSubmissions_PlayerId",
                table: "DeckSubmissions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Match_NoSelfPlay",
                table: "Matches",
                sql: "[Player1Id] <> [Player2Id]");

            migrationBuilder.CreateIndex(
                name: "IX_DeckSubmissions_PlayerId_WeekId",
                table: "DeckSubmissions",
                columns: new[] { "PlayerId", "WeekId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Match_NoSelfPlay",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_DeckSubmissions_PlayerId_WeekId",
                table: "DeckSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_DeckSubmissions_PlayerId",
                table: "DeckSubmissions",
                column: "PlayerId");
        }
    }
}
