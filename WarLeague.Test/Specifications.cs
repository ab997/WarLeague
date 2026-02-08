using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WarLeague.Data;
using WarLeague.Data.Enums;
using WarLeague.Core.Services;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;

namespace WarLeague.Test
{
    /// <summary>
    /// Domain behavior specifications using Arrange-Act-Assert pattern.
    /// Tests ONLY use services - NO direct database context access.
    /// All tests follow "WhenXThenY" naming to make behavior explicit.
    /// </summary>
    public class Specifications : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FormatService _formatService;
        private readonly SeasonService _seasonService;
        private readonly WeekService _weekService;
        private readonly TeamService _teamService;
        private readonly MatchService _matchService;
        private readonly DeckSubmissionService _deckSubmissionService;
        private readonly SubstitutionService _substitutionService;
        private readonly WarLeagueDbContext _context;

        public Specifications()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
                .Build();

            var connectionString = configuration.GetConnectionString("TestConnection")
                ?? throw new InvalidOperationException("Test connection string not found in appsettings.Test.json");

            var optionsBuilder = new DbContextOptionsBuilder<WarLeagueDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            _context = new WarLeagueDbContext(optionsBuilder.Options);
            _serviceProvider = TestServiceProvider.CreateServiceProvider(_context);

            _formatService = _serviceProvider.GetRequiredService<FormatService>();
            _seasonService = _serviceProvider.GetRequiredService<SeasonService>();
            _weekService = _serviceProvider.GetRequiredService<WeekService>();
            _teamService = _serviceProvider.GetRequiredService<TeamService>();
            _matchService = _serviceProvider.GetRequiredService<MatchService>();
            _deckSubmissionService = _serviceProvider.GetRequiredService<DeckSubmissionService>();
            _substitutionService = _serviceProvider.GetRequiredService<SubstitutionService>();

