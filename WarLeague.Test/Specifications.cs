using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WarLeague.Data;
using WarLeague.Data.Enums;
using WarLeague.Core.Services;
using WarLeague.Data.Entities;

namespace WarLeague.Test
{
    /// <summary>
    /// Domain behavior specifications - testing domain services and business rules.
    /// Tests focus on behavior and outcomes, not implementation details.
    /// </summary>
    public class Specifications
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FormatService _formatService;
        private readonly SeasonService _seasonService;
        private readonly WeekService _weekService;
        private readonly TeamService _teamService;
        private readonly MatchService _matchService;
        private readonly DeckSubmissionService _deckSubmissionService;
        private readonly SubstitutionService _substitutionService;
        private readonly string _connectionString;

        public Specifications()
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
            .Build();

            _connectionString = configuration.GetConnectionString("TestConnection")
                ?? throw new InvalidOperationException("Test connection string not found in appsettings.Test.json");

            var optionsBuilder = new DbContextOptionsBuilder<WarLeagueDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);

            var Context = new WarLeagueDbContext(optionsBuilder.Options);

            _serviceProvider = TestServiceProvider.CreateServiceProvider(Context);

            _formatService = _serviceProvider.GetRequiredService<FormatService>();
            _seasonService = _serviceProvider.GetRequiredService<SeasonService>();
            _weekService = _serviceProvider.GetRequiredService<WeekService>();
            _teamService = _serviceProvider.GetRequiredService<TeamService>();
            _matchService = _serviceProvider.GetRequiredService<MatchService>();
            _deckSubmissionService = _serviceProvider.GetRequiredService<DeckSubmissionService>();
            _substitutionService = _serviceProvider.GetRequiredService<SubstitutionService>();

            RecreateDatabase(Context);
        }

        private void RecreateDatabase(WarLeagueDbContext context)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        [Fact]
        public async Task Format_CanCreateANewFormat()
        {
            var formatName = "Speed Duel";

            var result = await _formatService.CreateFormatAsync(formatName);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            var createdFormat = await _formatService.GetFormatAsync(formatName);
            createdFormat.ShouldNotBeNull();
            createdFormat!.Name.ShouldBe(formatName);
        }

        [Fact]
        public async Task Format_CannotCreateTwoFormatsWithTheSameName()
        {
            var result1 = await _formatService.CreateFormatAsync("HAT");
            var result2 = await _formatService.CreateFormatAsync("HAT");

            result1.Success.ShouldBeTrue();
            result2.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task Format_NewlyCreatedFormatsShouldNotBeSingleFormatMode()
        {
            await _formatService.CreateFormatAsync("Traditional");

            var format = await _formatService.GetFormatAsync("Traditional");
            format!.SingleFormatMode.ShouldBeFalse();
        }

        [Fact]
        public async Task Format_CanDeleteAnExistingFormat()
        {
            _ = await _formatService.CreateFormatAsync("HAT");

            var result = await _formatService.DeleteFormatAsync("HAT");

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();

            // Verify it's gone by trying to get it
            var checkFormat = await _formatService.GetFormatAsync("HAT");
            checkFormat.ShouldBeNull();
        }

        [Fact]
        public async Task Format_CanUpdateRulesForAnExistingFormat()
        {
            _ = await _formatService.CreateFormatAsync("HAT");
            var updatedRules = "{\"banList\": [\"Pot of Greed\", \"Monster Reborn\"]}";

            var result = await _formatService.UpdateFormatRulesAsync("HAT", updatedRules);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            var updatedFormat = await _formatService.GetFormatAsync("HAT");
            updatedFormat!.Rules.ShouldBe(updatedRules);
        }

        #region Season Specifications

        [Fact]
        public async Task Season_CanCreateANewSeasonForAFormat()
        {
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            var result = await _seasonService.CreateAsync(format!.Id, 1, 4);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            // Fetch the created season to verify properties
            var season = format.Seasons.SingleOrDefault(s => s.SeasonNumber == 1);
            season.ShouldNotBeNull();
            season!.SeasonNumber.ShouldBe(1);
            season.MinimumTeamMembers.ShouldBe(4);
        }

        [Fact]
        public async Task Season_CannotCreateTwoSeasonsWithTheSameNumberInTheSameFormat()
        {
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);

            var result1 = await _seasonService.CreateAsync(format!.Id, 1, 3);
            var result2 = await _seasonService.CreateAsync(format!.Id, 1, 3);

            result1.Success.ShouldBeFalse();
            result2.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task Season_NewlyCreatedSeasonsShouldBeInactive()
        {
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);

            var season = format!.Seasons.SingleOrDefault(s => s.SeasonNumber == 1);
            season!.Active.ShouldBeFalse();
        }

        [Fact]
        public async Task Season_NewlyCreatedSeasonsShouldAllowTeamModificationsByDefault()
        {
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);

            var season = format!.Seasons.SingleOrDefault(s => s.SeasonNumber == 1);
            season!.DisableTeamModification.ShouldBeFalse();
        }

        [Fact]
        public async Task Season_WhenTeamsArePresent_CanNotDeleteSeason()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");

            // Should throw exception because there are teams present in the season
            await Should.ThrowAsync<Exception>(async () =>
            {
                await _seasonService.DeleteAsync(format!.Id, 1);
            });
        }

        [Fact]
        public async Task Season_CanSetASeasonAsActive()
        {
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);
            var season = format!.Seasons.SingleOrDefault(s => s.SeasonNumber == 1);

            var result = await _seasonService.SetActiveAsync(format.Id, season!.SeasonNumber);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            // Fetch the season again to verify it's active
            var updatedFormat = await _formatService.GetFormatAsync("HAT");
            var updatedSeason = updatedFormat!.Seasons.Single(s => s.SeasonNumber == 1);
            updatedSeason.Active.ShouldBeTrue();
        }


        [Fact]
        public async Task Season_CanDisableTeamModificationsForASeason()
        {
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);
            var season = format!.Seasons.SingleOrDefault(s => s.SeasonNumber == 1);

            var result = await _seasonService.SetTeamModificationsAsync(season!.Id, enabled: false);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            // Fetch the season again to verify modifications are disabled
            var updatedFormat = await _formatService.GetFormatAsync("HAT");
            var updatedSeason = updatedFormat!.Seasons.Single(s => s.SeasonNumber == 1);
            updatedSeason.DisableTeamModification.ShouldBeTrue();
        }

        [Fact]
        public async Task Season_CanEnableTeamModificationsForASeason()
        {
            await _formatService.CreateFormatAsync("HAT");
            var format = await _formatService.GetFormatAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 1, 4);
            var season = format!.Seasons.SingleOrDefault(s => s.SeasonNumber == 1);

            var result = await _seasonService.SetTeamModificationsAsync(season!.Id, enabled: true);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            // Fetch the season again to verify modifications are enabled
            var updatedFormat = await _formatService.GetFormatAsync("HAT");
            var updatedSeason = updatedFormat!.Seasons.Single(s => s.SeasonNumber == 1);
            updatedSeason.DisableTeamModification.ShouldBeFalse();
        }

        #endregion







        #region Week Specifications

        [Fact]
        public async Task Week_CanCreateANewWeek()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            var subCloseDate = DateTime.Parse("2025-01-05");

            var result = await _weekService.CreateAsync(seasonId, 5, startDate, endDate, subCloseDate, 2);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            // Fetch the created week to verify properties
            var week = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            week.ShouldNotBeNull();
            week!.WeekNumber.ShouldBe(5);
            week.StartDate.ShouldBe(startDate);
            week.EndDate.ShouldBe(endDate);
            week.SubmissionsClosedDate.ShouldBe(subCloseDate);
        }

        [Fact]
        public async Task Week_CannotCreateTwoWeeksWithSameNumberInSameSeason()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 1, startDate, endDate, null, 2);

            var result = await _weekService.CreateAsync(seasonId, 1, startDate, endDate, null, 2);

            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task Week_NewlyCreatedWeeksShouldHaveNotOpenYetStatus()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");

            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);

            var week = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            week!.Status.ShouldBe(WeekStatus.NotOpenYet);
        }

        [Fact]
        public async Task Week_CanDeleteAWeek()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);

            var result = await _weekService.DeleteAsync(seasonId, 5);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            // Verify it's gone
            var deletedWeek = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            deletedWeek.ShouldBeNull();
        }

        [Fact]
        public async Task Week_CanUpdateWeekDates()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
            var newStartDate = DateTime.Parse("2025-02-01");
            var newEndDate = DateTime.Parse("2025-02-07");

            var result = await _weekService.UpdateAsync(seasonId, 5, newStartDate, newEndDate, null, null, 2);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            var updatedWeek = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            updatedWeek!.StartDate.ShouldBe(newStartDate);
            updatedWeek.EndDate.ShouldBe(newEndDate);
        }

        [Fact]
        public async Task Week_CanUpdateWeekStatus()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);

            var result = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Open, 2);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            var updatedWeek = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            updatedWeek!.Status.ShouldBe(WeekStatus.Open);
        }

        [Fact]
        public async Task Week_CanTransitionFromNotOpenYetToOpen()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
            var week = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            week!.Status.ShouldBe(WeekStatus.NotOpenYet);

            var result = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Open, 2);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            var updatedWeek = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            updatedWeek!.Status.ShouldBe(WeekStatus.Open);
        }

        [Fact]
        public async Task Week_CanTransitionFromOpenToSubmissionsClosed()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
            await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Open, 2);

            var result = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.SubmissionsClosed, 2);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            var updatedWeek = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            updatedWeek!.Status.ShouldBe(WeekStatus.SubmissionsClosed);
        }

        [Fact]
        public async Task Week_CanTransitionFromSubmissionsClosedToInProgress()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
            await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.SubmissionsClosed, 2);

            var result = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.InProgress, 2);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            var updatedWeek = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            updatedWeek!.Status.ShouldBe(WeekStatus.InProgress);
        }

        [Fact]
        public async Task Week_CanTransitionFromInProgressToCompleted()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null, 2);
            await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.InProgress, 2);

            var result = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Completed, 2);

            result.ShouldNotBeNull();
            result.Success.ShouldBeTrue();
            
            var updatedWeek = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 5 && w.SeasonId == seasonId);
            updatedWeek!.Status.ShouldBe(WeekStatus.Completed);
        }

        [Fact]
        public async Task Week_CloseSubmissionsReturnsErrorWhenNoOpenWeekExists()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;

            var result = await _weekService.CloseSubmissionsAsync(seasonId);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("No open week");
        }

        [Fact]
        public async Task Week_CloseWeekReturnsErrorWhenNoInProgressWeekExists()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;

            var result = await _weekService.CloseAsync(seasonId);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("No InProgress week");
        }

        #endregion

        #region Substitution Specifications

        [Fact]
        public async Task Substitution_CanSubstitutePlayerSuccessfully()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;

            // Create week 2 with InProgress status
            await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.InProgress, 2);
            var week2 = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 2 && w.SeasonId == seasonId);

            // Create a scheduled match and deck submission for Player1
            var player1 = Context.Players.First(p => p.UserName == "Player1");
            var player2 = Context.Players.First(p => p.UserName == "Player2");
            var player5 = Context.Players.First(p => p.UserName == "Player5");

            var match = new Match
            {
                WeekId = week2!.Id,
                Player1Id = player1.Id,
                Player2Id = player5.Id,
                Status = MatchStatus.Scheduled
            };
            Context.Matches.Add(match);

            var deckSubmission = new DeckSubmission
            {
                WeekId = week2.Id,
                PlayerId = player1.Id,
                DeckFile = "test.ydk",
                SeatNumber = 1
            };
            Context.DeckSubmissions.Add(deckSubmission);
            await Context.SaveChangesAsync();

            // Substitute Player2 in for Player1
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player2.Id, player1.Id);

            result.Success.ShouldBeTrue();
            result.Message.ShouldContain("Substitution successful");

            // Verify match was updated
            var updatedMatch = await Context.Matches.FindAsync(match.Id);
            updatedMatch!.Player1Id.ShouldBe(player2.Id);

            // Verify deck submission was updated
            var updatedDeck = await Context.DeckSubmissions.FindAsync(deckSubmission.Id);
            updatedDeck!.PlayerId.ShouldBe(player2.Id);
        }

        [Fact]
        public async Task Substitution_ReturnsErrorWhenTeamNotFound()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var player1 = Context.Players.First(p => p.UserName == "Player1");
            var player2 = Context.Players.First(p => p.UserName == "Player2");

            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Nonexistent Team", player2.Id, player1.Id);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not found");
        }

        [Fact]
        public async Task Substitution_ReturnsErrorWhenPlayerInNotOnTeam()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var player1 = Context.Players.First(p => p.UserName == "Player1");
            var player5 = Context.Players.First(p => p.UserName == "Player5"); // On Team Beta

            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player5.Id, player1.Id);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not a member");
        }

        [Fact]
        public async Task Substitution_ReturnsErrorWhenPlayerOutNotOnTeam()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var player1 = Context.Players.First(p => p.UserName == "Player1");
            var player5 = Context.Players.First(p => p.UserName == "Player5"); // On Team Beta

            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player1.Id, player5.Id);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not a member");
        }

        [Fact]
        public async Task Substitution_ReturnsErrorWhenPlayerInAlreadyPlaying()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;

            // Create week 2 with InProgress status
            await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.InProgress, 2);
            var week2 = await Context.Weeks.FirstOrDefaultAsync(w => w.WeekNumber == 2 && w.SeasonId == seasonId);

            var player1 = Context.Players.First(p => p.UserName == "Player1");
            var player2 = Context.Players.First(p => p.UserName == "Player2");
            var player5 = Context.Players.First(p => p.UserName == "Player5");
            var player6 = Context.Players.First(p => p.UserName == "Player6");

            // Player1 is already scheduled to play
            var match1 = new Match
            {
                WeekId = week2!.Id,
                Player1Id = player1.Id,
                Player2Id = player5.Id,
                Status = MatchStatus.Scheduled
            };

            // Player2 is also scheduled to play
            var match2 = new Match
            {
                WeekId = week2.Id,
                Player1Id = player2.Id,
                Player2Id = player6.Id,
                Status = MatchStatus.Scheduled
            };

            Context.Matches.AddRange(match1, match2);
            await Context.SaveChangesAsync();

            // Try to substitute Player2 (who's already playing) in for Player1
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player2.Id, player1.Id);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already scheduled");
        }

        [Fact]
        public async Task Substitution_ReturnsErrorWhenPlayerOutNotScheduledToPlay()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;

            // Create week 2 with InProgress status
            await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.InProgress, 2);

            var player1 = Context.Players.First(p => p.UserName == "Player1");
            var player2 = Context.Players.First(p => p.UserName == "Player2");

            // No matches created, so Player1 is not scheduled to play
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player2.Id, player1.Id);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not currently scheduled");
        }

        [Fact]
        public async Task Substitution_ReturnsErrorWhenNoInProgressWeekExists()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var player1 = Context.Players.First(p => p.UserName == "Player1");
            var player2 = Context.Players.First(p => p.UserName == "Player2");

            // Week 1 exists but is Completed, no InProgress week
            var result = await _substitutionService.SubstitutePlayerAsync(seasonId, "Team Alpha", player2.Id, player1.Id);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("No InProgress week");
        }

        #endregion


        #region Even Number Teams - Round Robin Pairings

        [Fact]
        public async Task GeneratePairings_TwoTeams_CreatesCorrectPairings()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 3);
            await CloseWeekSubmissions(weekId);

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeTrue();
            result.Week.ShouldNotBeNull();
            result.CreatedMatches.ShouldNotBeNull();
            result.CreatedMatches.Count.ShouldBe(3); // 3 players per team = 3 matches
            result.Week.Status.ShouldBe(WeekStatus.InProgress);
        }

        [Fact]
        public async Task GeneratePairings_FourTeams_CreatesCorrectPairings()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 4, playersPerTeam: 2);
            await CloseWeekSubmissions(weekId);

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeTrue();
            result.CreatedMatches!.Count.ShouldBe(4); // 4 teams -> 2 matchups -> 2 players each = 4 matches
        }

        [Fact]
        public async Task GeneratePairings_SixTeams_CreatesCorrectPairings()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 6, playersPerTeam: 2);
            await CloseWeekSubmissions(weekId);

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeTrue();
            result.CreatedMatches!.Count.ShouldBe(6); // 6 teams -> 3 matchups -> 2 players each = 6 matches
        }

        #endregion

        #region Odd Number Teams - BYE Handling

        [Fact]
        public async Task GeneratePairings_ThreeTeams_OneTeamGetsBye()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 3, playersPerTeam: 2);
            await CloseWeekSubmissions(weekId);

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeTrue();
            // 3 teams -> 1 matchup (other gets BYE) -> 2 players = 2 matches
            result.CreatedMatches!.Count.ShouldBe(2);
            result.WeeklyMatchups!.Count.ShouldBe(1); // Only 1 actual matchup
        }

        [Fact]
        public async Task GeneratePairings_FiveTeams_OneTeamGetsBye()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 5, playersPerTeam: 2);
            await CloseWeekSubmissions(weekId);

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeTrue();
            // 5 teams -> 2 matchups (one gets BYE) -> 2 players each = 4 matches
            result.CreatedMatches!.Count.ShouldBe(4);
            result.WeeklyMatchups!.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GeneratePairings_SevenTeams_OneTeamGetsBye()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 7, playersPerTeam: 2);
            await CloseWeekSubmissions(weekId);

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeTrue();
            // 7 teams -> 3 matchups (one gets BYE) -> 2 players each = 6 matches
            result.CreatedMatches!.Count.ShouldBe(6);
            result.WeeklyMatchups!.Count.ShouldBe(3);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GeneratePairings_SingleTeam_ReturnsError()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 1, playersPerTeam: 3);
            await CloseWeekSubmissions(weekId);

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("at least 2 teams");
        }

        [Fact]
        public async Task GeneratePairings_ZeroTeams_ReturnsError()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 0, playersPerTeam: 0);
            await CloseWeekSubmissions(weekId);

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("at least 2 teams");
        }

        [Fact]
        public async Task GeneratePairings_NoWeekInSubmissionsClosedState_ReturnsError()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            // Do NOT close submissions - leave week in Open state

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("SubmissionsClosed");
        }

        [Fact]
        public async Task GeneratePairings_MatchesAlreadyExist_ReturnsError()
        {
            var (seasonId, weekId) = await SetupSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            await CloseWeekSubmissions(weekId);

            // Generate pairings first time
            var firstResult = await _matchService.GeneratePairingsAsync(seasonId);
            firstResult.Success.ShouldBeTrue();

            // Revert week status to SubmissionsClosed to test duplicate prevention
            var week = await Context.Weeks.FindAsync(weekId);
            week!.Status = WeekStatus.SubmissionsClosed;
            await Context.SaveChangesAsync();

            // Try to generate again - should fail
            var secondResult = await _matchService.GeneratePairingsAsync(seasonId);

            secondResult.Success.ShouldBeFalse();
            secondResult.Message.ShouldContain("already exist");
        }

        #endregion

        #region Seat-Based Pairing Verification

        [Fact]
        public async Task GeneratePairings_PairsBySeatNumber_Seat1VsSeat1()
        {
            var (seasonId, weekId, teams, players) = await SetupDetailedSeasonWithSubmissions();
            await CloseWeekSubmissions(weekId);

            var result = await _matchService.GeneratePairingsAsync(seasonId);

            result.Success.ShouldBeTrue();

            // Verify matches are created
            var matches = result.CreatedMatches!.ToList();
            matches.Count.ShouldBe(3); // 3 seats = 3 matches

            // Get team1 and team2 players by seat
            var team1Players = players.Where(p => p.UserName.Contains("Team1")).OrderBy(p => p.UserName).ToList();
            var team2Players = players.Where(p => p.UserName.Contains("Team2")).OrderBy(p => p.UserName).ToList();

            // Verify seat 1 vs seat 1
            var seat1Match = matches.SingleOrDefault(m =>
                (m.Player1Id == team1Players[0].Id && m.Player2Id == team2Players[0].Id) ||
                (m.Player1Id == team2Players[0].Id && m.Player2Id == team1Players[0].Id));
            seat1Match.ShouldNotBeNull();

            // Verify seat 2 vs seat 2
            var seat2Match = matches.SingleOrDefault(m =>
                (m.Player1Id == team1Players[1].Id && m.Player2Id == team2Players[1].Id) ||
                (m.Player1Id == team2Players[1].Id && m.Player2Id == team1Players[1].Id));
            seat2Match.ShouldNotBeNull();

            // Verify seat 3 vs seat 3
            var seat3Match = matches.SingleOrDefault(m =>
                (m.Player1Id == team1Players[2].Id && m.Player2Id == team2Players[2].Id) ||
                (m.Player1Id == team2Players[2].Id && m.Player2Id == team1Players[2].Id));
            seat3Match.ShouldNotBeNull();
        }

        [Fact]
        public async Task GeneratePairings_DifferentSeatCounts_OnlyPairsMatchingSeats()
        {
            var format = new Format { Name = "TestFormat", Rules = "{}" };
            await Context.Formats.AddAsync(format);
            await Context.SaveChangesAsync();

            var season = new Season { SeasonNumber = 1, FormatId = format.Id, Active = true, MinimumTeamMembers = 2 };
            await Context.Seasons.AddAsync(season);
            await Context.SaveChangesAsync();

            // Create 2 teams
            var team1Captain = new Player { DiscordUserId = 1001, UserName = "Captain1" };
            var team2Captain = new Player { DiscordUserId = 2001, UserName = "Captain2" };
            await Context.Players.AddRangeAsync(team1Captain, team2Captain);
            await Context.SaveChangesAsync();

            var team1 = new Team { Name = "Team1", CaptainId = team1Captain.Id, SeasonId = season.Id };
            var team2 = new Team { Name = "Team2", CaptainId = team2Captain.Id, SeasonId = season.Id };
            await Context.Teams.AddRangeAsync(team1, team2);
            await Context.SaveChangesAsync();

            // Team1 has 3 players, Team2 has 2 players
            var t1p1 = new Player { DiscordUserId = 1011, UserName = "T1P1" };
            var t1p2 = new Player { DiscordUserId = 1012, UserName = "T1P2" };
            var t1p3 = new Player { DiscordUserId = 1013, UserName = "T1P3" };
            var t2p1 = new Player { DiscordUserId = 2011, UserName = "T2P1" };
            var t2p2 = new Player { DiscordUserId = 2012, UserName = "T2P2" };
            await Context.Players.AddRangeAsync(t1p1, t1p2, t1p3, t2p1, t2p2);
            await Context.SaveChangesAsync();

            await Context.PlayerSeasonTeams.AddRangeAsync(
                new PlayerSeasonTeam { PlayerId = t1p1.Id, SeasonId = season.Id, TeamId = team1.Id },
                new PlayerSeasonTeam { PlayerId = t1p2.Id, SeasonId = season.Id, TeamId = team1.Id },
                new PlayerSeasonTeam { PlayerId = t1p3.Id, SeasonId = season.Id, TeamId = team1.Id },
                new PlayerSeasonTeam { PlayerId = t2p1.Id, SeasonId = season.Id, TeamId = team2.Id },
                new PlayerSeasonTeam { PlayerId = t2p2.Id, SeasonId = season.Id, TeamId = team2.Id }
            );
            await Context.SaveChangesAsync();

            var week = new Week
            {
                WeekNumber = 1,
                SeasonId = season.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = WeekStatus.Open,
                SubmissionsRequired = 3
            };
            await Context.Weeks.AddAsync(week);
            await Context.SaveChangesAsync();

            // Submit decks - Team1 has 3 submissions, Team2 has only 2
            await Context.DeckSubmissions.AddRangeAsync(
                new DeckSubmission { PlayerId = t1p1.Id, WeekId = week.Id, SeatNumber = 1, DeckFile = "deck1" },
                new DeckSubmission { PlayerId = t1p2.Id, WeekId = week.Id, SeatNumber = 2, DeckFile = "deck2" },
                new DeckSubmission { PlayerId = t1p3.Id, WeekId = week.Id, SeatNumber = 3, DeckFile = "deck3" },
                new DeckSubmission { PlayerId = t2p1.Id, WeekId = week.Id, SeatNumber = 1, DeckFile = "deck1" },
                new DeckSubmission { PlayerId = t2p2.Id, WeekId = week.Id, SeatNumber = 2, DeckFile = "deck2" }
            );
            await Context.SaveChangesAsync();

            week.Status = WeekStatus.SubmissionsClosed;
            await Context.SaveChangesAsync();

            var result = await _matchService.GeneratePairingsAsync(season.Id);

            result.Success.ShouldBeTrue();
            // Should only create 2 matches (min of 3 and 2)
            result.CreatedMatches!.Count.ShouldBe(2);

            // Team1 player 3 should be unpaired
            result.WeeklyMatchups![0].UnpairedA.Count.ShouldBe(1);
            result.WeeklyMatchups[0].UnpairedA[0].Id.ShouldBe(t1p3.Id);
        }

        #endregion

        #region Helper Methods

        private async Task<(int seasonId, int weekId)> SetupSeasonWithTeamsAndSubmissions(int teamCount, int playersPerTeam)
        {
            var (seasonId, weekId, teams, players) = await SetupDetailedSeasonWithSubmissions(teamCount, playersPerTeam);
            return (seasonId, weekId);
        }

        private async Task<(int seasonId, int weekId, List<Team> teams, List<Player> players)> SetupDetailedSeasonWithSubmissions(int teamCount = 2, int playersPerTeam = 3)
        {
            var format = new Format { Name = $"TestFormat{Guid.NewGuid()}", Rules = "{}" };
            await Context.Formats.AddAsync(format);
            await Context.SaveChangesAsync();

            var season = new Season
            {
                SeasonNumber = 1,
                FormatId = format.Id,
                Active = true,
                MinimumTeamMembers = playersPerTeam
            };
            await Context.Seasons.AddAsync(season);
            await Context.SaveChangesAsync();

            var teams = new List<Team>();
            var players = new List<Player>();

            for (int t = 0; t < teamCount; t++)
            {
                var captain = new Player { DiscordUserId = (ulong)(1000 + t * 100), UserName = $"Captain{t + 1}" };
                await Context.Players.AddAsync(captain);
                await Context.SaveChangesAsync();

                var team = new Team
                {
                    Name = $"Team{t + 1}",
                    CaptainId = captain.Id,
                    SeasonId = season.Id,
                    DiscordRoleId = (ulong)(5000 + t)
                };
                await Context.Teams.AddAsync(team);
                await Context.SaveChangesAsync();
                teams.Add(team);

                for (int p = 0; p < playersPerTeam; p++)
                {
                    var player = new Player
                    {
                        DiscordUserId = (ulong)(1000 + t * 100 + p + 1),
                        UserName = $"Team{t + 1}Player{p + 1}"
                    };
                    await Context.Players.AddAsync(player);
                    await Context.SaveChangesAsync();
                    players.Add(player);

                    await Context.PlayerSeasonTeams.AddAsync(new PlayerSeasonTeam
                    {
                        PlayerId = player.Id,
                        SeasonId = season.Id,
                        TeamId = team.Id
                    });
                }
            }
            await Context.SaveChangesAsync();

            var week = new Week
            {
                WeekNumber = 1,
                SeasonId = season.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = WeekStatus.Open,
                SubmissionsRequired = playersPerTeam
            };
            await Context.Weeks.AddAsync(week);
            await Context.SaveChangesAsync();

            // Create deck submissions for all players
            int submissionId = 1;
            foreach (var team in teams)
            {
                var teamPlayers = players.Where(p => p.UserName.StartsWith(team.Name)).ToList();
                for (int seatNum = 1; seatNum <= teamPlayers.Count; seatNum++)
                {
                    await Context.DeckSubmissions.AddAsync(new DeckSubmission
                    {
                        PlayerId = teamPlayers[seatNum - 1].Id,
                        WeekId = week.Id,
                        SeatNumber = seatNum,
                        DeckFile = $"Deck content {submissionId++}"
                    });
                }
            }
            await Context.SaveChangesAsync();

            return (season.Id, week.Id, teams, players);
        }

        private async Task CloseWeekSubmissions(int weekId)
        {
            var week = await Context.Weeks.FindAsync(weekId);
            if (week != null)
            {
                week.Status = WeekStatus.SubmissionsClosed;
                await Context.SaveChangesAsync();
            }
        }

        #endregion


        #region Seat Conflict Tests

        [Fact]
        public async Task SubmitAsync_TwoPlayersFromSameTeamClaimSameSeat_ShouldFail()
        {
            var (seasonId, player1, _) = await SetupSeasonWithPlayerAndWeek();

            // Create second player on same team
            var player2 = new Player { DiscordUserId = 777666, UserName = "Player2SameTeam" };
            await Context.Players.AddAsync(player2);
            var team = await Context.Teams.FirstAsync();
            await Context.PlayerSeasonTeams.AddAsync(new PlayerSeasonTeam
            {
                PlayerId = player2.Id,
                SeasonId = seasonId,
                TeamId = team.Id
            });
            await Context.SaveChangesAsync();

            // Player1 takes seat 1
            var result1 = await _deckSubmissionService.SubmitAsync(seasonId, player1.Id, "player1 deck", seatNumber: 1);
            result1.Success.ShouldBeTrue();

            // Player2 tries to take seat 1 - should fail
            var result2 = await _deckSubmissionService.SubmitAsync(seasonId, player2.Id, "player2 deck", seatNumber: 1);
            result2.Success.ShouldBeFalse();
            result2.Message.ShouldContain("already taken");
            result2.Message.ShouldContain(player1.UserName);
        }

        [Fact]
        public async Task SubmitAsync_PlayersFromDifferentTeamsCanUseSameSeat_ShouldSucceed()
        {
            var (seasonId, player1Team1, _) = await SetupSeasonWithPlayerAndWeek();

            // Create second team and player
            var format = await Context.Formats.FirstAsync();
            var season = await Context.Seasons.FirstAsync();

            var captain2 = new Player { DiscordUserId = 888777, UserName = "Captain2" };
            await Context.Players.AddAsync(captain2);
            await Context.SaveChangesAsync();

            var team2 = new Team
            {
                Name = "Team2",
                CaptainId = captain2.Id,
                SeasonId = season.Id,
                DiscordRoleId = 9999
            };
            await Context.Teams.AddAsync(team2);
            await Context.SaveChangesAsync();

            var player2Team2 = new Player { DiscordUserId = 888888, UserName = "Player2Team2" };
            await Context.Players.AddAsync(player2Team2);
            await Context.PlayerSeasonTeams.AddAsync(new PlayerSeasonTeam
            {
                PlayerId = player2Team2.Id,
                SeasonId = seasonId,
                TeamId = team2.Id
            });
            await Context.SaveChangesAsync();

            // Both players submit to seat 1 - should both succeed (different teams)
            var result1 = await _deckSubmissionService.SubmitAsync(seasonId, player1Team1.Id, "team1 deck", seatNumber: 1);
            var result2 = await _deckSubmissionService.SubmitAsync(seasonId, player2Team2.Id, "team2 deck", seatNumber: 1);

            result1.Success.ShouldBeTrue();
            result2.Success.ShouldBeTrue();

            // Verify both submissions exist
            var submissions = await Context.DeckSubmissions.ToListAsync();
            submissions.Count.ShouldBe(2);
        }

        [Fact]
        public async Task SubmitAsync_SeatNumberOutOfRange_ShouldFail()
        {
            var (seasonId, player, _) = await SetupSeasonWithPlayerAndWeek(submissionsRequired: 3);

            // Try seat 0 - should fail
            var result0 = await _deckSubmissionService.SubmitAsync(seasonId, player.Id, "deck", seatNumber: 0);
            result0.Success.ShouldBeFalse();
            result0.Message.ShouldContain("Seat number must be between");

            // Try seat 4 (max is 3) - should fail
            var result4 = await _deckSubmissionService.SubmitAsync(seasonId, player.Id, "deck", seatNumber: 4);
            result4.Success.ShouldBeFalse();
            result4.Message.ShouldContain("Seat number must be between");
        }

        #endregion

        #region Upsert Behavior Tests

        [Fact]
        public async Task SubmitAsync_ResubmitDeckForSamePlayerWeek_UpdatesExisting()
        {
            var (seasonId, player, weekId) = await SetupSeasonWithPlayerAndWeek();

            // First submission
            var result1 = await _deckSubmissionService.SubmitAsync(seasonId, player.Id, "original deck", seatNumber: 1);
            result1.Success.ShouldBeTrue();
            result1.Message.ShouldContain("submitted");

            // Get the submission
            var submission1 = await Context.DeckSubmissions
                .FirstOrDefaultAsync(ds => ds.PlayerId == player.Id && ds.WeekId == weekId);
            submission1.ShouldNotBeNull();
            submission1!.DeckFile.ShouldBe("original deck");
            submission1.SeatNumber.ShouldBe(1);
            var originalDate = submission1.SubmittedDate;

            // Wait a moment to ensure time difference
            await Task.Delay(50);

            // Resubmit with new content and different seat
            var result2 = await _deckSubmissionService.SubmitAsync(seasonId, player.Id, "updated deck", seatNumber: 2);
            result2.Success.ShouldBeTrue();
            result2.Message.ShouldContain("updated");

            // Verify only one submission exists (upserted, not duplicated)
            var allSubmissions = await Context.DeckSubmissions
                .Where(ds => ds.PlayerId == player.Id && ds.WeekId == weekId)
                .ToListAsync();
            allSubmissions.Count.ShouldBe(1);

            // Verify content and seat were updated
            var updatedSubmission = allSubmissions[0];
            updatedSubmission.DeckFile.ShouldBe("updated deck");
            updatedSubmission.SeatNumber.ShouldBe(2);
            updatedSubmission.SubmittedDate.ShouldBeGreaterThan(originalDate);
        }

        [Fact]
        public async Task SubmitAsync_UpdateSeatToOneTakenByOther_ShouldFail()
        {
            var (seasonId, player1, _) = await SetupSeasonWithPlayerAndWeek();

            // Create second player on same team
            var player2 = new Player { DiscordUserId = 555444, UserName = "Player2" };
            await Context.Players.AddAsync(player2);
            var team = await Context.Teams.FirstAsync();
            await Context.PlayerSeasonTeams.AddAsync(new PlayerSeasonTeam
            {
                PlayerId = player2.Id,
                SeasonId = seasonId,
                TeamId = team.Id
            });
            await Context.SaveChangesAsync();

            // Player1 takes seat 1
            var result1 = await _deckSubmissionService.SubmitAsync(seasonId, player1.Id, "deck1", seatNumber: 1);
            result1.Success.ShouldBeTrue();

            // Player2 takes seat 2
            var result2 = await _deckSubmissionService.SubmitAsync(seasonId, player2.Id, "deck2", seatNumber: 2);
            result2.Success.ShouldBeTrue();

            // Player1 tries to update their submission to seat 2 (taken by player2) - should fail
            var result3 = await _deckSubmissionService.SubmitAsync(seasonId, player1.Id, "deck1 updated", seatNumber: 2);
            result3.Success.ShouldBeFalse();
            result3.Message.ShouldContain("already taken");
        }

        [Fact]
        public async Task SubmitAsync_UpdateToSameSeatAsCurrently_ShouldSucceed()
        {
            var (seasonId, player, _) = await SetupSeasonWithPlayerAndWeek();

            // Initial submission
            var result1 = await _deckSubmissionService.SubmitAsync(seasonId, player.Id, "original", seatNumber: 1);
            result1.Success.ShouldBeTrue();

            // Update deck content but keep same seat - should succeed
            var result2 = await _deckSubmissionService.SubmitAsync(seasonId, player.Id, "updated", seatNumber: 1);
            result2.Success.ShouldBeTrue();
            result2.Message.ShouldContain("updated");

            // Verify submission was updated
            var submission = await Context.DeckSubmissions
                .FirstOrDefaultAsync(ds => ds.PlayerId == player.Id);
            submission.ShouldNotBeNull();
            submission!.DeckFile.ShouldBe("updated");
            submission.SeatNumber.ShouldBe(1);
        }

        #endregion

        #region Week Status Validation

        [Fact]
        public async Task SubmitAsync_WeekNotOpen_ShouldFail()
        {
            var (seasonId, player, weekId) = await SetupSeasonWithPlayerAndWeek();

            // Close the week
            var week = await Context.Weeks.FindAsync(weekId);
            week!.Status = WeekStatus.SubmissionsClosed;
            await Context.SaveChangesAsync();

            var result = await _deckSubmissionService.SubmitAsync(seasonId, player.Id, "deck", seatNumber: 1);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not open");
        }

        [Fact]
        public async Task SubmitAsync_NoOpenWeek_ShouldFail()
        {
            var (seasonId, player, weekId) = await SetupSeasonWithPlayerAndWeek();

            // Set week to NotOpenYet
            var week = await Context.Weeks.FindAsync(weekId);
            week!.Status = WeekStatus.NotOpenYet;
            await Context.SaveChangesAsync();

            var result = await _deckSubmissionService.SubmitAsync(seasonId, player.Id, "deck", seatNumber: 1);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("No open week");
        }

        #endregion

        #region Player Validation

        [Fact]
        public async Task SubmitAsync_PlayerNotOnTeam_ShouldFail()
        {
            var (seasonId, _, _) = await SetupSeasonWithPlayerAndWeek();

            // Create player not on any team
            var unassignedPlayer = new Player { DiscordUserId = 111222, UserName = "UnassignedPlayer" };
            await Context.Players.AddAsync(unassignedPlayer);
            await Context.SaveChangesAsync();

            var result = await _deckSubmissionService.SubmitAsync(seasonId, unassignedPlayer.Id, "deck", seatNumber: 1);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("not on any team");
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task DeleteSubmissionAsync_ExistingSubmission_ShouldSucceed()
        {
            var (seasonId, player, weekId) = await SetupSeasonWithPlayerAndWeek();

            // Create submission
            await _deckSubmissionService.SubmitAsync(seasonId, player.Id, "deck", seatNumber: 1);

            // Delete it
            var deleteResult = await _deckSubmissionService.DeleteSubmissionAsync(seasonId, player.Id);

            deleteResult.Success.ShouldBeTrue();

            // Verify it's gone
            var submission = await Context.DeckSubmissions
                .FirstOrDefaultAsync(ds => ds.PlayerId == player.Id && ds.WeekId == weekId);
            submission.ShouldBeNull();
        }

        [Fact]
        public async Task DeleteSubmissionAsync_NonExistentSubmission_ShouldFail()
        {
            var (seasonId, player, _) = await SetupSeasonWithPlayerAndWeek();

            // Try to delete without creating submission
            var result = await _deckSubmissionService.DeleteSubmissionAsync(seasonId, player.Id);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("No existing deck submission found");
        }

        [Fact]
        public async Task DeleteSubmissionAsync_FreesSeatForOtherPlayers()
        {
            var (seasonId, player1, _) = await SetupSeasonWithPlayerAndWeek();

            // Create second player
            var player2 = new Player { DiscordUserId = 333444, UserName = "Player2" };
            await Context.Players.AddAsync(player2);
            var team = await Context.Teams.FirstAsync();
            await Context.PlayerSeasonTeams.AddAsync(new PlayerSeasonTeam
            {
                PlayerId = player2.Id,
                SeasonId = seasonId,
                TeamId = team.Id
            });
            await Context.SaveChangesAsync();

            // Player1 takes seat 1
            await _deckSubmissionService.SubmitAsync(seasonId, player1.Id, "deck1", seatNumber: 1);

            // Player2 can't take seat 1
            var failResult = await _deckSubmissionService.SubmitAsync(seasonId, player2.Id, "deck2", seatNumber: 1);
            failResult.Success.ShouldBeFalse();

            // Player1 deletes their submission
            await _deckSubmissionService.DeleteSubmissionAsync(seasonId, player1.Id);

            // Now player2 CAN take seat 1
            var successResult = await _deckSubmissionService.SubmitAsync(seasonId, player2.Id, "deck2", seatNumber: 1);
            successResult.Success.ShouldBeTrue();
        }

        #endregion

        #region Helper Methods

        private async Task<(int seasonId, Player player, int weekId)> SetupSeasonWithPlayerAndWeek(int submissionsRequired = 3)
        {
            var format = new Format { Name = $"TestFormat{Guid.NewGuid()}", Rules = "{}" };
            await Context.Formats.AddAsync(format);
            await Context.SaveChangesAsync();

            var season = new Season
            {
                SeasonNumber = 1,
                FormatId = format.Id,
                Active = true,
                MinimumTeamMembers = 3
            };
            await Context.Seasons.AddAsync(season);
            await Context.SaveChangesAsync();

            var captain = new Player { DiscordUserId = 123456, UserName = "Captain" };
            await Context.Players.AddAsync(captain);
            await Context.SaveChangesAsync();

            var team = new Team
            {
                Name = "TestTeam",
                CaptainId = captain.Id,
                SeasonId = season.Id,
                DiscordRoleId = 789456
            };
            await Context.Teams.AddAsync(team);
            await Context.SaveChangesAsync();

            var player = new Player { DiscordUserId = 654321, UserName = "Player1" };
            await Context.Players.AddAsync(player);
            await Context.SaveChangesAsync();

            await Context.PlayerSeasonTeams.AddAsync(new PlayerSeasonTeam
            {
                PlayerId = player.Id,
                SeasonId = season.Id,
                TeamId = team.Id
            });
            await Context.SaveChangesAsync();

            var week = new Week
            {
                WeekNumber = 1,
                SeasonId = season.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = WeekStatus.Open,
                SubmissionsRequired = submissionsRequired
            };
            await Context.Weeks.AddAsync(week);
            await Context.SaveChangesAsync();

            return (season.Id, player, week.Id);
        }

        private async Task<(int seasonId, Player player)> SetupSeasonWithPlayer()
        {
            var format = new Format { Name = $"TestFormat{Guid.NewGuid()}", Rules = "{}" };
            await Context.Formats.AddAsync(format);
            await Context.SaveChangesAsync();

            var season = new Season
            {
                SeasonNumber = 1,
                FormatId = format.Id,
                Active = true,
                MinimumTeamMembers = 3
            };
            await Context.Seasons.AddAsync(season);
            await Context.SaveChangesAsync();

            var captain = new Player { DiscordUserId = 123456, UserName = "Captain" };
            await Context.Players.AddAsync(captain);
            await Context.SaveChangesAsync();

            var team = new Team
            {
                Name = "TestTeam",
                CaptainId = captain.Id,
                SeasonId = season.Id,
                DiscordRoleId = 789456
            };
            await Context.Teams.AddAsync(team);
            await Context.SaveChangesAsync();

            var player = new Player { DiscordUserId = 654321, UserName = "Player1" };
            await Context.Players.AddAsync(player);
            await Context.SaveChangesAsync();

            await Context.PlayerSeasonTeams.AddAsync(new PlayerSeasonTeam
            {
                PlayerId = player.Id,
                SeasonId = season.Id,
                TeamId = team.Id
            });
            await Context.SaveChangesAsync();

            return (season.Id, player);
        }

        #endregion
    }
}
