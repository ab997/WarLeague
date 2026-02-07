//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Shouldly;
//using WarLeague.Core.Data;
//using WarLeague.Core.Data.Enums;
//using WarLeague.Core.Domain.Services;

//namespace WarLeague.Test
//{
//    /// <summary>
//    /// Domain behavior specifications - testing domain services and business rules.
//    /// Tests focus on behavior and outcomes, not implementation details.
//    /// </summary>
//    public class Specifications : IDisposable
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly FormatService _formatService;
//        private readonly SeasonService _seasonService;
//        private readonly WeekService _weekService;
//        private readonly TeamService _teamService;
//        private readonly SubstitutionService _substitutionService;
//        private readonly string _connectionString;
//        private readonly WarLeagueDbContext Context;

//        public Specifications()
//        {
//            var configuration = new ConfigurationBuilder()
//            .SetBasePath(Directory.GetCurrentDirectory())
//            .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
//            .Build();

//            _connectionString = configuration.GetConnectionString("TestConnection")
//                ?? throw new InvalidOperationException("Test connection string not found in appsettings.Test.json");

//            var optionsBuilder = new DbContextOptionsBuilder<WarLeagueDbContext>();
//            optionsBuilder.UseSqlServer(_connectionString);

//            Context = new WarLeagueDbContext(optionsBuilder.Options);

//            _serviceProvider = TestServiceProvider.CreateServiceProvider(Context);
            
//            _formatService = _serviceProvider.GetRequiredService<FormatService>();
//            _seasonService = _serviceProvider.GetRequiredService<SeasonService>();
//            _weekService = _serviceProvider.GetRequiredService<WeekService>();
//            _teamService = _serviceProvider.GetRequiredService<TeamService>();
//            _substitutionService = _serviceProvider.GetRequiredService<SubstitutionService>();

//            RecreateDatabase(Context);

//        }
//        public void Dispose()
//        {
//            Context?.Dispose();
//        }

//        private void RecreateDatabase(WarLeagueDbContext context)
//        {
//            context.Database.EnsureDeleted();
//            context.Database.EnsureCreated();
//        }

//        [Fact]
//        public async Task Format_CanCreateANewFormat()
//        {
//            var formatName = "Speed Duel";

//            var createdFormat = await _formatService.CreateFormatAsync(formatName);

//            createdFormat.ShouldNotBeNull();
//            createdFormat.Name.ShouldBe(formatName);
//        }

//        [Fact]
//        public async Task Format_CannotCreateTwoFormatsWithTheSameName()
//        {
//            var result1 = await _formatService.CreateFormatAsync("HAT");
//            var result2 = await _formatService.CreateFormatAsync("HAT");

//            result2.ShouldBeNull();
//        }

//        [Fact]
//        public async Task Format_NewlyCreatedFormatsShouldNotBeSingleFormatMode()
//        {
//            var result = await _formatService.CreateFormatAsync("Traditional");

//            result!.SingleFormatMode.ShouldBeFalse();
//        }

//        [Fact]
//        public async Task Format_CanDeleteAnExistingFormat()
//        {
//            _ = await _formatService.CreateFormatAsync("HAT");

//            var deletedFormat = await _formatService.DeleteFormatAsync("HAT");

//            deletedFormat.ShouldNotBeNull();
            
//            // Verify it's gone by trying to get it
//            var checkFormat = await _formatService.GetFormatAsync("HAT");
//            checkFormat.ShouldBeNull();
//        }

//        [Fact]
//        public async Task Format_CanUpdateRulesForAnExistingFormat()
//        {
//            _ = await _formatService.CreateFormatAsync("HAT");
//            var updatedRules = "{\"banList\": [\"Pot of Greed\", \"Monster Reborn\"]}";

//            var updatedFormat = await _formatService.UpdateFormatRulesAsync("HAT", updatedRules);

//            updatedFormat.ShouldNotBeNull();
//            updatedFormat.Rules.ShouldBe(updatedRules);
//        }

//        #region Season Specifications

//        [Fact]
//        public async Task Season_CanCreateANewSeasonForAFormat()
//        {
//            var format = await _formatService.CreateFormatAsync("HAT");
//            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

//            season.ShouldNotBeNull();
//            season.SeasonNumber.ShouldBe(1);
//            season.MinimumTeamMembers.ShouldBe(4);
//        }

