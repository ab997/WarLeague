using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WarLeague.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    YgoproId = table.Column<string>(type: "text", nullable: false),
                    FirstReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Utf8Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Formats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Rules = table.Column<string>(type: "text", nullable: false),
                    SingleFormatMode = table.Column<bool>(type: "boolean", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LastLegalReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissionMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PermissionType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissionMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BanlistEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FormatId = table.Column<int>(type: "integer", nullable: false),
                    CardId = table.Column<int>(type: "integer", nullable: false),
                    BanlistEntryCategory = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanlistEntries_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BanlistEntries_Formats_FormatId",
                        column: x => x.FormatId,
                        principalTable: "Formats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonNumber = table.Column<int>(type: "integer", nullable: false),
                    FormatId = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    DisableTeamModification = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumTeamMembers = table.Column<int>(type: "integer", nullable: false),
                    Phase = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seasons_Formats_FormatId",
                        column: x => x.FormatId,
                        principalTable: "Formats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PlayoffTeamsCount = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Weeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmissionsClosedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmissionsRequired = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Weeks_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CaptainId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    ConferenceId = table.Column<int>(type: "integer", nullable: false),
                    DiscordRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Conferences_ConferenceId",
                        column: x => x.ConferenceId,
                        principalTable: "Conferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_Players_CaptainId",
                        column: x => x.CaptainId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeckSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeekId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    DeckFile = table.Column<string>(type: "text", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SeatNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeckSubmissions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeckSubmissions_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeekId = table.Column<int>(type: "integer", nullable: false),
                    Player1Id = table.Column<int>(type: "integer", nullable: false),
                    Player2Id = table.Column<int>(type: "integer", nullable: false),
                    WinnerId = table.Column<int>(type: "integer", nullable: true),
                    Team1Id = table.Column<int>(type: "integer", nullable: false),
                    Team2Id = table.Column<int>(type: "integer", nullable: false),
                    WinnerTeamId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReportedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplayUrl = table.Column<string>(type: "text", nullable: true),
                    MatchResultType = table.Column<string>(type: "text", nullable: true),
                    Player1Wins = table.Column<int>(type: "integer", nullable: true),
                    Player2Wins = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.CheckConstraint("CK_Match_CanonicalOrder", "[Player1Id] < [Player2Id]");
                    table.ForeignKey(
                        name: "FK_Matches_Players_Player1Id",
                        column: x => x.Player1Id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Players_Player2Id",
                        column: x => x.Player2Id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Players_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_WinnerTeamId",
                        column: x => x.WinnerTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSeasonTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSeasonTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonTeams_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonTeams_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayoffMatchups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeekId = table.Column<int>(type: "integer", nullable: false),
                    Team1Id = table.Column<int>(type: "integer", nullable: false),
                    Team2Id = table.Column<int>(type: "integer", nullable: false),
                    MatchupType = table.Column<string>(type: "text", nullable: false),
                    TeamWinnerId = table.Column<int>(type: "integer", nullable: true),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    BracketPosition = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoffMatchups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoffMatchups_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayoffMatchups_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayoffMatchups_Teams_TeamWinnerId",
                        column: x => x.TeamWinnerId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayoffMatchups_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoundRobinMatchups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeekId = table.Column<int>(type: "integer", nullable: false),
                    Team1Id = table.Column<int>(type: "integer", nullable: false),
                    Team2Id = table.Column<int>(type: "integer", nullable: false),
                    MatchupType = table.Column<string>(type: "text", nullable: false),
                    TeamWinnerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundRobinMatchups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoundRobinMatchups_Teams_Team1Id",
                        column: x => x.Team1Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundRobinMatchups_Teams_Team2Id",
                        column: x => x.Team2Id,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundRobinMatchups_Teams_TeamWinnerId",
                        column: x => x.TeamWinnerId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundRobinMatchups_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamStandings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Tiebreaker = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamStandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamStandings_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamStandings_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanlistEntries_CardId",
                table: "BanlistEntries",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_BanlistEntries_FormatId_CardId",
                table: "BanlistEntries",
                columns: new[] { "FormatId", "CardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_YgoproId",
                table: "Cards",
                column: "YgoproId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conferences_SeasonId_Name",
                table: "Conferences",
                columns: new[] { "SeasonId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeckSubmissions_PlayerId_WeekId",
                table: "DeckSubmissions",
                columns: new[] { "PlayerId", "WeekId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeckSubmissions_WeekId",
                table: "DeckSubmissions",
                column: "WeekId");

            migrationBuilder.CreateIndex(
                name: "IX_Formats_GuildId_Name",
                table: "Formats",
                columns: new[] { "GuildId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Formats_GuildId_SingleFormatMode",
                table: "Formats",
                columns: new[] { "GuildId", "SingleFormatMode" },
                unique: true,
                filter: "[SingleFormatMode] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Player1Id",
                table: "Matches",
                column: "Player1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Player2Id",
                table: "Matches",
                column: "Player2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Team1Id",
                table: "Matches",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Team2Id",
                table: "Matches",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WeekId_Player1Id_Player2Id",
                table: "Matches",
                columns: new[] { "WeekId", "Player1Id", "Player2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerId",
                table: "Matches",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerTeamId",
                table: "Matches",
                column: "WinnerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_DiscordUserId",
                table: "Players",
                column: "DiscordUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonTeams_PlayerId_SeasonId",
                table: "PlayerSeasonTeams",
                columns: new[] { "PlayerId", "SeasonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonTeams_SeasonId",
                table: "PlayerSeasonTeams",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonTeams_TeamId",
                table: "PlayerSeasonTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_Team1Id",
                table: "PlayoffMatchups",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_Team2Id",
                table: "PlayoffMatchups",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_TeamWinnerId",
                table: "PlayoffMatchups",
                column: "TeamWinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_WeekId_BracketPosition",
                table: "PlayoffMatchups",
                columns: new[] { "WeekId", "BracketPosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoffMatchups_WeekId_Team1Id_Team2Id",
                table: "PlayoffMatchups",
                columns: new[] { "WeekId", "Team1Id", "Team2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissionMappings_GuildId_PermissionType",
                table: "RolePermissionMappings",
                columns: new[] { "GuildId", "PermissionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_Team1Id",
                table: "RoundRobinMatchups",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_Team2Id",
                table: "RoundRobinMatchups",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_TeamWinnerId",
                table: "RoundRobinMatchups",
                column: "TeamWinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundRobinMatchups_WeekId_Team1Id_Team2Id",
                table: "RoundRobinMatchups",
                columns: new[] { "WeekId", "Team1Id", "Team2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_FormatId_Active",
                table: "Seasons",
                columns: new[] { "FormatId", "Active" },
                unique: true,
                filter: "[Active] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_FormatId_SeasonNumber",
                table: "Seasons",
                columns: new[] { "FormatId", "SeasonNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CaptainId",
                table: "Teams",
                column: "CaptainId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ConferenceId",
                table: "Teams",
                column: "ConferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SeasonId_CaptainId",
                table: "Teams",
                columns: new[] { "SeasonId", "CaptainId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SeasonId_DiscordRoleId",
                table: "Teams",
                columns: new[] { "SeasonId", "DiscordRoleId" },
                unique: true,
                filter: "[DiscordRoleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SeasonId_Name",
                table: "Teams",
                columns: new[] { "SeasonId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamStandings_SeasonId_TeamId",
                table: "TeamStandings",
                columns: new[] { "SeasonId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamStandings_TeamId",
                table: "TeamStandings",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_SeasonId_Status",
                table: "Weeks",
                columns: new[] { "SeasonId", "Status" },
                unique: true,
                filter: "[Status] <> 'Completed' and [Status] <> 'NotOpenYet'");

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_SeasonId_WeekNumber",
                table: "Weeks",
                columns: new[] { "SeasonId", "WeekNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanlistEntries");

            migrationBuilder.DropTable(
                name: "DeckSubmissions");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "PlayerSeasonTeams");

            migrationBuilder.DropTable(
                name: "PlayoffMatchups");

            migrationBuilder.DropTable(
                name: "RolePermissionMappings");

            migrationBuilder.DropTable(
                name: "RoundRobinMatchups");

            migrationBuilder.DropTable(
                name: "TeamStandings");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "Weeks");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Conferences");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "Formats");
        }
    }
}