            RecreateDatabase();
        }

        private void RecreateDatabase()
        {
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region Format Behavior Specifications

        [Fact]
        public async Task WhenCreatingValidFormat_ThenReturnsSuccess()
        {
            // Arrange
            var formatName = "Speed Duel";

            // Act
            var result = await _formatService.CreateFormatAsync(formatName);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenCreatingDuplicateFormat_ThenReturnsFail()
        {
            // Arrange
            await _formatService.CreateFormatAsync("HAT");

            // Act
            var result = await _formatService.CreateFormatAsync("HAT");

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenDeletingExistingFormat_ThenReturnsSuccess()
        {
            // Arrange
            await _formatService.CreateFormatAsync("HAT");

            // Act
            var result = await _formatService.DeleteFormatAsync("HAT");

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenUpdatingFormatRules_ThenReturnsSuccess()
        {
            // Arrange
            await _formatService.CreateFormatAsync("HAT");
            var rules = "{\"banList\": [\"Pot of Greed\"]}";

            // Act
            var result = await _formatService.UpdateFormatRulesAsync("HAT", rules);

            // Assert
            result.Success.ShouldBeTrue();
        }

        #endregion

        #region Season Behavior Specifications

        [Fact]
        public async Task WhenCreatingSeasonWithValidParameters_ThenReturnsSuccess()
        {
            // Arrange
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");

            // Act
            var result = await _seasonService.CreateAsync(format!.Id, seasonNumber: 1, minTeamMembers: 4);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenCreatingDuplicateSeasonNumber_ThenReturnsFail()
        {
            // Arrange
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);

            // Act
            var result = await _seasonService.CreateAsync(format.Id, 1, 3);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenSettingSeasonActive_ThenReturnsSuccess()
        {
            // Arrange
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);

            // Act
            SeasonResult result = await _seasonService.SetActiveAsync(format.Id, seasonNumber: 1);

            // Assert
            result.Success.ShouldBeTrue();
            result.Season!.Active.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenDisablingTeamModifications_ThenReturnsSuccess()
        {
            // Arrange
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);
            var season = format.Seasons.First();

            // Act
            SeasonResult result = await _seasonService.SetTeamModificationsAsync(season.Id, enabled: false);

            // Assert
            result.Success.ShouldBeTrue();
            result.Season!.DisableTeamModification.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenEnablingTeamModifications_ThenReturnsSuccess()
        {
            // Arrange
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);
            var season = format.Seasons.First();

            // Act
            var result = await _seasonService.SetTeamModificationsAsync(season.Id, enabled: true);

            // Assert
            result.Success.ShouldBeTrue();
            result.Season!.DisableTeamModification.ShouldBeFalse();
        }

        #endregion

        #region Week Behavior Specifications

        [Fact]
        public async Task WhenCreatingWeekWithValidParameters_ThenReturnsSuccess()
        {
            // Arrange
            var (formatId, seasonId) = await CreateFormatAndSeason();
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");

            // Act
            var result = await _weekService.CreateAsync(seasonId, weekNumber: 1, startDate, endDate, null, submissionsRequired: 3);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenCreatingDuplicateWeekNumber_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 1, startDate, endDate, null, 3);

            // Act
            var result = await _weekService.CreateAsync(seasonId, 1, startDate.AddDays(7), endDate.AddDays(7), null, 3);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already exists", Case.Insensitive);
        }

        [Fact]
        public async Task WhenOpeningWeek_InStatusNotOpenYet_ThenReturnsSuccess()
        {
            // Arrange
            int weekNumber = 1;
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, weekNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);

            // Act
            var result = await _weekService.TransitionToOpenWeekAsync(seasonId, weekNumber);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenOpeningWeek_NotInStatusNotOpenYet_ThenReturnsFail()
        {
            // Arrange
            int weekNumber = 1;
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, weekNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);
            await _weekService.UpdateAsync(seasonId, weekNumber, null, null, null, WeekStatus.InProgress, 3);

            // Act
            var result = await _weekService.TransitionToOpenWeekAsync(seasonId, weekNumber);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenClosingSubmissionsWithNoOpenWeek_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();

            // Act
            var result = await _weekService.TransitionToCloseSubmissionsAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenClosingWeekWithNoInProgressWeek_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();

            // Act
            var result = await _weekService.TransitionToCompletedAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("InProgress week", Case.Insensitive);
        }

        [Fact]
        public async Task WhenDeletingExistingWeek_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);

            // Act
            var result = await _weekService.DeleteAsync(seasonId, 1);

            // Assert
            result.Success.ShouldBeTrue();
        }

        #endregion

        #region Deck Submission Behavior Specifications

        [Fact]
        public async Task WhenSubmittingFirstDeck_ThenReturnsSuccess()
        {
            // Arrange
            var (seasonId, playerId) = await CreateSeasonWithTeamAndOpenWeek();

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck content", seatNumber: 1);

            // Assert
            result.Success.ShouldBeTrue();
            result.Message.ShouldContain("submitted", Case.Insensitive);
        }

        [Fact]
        public async Task WhenResubmittingDeck_ThenUpdatesExistingSubmission()
        {
            // Arrange
            var (seasonId, playerId) = await CreateSeasonWithTeamAndOpenWeek();
            await _deckSubmissionService.SubmitAsync(seasonId, playerId, "original deck", 1);

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, playerId, "updated deck", 2);

            // Assert
            result.Success.ShouldBeTrue();
            result.Message.ShouldContain("updated", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubmittingToOccupiedSeatOnSameTeam_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, player1Id, player2Id) = await CreateSeasonWithTeamAndTwoPlayersAndOpenWeek();
            await _deckSubmissionService.SubmitAsync(seasonId, player1Id, "deck1", seatNumber: 1);

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, player2Id, "deck2", seatNumber: 1);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already taken", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubmittingToSameSeatOnDifferentTeams_ThenBothSucceed()
        {
            // Arrange
            var (formatId, seasonId) = await CreateFormatAndSeason();
            var team1PlayerId = await CreateTeamWithPlayer(seasonId, "Team1");
            var team2PlayerId = await CreateTeamWithPlayer(seasonId, "Team2");
            await CreateOpenWeek(seasonId);

            // Act
            var result1 = await _deckSubmissionService.SubmitAsync(seasonId, team1PlayerId, "deck1", 1);
            var result2 = await _deckSubmissionService.SubmitAsync(seasonId, team2PlayerId, "deck2", 1);

            // Assert
            result1.Success.ShouldBeTrue();
            result2.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenSubmittingWithInvalidSeatNumber_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, playerId) = await CreateSeasonWithTeamAndOpenWeek(submissionsRequired: 3);

            // Act
            var result0 = await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck", seatNumber: 0);
            var result4 = await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck", seatNumber: 4);

            // Assert
            result0.Success.ShouldBeFalse();
            result0.Message.ShouldContain("between", Case.Insensitive);
            result4.Success.ShouldBeFalse();
            result4.Message.ShouldContain("between", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubmittingWithNoOpenWeek_ThenReturnsFail()
        {
            // Arrange
            var (formatId, seasonId) = await CreateFormatAndSeason();
            var playerId = await CreateTeamWithPlayer(seasonId, "Team1");
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck", 1);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not open", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubmittingAsPlayerNotOnTeam_ThenReturnsFail()
        {
            // Arrange
            var (formatId, seasonId) = await CreateFormatAndSeason();
            await CreateOpenWeek(seasonId);
            var unassignedPlayer = await CreatePlayer(999999);

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, unassignedPlayer.Id, "deck", 1);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not on any team", Case.Insensitive);
        }

        [Fact]
        public async Task WhenDeletingExistingSubmission_ThenReturnsSuccess()
        {
            // Arrange
            var (seasonId, playerId) = await CreateSeasonWithTeamAndOpenWeek();
            await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck", 1);

            // Act
            var result = await _deckSubmissionService.DeleteSubmissionAsync(seasonId, playerId);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenDeletingNonExistentSubmission_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, playerId) = await CreateSeasonWithTeamAndOpenWeek();

            // Act
            var result = await _deckSubmissionService.DeleteSubmissionAsync(seasonId, playerId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("No existing deck submission", Case.Insensitive);
        }

        [Fact]
        public async Task WhenDeletingSubmission_ThenFreesSeatForOtherPlayers()
        {
            // Arrange
            var (seasonId, player1Id, player2Id) = await CreateSeasonWithTeamAndTwoPlayersAndOpenWeek();
            await _deckSubmissionService.SubmitAsync(seasonId, player1Id, "deck1", 1);
            await _deckSubmissionService.DeleteSubmissionAsync(seasonId, player1Id);

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, player2Id, "deck2", 1);

            // Assert
            result.Success.ShouldBeTrue();
        }

        #endregion

        #region Substitution Behavior Specifications

        [Fact]
        public async Task WhenSubstitutingPlayerWithValidScenario_ThenReturnsSuccess()
        {
            // Arrange
            var (seasonId, teamName, playerInId, playerOutId) = await CreateSubstitutionScenario();

            // Act
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, teamName, playerInId, playerOutId);

            // Assert
            result.Success.ShouldBeTrue();
            result.Message.ShouldContain("successful", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubstitutingWithNonExistentTeam_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, _, playerInId, playerOutId) = await CreateSubstitutionScenario();

            // Act
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "NonexistentTeam", playerInId, playerOutId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not found", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubstitutingPlayerInNotOnTeam_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, teamName, _, playerOutId) = await CreateSubstitutionScenario();
            var outsider = await CreatePlayer(888888);

            // Act
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, teamName, outsider.Id, playerOutId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not a member", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubstitutingPlayerOutNotOnTeam_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, teamName, playerInId, _) = await CreateSubstitutionScenario();
            var outsider = await CreatePlayer(999999);

            // Act
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, teamName, playerInId, outsider.Id);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not a member", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubstitutingWithNoInProgressWeek_ThenReturnsFail()
        {
            // Arrange
            var (formatId, seasonId) = await CreateFormatAndSeason();
            var (player1, player2) = await CreateTwoPlayersOnSameTeam(seasonId, "Team1");

            // Act
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team1", player2.Id, player1.Id);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("InProgress week", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubstitutingWithPlayerInAlreadyScheduled_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, teamName, player1, player2, weekId) = await CreateTwoPlayersWithMatchesScenario();

            // Act - player2 can't sub in for player1 because player2 is already playing
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, teamName, player2, player1);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already scheduled", Case.Insensitive);
        }

        [Fact]
        public async Task WhenSubstitutingWithPlayerOutNotScheduled_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, teamName, playerInId, _) = await CreateSubstitutionScenario();

            // Act
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, teamName, playerInId, playerInId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not currently scheduled", Case.Insensitive);
        }

        #endregion

        #region Match Generation Behavior Specifications

        [Fact]
        public async Task WhenGeneratingPairingsWithTwoTeams_ThenCreatesCorrectNumberOfMatches()
        {
            // Arrange
            var (seasonId, weekId) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 3);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches.ShouldNotBeNull();
            result.CreatedMatches!.Count.ShouldBe(3);
        }

        [Fact]
        public async Task WhenGeneratingPairingsWithFourTeams_ThenCreatesCorrectNumberOfMatches()
        {
            // Arrange
            var (seasonId, weekId) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 4, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches!.Count.ShouldBe(4);
        }

        [Fact]
        public async Task WhenGeneratingPairingsWithOddNumberOfTeams_ThenHandlesByeCorrectly()
        {
            // Arrange
            var (seasonId, weekId) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 3, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches!.Count.ShouldBe(2);
        }

        [Fact]
        public async Task WhenGeneratingPairingsWithOneTeam_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, weekId) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 1, playersPerTeam: 3);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("at least 2 teams", Case.Insensitive);
        }

        [Fact]
        public async Task WhenGeneratingPairingsWithNoSubmissionsClosedWeek_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, weekId) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("SubmissionsClosed", Case.Insensitive);
        }

        #endregion

        #region Helper Methods - Setup Scenarios

        private async Task<(int formatId, int seasonId)> CreateFormatAndSeason()
        {
            var formatName = $"Format{Guid.NewGuid()}";
            await _formatService.CreateFormatAsync(formatName);
            var format = await _formatService.GetFormatAsync(formatName);
            await _seasonService.CreateAsync(format!.Id, 1, 4);
            var season = format.Seasons.First();
            return (format.Id, season.Id);
        }

        private async Task<(int seasonId, int playerId)> CreateSeasonWithTeamAndOpenWeek(int submissionsRequired = 3)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var playerId = await CreateTeamWithPlayer(seasonId, "Team1");
            await CreateOpenWeek(seasonId, submissionsRequired);
            return (seasonId, playerId);
        }

        private async Task<(int seasonId, int player1Id, int player2Id)> CreateSeasonWithTeamAndTwoPlayersAndOpenWeek()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var (player1, player2) = await CreateTwoPlayersOnSameTeam(seasonId, "Team1");
            await CreateOpenWeek(seasonId);
            return (seasonId, player1.Id, player2.Id);
        }

        private async Task<(int seasonId, string teamName, int playerInId, int playerOutId)> CreateSubstitutionScenario()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var teamName = "Team1";
            var (player1, player2) = await CreateTwoPlayersOnSameTeam(seasonId, teamName);
            
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
            await _weekService.UpdateAsync(seasonId, 1, null, null, null, WeekStatus.InProgress, 2);
            
            var opponent = await CreatePlayer(777777);
            var opponentTeamId = await CreateTeam(seasonId, "OpponentTeam", opponent.Id);
            
            await CreateMatch(seasonId, 1, player1.Id, opponent.Id);
            await CreateDeckSubmission(seasonId, 1, player1.Id, 1);
            
            return (seasonId, teamName, player2.Id, player1.Id);
        }

        private async Task<(int seasonId, string teamName, int player1Id, int player2Id, int weekId)> CreateTwoPlayersWithMatchesScenario()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var teamName = "Team1";
            var (player1, player2) = await CreateTwoPlayersOnSameTeam(seasonId, teamName);
            
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
            await _weekService.UpdateAsync(seasonId, 1, null, null, null, WeekStatus.InProgress, 2);
            
            var week = await _context.Weeks.FirstAsync(w => w.SeasonId == seasonId && w.WeekNumber == 1);
            
            var opponent1 = await CreatePlayer(888881);
            var opponent2 = await CreatePlayer(888882);
            var opponentTeamId = await CreateTeam(seasonId, "OpponentTeam", opponent1.Id);
            await AddPlayerToTeam(opponent2.Id, seasonId, opponentTeamId);
            
            await CreateMatch(seasonId, 1, player1.Id, opponent1.Id);
            await CreateMatch(seasonId, 1, player2.Id, opponent2.Id);
            
            return (seasonId, teamName, player1.Id, player2.Id, week.Id);
        }

        private async Task<(int seasonId, int weekId)> CreateSeasonWithTeamsAndSubmissions(int teamCount, int playersPerTeam)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            
            for (int i = 0; i < teamCount; i++)
            {
                var captain = await CreatePlayer((ulong)(1000 + i * 100));
                var teamId = await CreateTeam(seasonId, $"Team{i + 1}", captain.Id);
                
                for (int j = 1; j <= playersPerTeam; j++)
                {
                    var player = await CreatePlayer((ulong)(1000 + i * 100 + j));
                    await AddPlayerToTeam(player.Id, seasonId, teamId);
                }
            }
            
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, playersPerTeam);
            await _weekService.UpdateAsync(seasonId, 1, null, null, null, WeekStatus.Open, playersPerTeam);
            
            var teams = await _context.Teams.Where(t => t.SeasonId == seasonId).ToListAsync();
            var week = await _context.Weeks.FirstAsync(w => w.SeasonId == seasonId && w.WeekNumber == 1);
            
            foreach (var team in teams)
            {
                var teamPlayerIds = await GetTeamPlayerIds(seasonId, team.Id);
                for (int seat = 1; seat <= playersPerTeam; seat++)
                {
                    await CreateDeckSubmission(seasonId, week.WeekNumber, teamPlayerIds[seat - 1], seat);
                }
            }
            
            return (seasonId, week.Id);
        }

        private async Task<int> CreateTeamWithPlayer(int seasonId, string teamName)
        {
            var captain = await CreatePlayer(123456);
            var teamId = await CreateTeam(seasonId, teamName, captain.Id);
            var player = await CreatePlayer(654321);
            await AddPlayerToTeam(player.Id, seasonId, teamId);
            return player.Id;
        }

        private async Task<(Player player1, Player player2)> CreateTwoPlayersOnSameTeam(int seasonId, string teamName)
        {
            var captain = await CreatePlayer(111111);
            var teamId = await CreateTeam(seasonId, teamName, captain.Id);
            var player1 = await CreatePlayer(222222);
            var player2 = await CreatePlayer(333333);
            await AddPlayerToTeam(player1.Id, seasonId, teamId);
            await AddPlayerToTeam(player2.Id, seasonId, teamId);
            return (player1, player2);
        }

        private async Task CreateOpenWeek(int seasonId, int submissionsRequired = 3)
        {
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, submissionsRequired);
            await _weekService.UpdateAsync(seasonId, 1, null, null, null, WeekStatus.Open, submissionsRequired);
        }

        private async Task CloseSubmissions(int seasonId)
        {
            await _weekService.TransitionToCloseSubmissionsAsync(seasonId);
        }

        private async Task<Player> CreatePlayer(ulong discordUserId)
        {
            var player = new Player { DiscordUserId = discordUserId, UserName = $"Player{discordUserId}" };
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return player;
        }

        private async Task<int> CreateTeam(int seasonId, string teamName, int captainId)
        {
            var team = new Team
            {
                Name = teamName,
                CaptainId = captainId,
                SeasonId = seasonId,
                DiscordRoleId = (ulong)(new Random().Next(100000, 999999))
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return team.Id;
        }

        private async Task AddPlayerToTeam(int playerId, int seasonId, int teamId)
        {
            _context.PlayerSeasonTeams.Add(new PlayerSeasonTeam
            {
                PlayerId = playerId,
                SeasonId = seasonId,
                TeamId = teamId
            });
            await _context.SaveChangesAsync();
        }

        private async Task CreateMatch(int seasonId, int weekNumber, int player1Id, int player2Id)
        {
            var week = await _context.Weeks.FirstOrDefaultAsync(w => w.SeasonId == seasonId && w.WeekNumber == weekNumber);
            if (week == null) return;
            
            _context.Matches.Add(new Match
            {
                WeekId = week.Id,
                Player1Id = player1Id,
                Player2Id = player2Id,
                Status = MatchStatus.Scheduled
            });
            await _context.SaveChangesAsync();
        }

        private async Task CreateDeckSubmission(int seasonId, int weekNumber, int playerId, int seatNumber)
        {
            var week = await _context.Weeks.FirstOrDefaultAsync(w => w.SeasonId == seasonId && w.WeekNumber == weekNumber);
            if (week == null) return;
            
            _context.DeckSubmissions.Add(new DeckSubmission
            {
                WeekId = week.Id,
                PlayerId = playerId,
                DeckFile = $"deck{playerId}",
                SeatNumber = seatNumber,
                SubmittedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        private async Task<List<int>> GetTeamPlayerIds(int seasonId, int teamId)
        {
            return await _context.PlayerSeasonTeams
                .Where(pst => pst.SeasonId == seasonId && pst.TeamId == teamId)
                .Select(pst => pst.PlayerId)
                .ToListAsync();
        }

        #endregion
    }
}
