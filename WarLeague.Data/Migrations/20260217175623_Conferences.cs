using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarLeague.Data.Migrations
{
    /// <inheritdoc />
    public partial class Conferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConferenceId",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Conferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeasonId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conferences_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ConferenceId",
                table: "Teams",
                column: "ConferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Conferences_SeasonId_Name",
                table: "Conferences",
                columns: new[] { "SeasonId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Conferences_ConferenceId",
                table: "Teams",
                column: "ConferenceId",
                principalTable: "Conferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Conferences_ConferenceId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "Conferences");

            migrationBuilder.DropIndex(
                name: "IX_Teams_ConferenceId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ConferenceId",
                table: "Teams");
        }
    }
}