//        [Fact]
//        public async Task Season_CannotCreateTwoSeasonsWithTheSameNumberInTheSameFormat()
//        {
//            var format = await _formatService.CreateFormatAsync("HAT");
//            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

//            var result1 = await _seasonService.CreateAsync(format!.Id, 1, 3);
//            var result2 = await _seasonService.CreateAsync(format!.Id, 1, 3);

//            result2.ShouldBeNull();
//        }

//        [Fact]
//        public async Task Season_NewlyCreatedSeasonsShouldBeInactive()
//        {
//            var format = await _formatService.CreateFormatAsync("HAT");
//            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

//            season!.Active.ShouldBeFalse();
//        }

//        [Fact]
//        public async Task Season_NewlyCreatedSeasonsShouldAllowTeamModificationsByDefault()
//        {
//            var format = await _formatService.CreateFormatAsync("HAT");
//            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

//            season!.DisableTeamModification.ShouldBeFalse();
//        }

//        [Fact]
//        public async Task Season_WhenTeamsArePresent_CanNotDeleteSeason()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");

//            // Should throw exception because there are teams present in the season
//            await Should.ThrowAsync<Exception>(async () =>
//            {
//                await _seasonService.DeleteAsync(format!.Id, 1);
//            });
//        }

//        [Fact]
//        public async Task Season_CanSetASeasonAsActive()
//        {
//            var format = await _formatService.CreateFormatAsync("HAT");
//            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

//            var result = await _seasonService.SetActiveAsync(format.Id, season!.SeasonNumber);

//            result.ShouldNotBeNull();
//            result.Active.ShouldBeTrue();
//        }


//        [Fact]
//        public async Task Season_CanDisableTeamModificationsForASeason()
//        {
//            var format = await _formatService.CreateFormatAsync("HAT");
//            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

//            var updatedSeason = await _seasonService.SetTeamModificationsAsync(season!.Id, enabled: false);

//            updatedSeason.ShouldNotBeNull();
//            updatedSeason.DisableTeamModification.ShouldBeTrue();
//        }

//        [Fact]
//        public async Task Season_CanEnableTeamModificationsForASeason()
//        {
//            var format = await _formatService.CreateFormatAsync("HAT");
//            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

//            var updatedSeason = await _seasonService.SetTeamModificationsAsync(season!.Id, enabled: true);

//            updatedSeason.ShouldNotBeNull();
//            updatedSeason.DisableTeamModification.ShouldBeFalse();
//        }

//        #endregion







//        #region Week Specifications

//        [Fact]
//        public async Task Week_CanCreateANewWeek()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");
//            var subCloseDate = DateTime.Parse("2025-01-05");

//            var week = await _weekService.CreateAsync(seasonId, 5, startDate, endDate, subCloseDate, 2);

//            week.ShouldNotBeNull();
//            week.WeekNumber.ShouldBe(5);
//            week.StartDate.ShouldBe(startDate);
//            week.EndDate.ShouldBe(endDate);
//            week.SubmissionsClosedDate.ShouldBe(subCloseDate);
//        }

//        [Fact]
//        public async Task Week_CannotCreateTwoWeeksWithSameNumberInSameSeason()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");
//            await _weekService.CreateAsync(seasonId, 1, startDate, endDate, null, 2);

//            var result = await _weekService.CreateAsync(seasonId, 1, startDate, endDate, null, 2);

//            result.ShouldBeNull();
//        }

//        [Fact]
//        public async Task Week_NewlyCreatedWeeksShouldHaveNotOpenYetStatus()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");

//            var week = await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);

//            week!.Status.ShouldBe(WeekStatus.NotOpenYet);
//        }

//        [Fact]
//        public async Task Week_CanDeleteAWeek()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");
//            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);

//            var deletedWeek = await _weekService.DeleteAsync(seasonId, 5);

//            deletedWeek.ShouldNotBeNull();
//            deletedWeek.WeekNumber.ShouldBe(5);
//        }

//        [Fact]
//        public async Task Week_CanUpdateWeekDates()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");
//            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
//            var newStartDate = DateTime.Parse("2025-02-01");
//            var newEndDate = DateTime.Parse("2025-02-07");

//            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, newStartDate, newEndDate, null, null, 2);

//            updatedWeek.ShouldNotBeNull();
//            updatedWeek.StartDate.ShouldBe(newStartDate);
//            updatedWeek.EndDate.ShouldBe(newEndDate);
//        }

//        [Fact]
//        public async Task Week_CanUpdateWeekStatus()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");
//            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);

