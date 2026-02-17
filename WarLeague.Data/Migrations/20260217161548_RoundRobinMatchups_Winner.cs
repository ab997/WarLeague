using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Data.Migrations
{
    /// <inheritdoc />
    public partial class RoundRobinMatchups_Winner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeamWinnerId",
                table: "RoundRobinMatchups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_TeamWinnerId",
                table: "RoundRobinMatchups",
                column: "TeamWinnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoundRobinMatchups_Teams_TeamWinnerId",
                table: "RoundRobinMatchups",
                column: "TeamWinnerId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoundRobinMatchups_Teams_TeamWinnerId",
                table: "RoundRobinMatchups");

            migrationBuilder.DropIndex(
                name: "IX_RoundRobinMatchups_TeamWinnerId",
                table: "RoundRobinMatchups");

            migrationBuilder.DropColumn(
                name: "TeamWinnerId",
                table: "RoundRobinMatchups");
        }
    }
}
