using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class Season_AddMinTeamMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinimumTeamMembers",
                table: "Seasons",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumTeamMembers",
                table: "Seasons");
        }
    }
}