//            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Open, 2);

//            updatedWeek.ShouldNotBeNull();
//            updatedWeek.Status.ShouldBe(WeekStatus.Open);
//        }

//        [Fact]
//        public async Task Week_CanTransitionFromNotOpenYetToOpen()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");
//            var week = await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
//            week!.Status.ShouldBe(WeekStatus.NotOpenYet);

//            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Open, 2);

//            updatedWeek.ShouldNotBeNull();
//            updatedWeek.Status.ShouldBe(WeekStatus.Open);
//        }

//        [Fact]
//        public async Task Week_CanTransitionFromOpenToSubmissionsClosed()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");
//            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
//            await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Open, 2);

//            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.SubmissionsClosed, 2);

//            updatedWeek.ShouldNotBeNull();
//            updatedWeek.Status.ShouldBe(WeekStatus.SubmissionsClosed);
//        }

//        [Fact]
//        public async Task Week_CanTransitionFromSubmissionsClosedToInProgress()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");
//            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
//            await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.SubmissionsClosed, 2);

//            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.InProgress, 2);

//            updatedWeek.ShouldNotBeNull();
//            updatedWeek.Status.ShouldBe(WeekStatus.InProgress);
//        }

//        [Fact]
//        public async Task Week_CanTransitionFromInProgressToCompleted()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var startDate = DateTime.Parse("2025-01-01");
//            var endDate = DateTime.Parse("2025-01-07");
//            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
//            await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.InProgress, 2);

//            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Completed, 2);

//            updatedWeek.ShouldNotBeNull();
//            updatedWeek.Status.ShouldBe(WeekStatus.Completed);
//        }

//        [Fact]
//        public async Task Week_CloseSubmissionsReturnsErrorWhenNoOpenWeekExists()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;

//            var result = await _weekService.CloseSubmissionsAsync(seasonId);

//            result.Success.ShouldBeFalse();
//            result.Message.ShouldContain("No open week");
//        }

//        [Fact]
//        public async Task Week_CloseWeekReturnsErrorWhenNoInProgressWeekExists()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;

//            var result = await _weekService.CloseAsync(seasonId);

//            result.Success.ShouldBeFalse();
//            result.Message.ShouldContain("No InProgress week");
//        }

//        #endregion

//        #region Substitution Specifications

//        [Fact]
//        public async Task Substitution_CanSubstitutePlayerSuccessfully()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;

//            // Create week 2 with InProgress status
//            var week2 = await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
//            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.InProgress, 2);

//            // Create a scheduled match and deck submission for Player1
//            var player1 = Context.Players.First(p => p.UserName == "Player1");
//            var player2 = Context.Players.First(p => p.UserName == "Player2");
//            var player5 = Context.Players.First(p => p.UserName == "Player5");

//            var match = new Core.Data.Entities.Match
//            {
//                WeekId = week2!.Id,
//                Player1Id = player1.Id,
//                Player2Id = player5.Id,
//                Status = Core.Data.Enums.MatchStatus.Scheduled
//            };
//            Context.Matches.Add(match);

//            var deckSubmission = new Core.Data.Entities.DeckSubmission
//            {
//                WeekId = week2.Id,
//                PlayerId = player1.Id,
//                DeckFile = "test.ydk",
//                SeatNumber = 1
//            };
//            Context.DeckSubmissions.Add(deckSubmission);
//            await Context.SaveChangesAsync();

//            // Substitute Player2 in for Player1
//            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player2.Id, player1.Id);

//            result.Success.ShouldBeTrue();
//            result.Message.ShouldContain("Substitution successful");

//            // Verify match was updated
//            var updatedMatch = await Context.Matches.FindAsync(match.Id);
//            updatedMatch!.Player1Id.ShouldBe(player2.Id);

//            // Verify deck submission was updated
//            var updatedDeck = await Context.DeckSubmissions.FindAsync(deckSubmission.Id);
//            updatedDeck!.PlayerId.ShouldBe(player2.Id);
//        }

//        [Fact]
//        public async Task Substitution_ReturnsErrorWhenTeamNotFound()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var player1 = Context.Players.First(p => p.UserName == "Player1");
//            var player2 = Context.Players.First(p => p.UserName == "Player2");

//            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Nonexistent Team", player2.Id, player1.Id);

//            result.Success.ShouldBeFalse();
//            result.Message.ShouldContain("not found");
//        }

