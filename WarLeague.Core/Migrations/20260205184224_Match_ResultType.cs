using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class Match_ResultType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatchResultType",
                table: "Matches",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchResultType",
                table: "Matches");
        }
    }
}
