using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Services;

namespace WarLeague.Test
{
    /// <summary>
    /// Domain behavior specifications - testing domain services and business rules.
    /// Tests focus on behavior and outcomes, not implementation details.
    /// </summary>
    public class Specifications : TransactionalTestBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FormatService _formatService;
        private readonly SeasonService _seasonService;
        private readonly WeekService _weekService;
        private readonly TeamService _teamService;

        public Specifications(DatabaseFixtureSeeded fixture) : base(fixture)
        {
            _serviceProvider = TestServiceProvider.CreateServiceProvider(Context);
            
            _formatService = _serviceProvider.GetRequiredService<FormatService>();
            _seasonService = _serviceProvider.GetRequiredService<SeasonService>();
            _weekService = _serviceProvider.GetRequiredService<WeekService>();
            _teamService = _serviceProvider.GetRequiredService<TeamService>();
        }

        [Fact]
        public async Task Format_CanCreateANewFormat()
        {
            var formatName = "Speed Duel";

            var createdFormat = await _formatService.CreateFormatAsync(formatName);

            createdFormat.ShouldNotBeNull();
            createdFormat.Name.ShouldBe(formatName);
        }

        [Fact]
        public async Task Format_CannotCreateTwoFormatsWithTheSameName()
        {
            var result1 = await _formatService.CreateFormatAsync("HAT");
            var result2 = await _formatService.CreateFormatAsync("HAT");

            result2.ShouldBeNull();
        }

        [Fact]
        public async Task Format_NewlyCreatedFormatsShouldNotBeSingleFormatMode()
        {
            var result = await _formatService.CreateFormatAsync("Traditional");

            result!.SingleFormatMode.ShouldBeFalse();
        }

        [Fact]
        public async Task Format_CanDeleteAnExistingFormat()
        {
            _ = await _formatService.CreateFormatAsync("HAT");

            var deletedFormat = await _formatService.DeleteFormatAsync("HAT");

            deletedFormat.ShouldNotBeNull();
            
            // Verify it's gone by trying to get it
            var checkFormat = await _formatService.GetFormatAsync("HAT");
            checkFormat.ShouldBeNull();
        }

        [Fact]
        public async Task Format_CanUpdateRulesForAnExistingFormat()
        {
            _ = await _formatService.CreateFormatAsync("HAT");
            var updatedRules = "{\"banList\": [\"Pot of Greed\", \"Monster Reborn\"]}";

            var updatedFormat = await _formatService.UpdateFormatRulesAsync("HAT", updatedRules);

            updatedFormat.ShouldNotBeNull();
            updatedFormat.Rules.ShouldBe(updatedRules);
        }

        #region Season Specifications

        [Fact]
        public async Task Season_CanCreateANewSeasonForAFormat()
        {
            var format = await _formatService.CreateFormatAsync("HAT");
            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

            season.ShouldNotBeNull();
            season.SeasonNumber.ShouldBe(1);
            season.MinimumTeamMembers.ShouldBe(4);
        }

        [Fact]
        public async Task Season_CannotCreateTwoSeasonsWithTheSameNumberInTheSameFormat()
        {
            var format = await _formatService.CreateFormatAsync("HAT");
            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

            var result1 = await _seasonService.CreateAsync(format!.Id, 1, 3);
            var result2 = await _seasonService.CreateAsync(format!.Id, 1, 3);

            result2.ShouldBeNull();
        }

        [Fact]
        public async Task Season_NewlyCreatedSeasonsShouldBeInactive()
        {
            var format = await _formatService.CreateFormatAsync("HAT");
            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

            season!.Active.ShouldBeFalse();
        }

        [Fact]
        public async Task Season_NewlyCreatedSeasonsShouldAllowTeamModificationsByDefault()
        {
            var format = await _formatService.CreateFormatAsync("HAT");
            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

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
            var format = await _formatService.CreateFormatAsync("HAT");
            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

            var result = await _seasonService.SetActiveAsync(format.Id, season!.SeasonNumber);

            result.ShouldNotBeNull();
            result.Active.ShouldBeTrue();
        }


        [Fact]
        public async Task Season_CanDisableTeamModificationsForASeason()
        {
            var format = await _formatService.CreateFormatAsync("HAT");
            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

            var updatedSeason = await _seasonService.SetTeamModificationsAsync(season!.Id, enabled: false);

            updatedSeason.ShouldNotBeNull();
            updatedSeason.DisableTeamModification.ShouldBeTrue();
        }

        [Fact]
        public async Task Season_CanEnableTeamModificationsForASeason()
        {
            var format = await _formatService.CreateFormatAsync("HAT");
            var season = await _seasonService.CreateAsync(format!.Id, 1, 4);

            var updatedSeason = await _seasonService.SetTeamModificationsAsync(season!.Id, enabled: true);

            updatedSeason.ShouldNotBeNull();
            updatedSeason.DisableTeamModification.ShouldBeFalse();
        }

        [Fact]
        public async Task Season_WhenTeamModificationsAreDisabled_CannotAddMembersToTeam()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            await _seasonService.SetTeamModificationsAsync(seasonId, enabled: false);

            // Should throw exception because team modifications are disabled for the season
            await Should.ThrowAsync<Exception>(async () =>
            {
                await _teamService.AddMemberAsync(seasonId, 1, "Team Alpha");
            });
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

            var week = await _weekService.CreateAsync(seasonId, 5, startDate, endDate, subCloseDate);

            week.ShouldNotBeNull();
            week.WeekNumber.ShouldBe(5);
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
            await _weekService.CreateAsync(seasonId, 1, startDate, endDate, null);

            var result = await _weekService.CreateAsync(seasonId, 1, startDate, endDate, null);

            result.ShouldBeNull();
        }

        [Fact]
        public async Task Week_NewlyCreatedWeeksShouldHaveNotOpenYetStatus()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");

            var week = await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null);

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
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null);

            var deletedWeek = await _weekService.DeleteAsync(seasonId, 5);

            deletedWeek.ShouldNotBeNull();
            deletedWeek.WeekNumber.ShouldBe(5);
        }

        [Fact]
        public async Task Week_CanUpdateWeekDates()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null);
            var newStartDate = DateTime.Parse("2025-02-01");
            var newEndDate = DateTime.Parse("2025-02-07");

            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, newStartDate, newEndDate, null, null);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.StartDate.ShouldBe(newStartDate);
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
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null);

            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Open);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.Open);
        }

        [Fact]
        public async Task Week_CanTransitionFromNotOpenYetToOpen()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            var week = await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null);
            week!.Status.ShouldBe(WeekStatus.NotOpenYet);

            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Open);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.Open);
        }

        [Fact]
        public async Task Week_CanTransitionFromOpenToSubmissionsClosed()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null);
            await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Open);

            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.SubmissionsClosed);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.SubmissionsClosed);
        }

        [Fact]
        public async Task Week_CanTransitionFromSubmissionsClosedToInProgress()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null);
            await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.SubmissionsClosed);

            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.InProgress);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.InProgress);
        }

        [Fact]
        public async Task Week_CanTransitionFromInProgressToCompleted()
        {
            Seed.SeedData(Context);
            var format = await _formatService.GetFormatAsync("HAT");
            var seasonId = format!.Seasons.First().Id;
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 5, startDate, endDate, null);
            await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.InProgress);

            var updatedWeek = await _weekService.UpdateAsync(seasonId, 5, null, null, null, WeekStatus.Completed);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.Completed);
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
    }
}