//        [Fact]
//        public async Task Substitution_ReturnsErrorWhenPlayerInNotOnTeam()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var player1 = Context.Players.First(p => p.UserName == "Player1");
//            var player5 = Context.Players.First(p => p.UserName == "Player5"); // On Team Beta

//            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player5.Id, player1.Id);

//            result.Success.ShouldBeFalse();
//            result.Message.ShouldContain("not a member");
//        }

//        [Fact]
//        public async Task Substitution_ReturnsErrorWhenPlayerOutNotOnTeam()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var player1 = Context.Players.First(p => p.UserName == "Player1");
//            var player5 = Context.Players.First(p => p.UserName == "Player5"); // On Team Beta

//            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player1.Id, player5.Id);

//            result.Success.ShouldBeFalse();
//            result.Message.ShouldContain("not a member");
//        }

//        [Fact]
//        public async Task Substitution_ReturnsErrorWhenPlayerInAlreadyPlaying()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;

//            // Create week 2 with InProgress status
//            var week2 = await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
//            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.InProgress, 2);

//            var player1 = Context.Players.First(p => p.UserName == "Player1");
//            var player2 = Context.Players.First(p => p.UserName == "Player2");
//            var player5 = Context.Players.First(p => p.UserName == "Player5");
//            var player6 = Context.Players.First(p => p.UserName == "Player6");

//            // Player1 is already scheduled to play
//            var match1 = new Core.Data.Entities.Match
//            {
//                WeekId = week2!.Id,
//                Player1Id = player1.Id,
//                Player2Id = player5.Id,
//                Status = Core.Data.Enums.MatchStatus.Scheduled
//            };

//            // Player2 is also scheduled to play
//            var match2 = new Core.Data.Entities.Match
//            {
//                WeekId = week2.Id,
//                Player1Id = player2.Id,
//                Player2Id = player6.Id,
//                Status = Core.Data.Enums.MatchStatus.Scheduled
//            };

//            Context.Matches.AddRange(match1, match2);
//            await Context.SaveChangesAsync();

//            // Try to substitute Player2 (who's already playing) in for Player1
//            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player2.Id, player1.Id);

//            result.Success.ShouldBeFalse();
//            result.Message.ShouldContain("already scheduled");
//        }

//        [Fact]
//        public async Task Substitution_ReturnsErrorWhenPlayerOutNotScheduledToPlay()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;

//            // Create week 2 with InProgress status
//            var week2 = await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
//            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.InProgress, 2);

//            var player1 = Context.Players.First(p => p.UserName == "Player1");
//            var player2 = Context.Players.First(p => p.UserName == "Player2");

//            // No matches created, so Player1 is not scheduled to play
//            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player2.Id, player1.Id);

//            result.Success.ShouldBeFalse();
//            result.Message.ShouldContain("not currently scheduled");
//        }

//        [Fact]
//        public async Task Substitution_ReturnsErrorWhenNoInProgressWeekExists()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;
//            var player1 = Context.Players.First(p => p.UserName == "Player1");
//            var player2 = Context.Players.First(p => p.UserName == "Player2");

//            // Week 1 exists but is Completed, no InProgress week
//            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player2.Id, player1.Id);

//            result.Success.ShouldBeFalse();
//            result.Message.ShouldContain("No InProgress week");
//        }

//        [Fact]
//        public async Task Substitution_ReturnsErrorWhenNoDeckSubmissionToUpdate()
//        {
//            Seed.SeedData(Context);
//            var format = await _formatService.GetFormatAsync("HAT");
//            var seasonId = format!.Seasons.First().Id;

//            // Create week 2 with InProgress status
//            var week2 = await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
//            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.InProgress, 2);

//            var player1 = Context.Players.First(p => p.UserName == "Player1");
//            var player2 = Context.Players.First(p => p.UserName == "Player2");
//            var player5 = Context.Players.First(p => p.UserName == "Player5");

//            // Create a scheduled match but NO deck submission
//            var match = new Core.Data.Entities.Match
//            {
//                WeekId = week2!.Id,
//                Player1Id = player1.Id,
//                Player2Id = player5.Id,
//                Status = Core.Data.Enums.MatchStatus.Scheduled
//            };
//            Context.Matches.Add(match);
//            await Context.SaveChangesAsync();

//            // Substitute Player2 in for Player1
//            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player2.Id, player1.Id);

//            result.Success.ShouldBeFalse();
//            result.Message.ShouldContain("no deck submission found");
//        }

//        #endregion
//    }
//}
