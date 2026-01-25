using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Core.Migrations
{
    /// <inheritdoc />
    public partial class EnableModsOnOff1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DisableTeamModification",
                table: "Seasons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisableTeamModification",
                table: "Seasons");
        }
    }
}
