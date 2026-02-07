using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;

namespace WarLeague.Test
{
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

            await _formatService.CreateFormatAsync(formatName);

            var createdFormat = await _formatService.GetFormatAsync(formatName);
            createdFormat.ShouldNotBeNull();
        }

        [Fact]
        public async Task Format_CannotCreateTwoFormatsWithTheSameName()
        {
            Seed.SeedData(Context);

            var result = await _formatService.CreateFormatAsync("HAT");

            result.ShouldBeNull();
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
            Seed.SeedData(Context);

            await _formatService.DeleteFormatAsync("GOAT");

            var deletedFormat = await _formatService.GetFormatAsync("GOAT");
            deletedFormat.ShouldBeNull();
        }

        [Fact]
        public async Task Format_CanUpdateRulesForAnExistingFormat()
        {
            Seed.SeedData(Context);
            var updatedRules = "{\"banList\": [\"Pot of Greed\", \"Monster Reborn\"]}";

            await _formatService.UpdateFormatRulesAsync("HAT", updatedRules);

            var format = await _formatRepository.GetByNameAsync("HAT");
            format!.Rules.ShouldBe(updatedRules);
        }













        [Fact]
        public async Task Season_CanCreateANewSeasonForAFormat()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");

            await _seasonService.CreateAsync(format!.Id, 5, 4);

            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(5, format.Id);
            season.ShouldNotBeNull();
            season.MinimumTeamMembers.ShouldBe(4);
        }

        [Fact]
        public async Task Season_CannotCreateTwoSeasonsWithTheSameNumberInTheSameFormat()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");

            var result = await _seasonService.CreateAsync(format!.Id, 1, 3);

            result.ShouldBeNull();
        }

        [Fact]
        public async Task Season_NewlyCreatedSeasonsShouldBeInactive()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("GOAT");

            var season = await _seasonService.CreateAsync(format!.Id, 2, 3);

            season!.Active.ShouldBeFalse();
        }

        [Fact]
        public async Task Season_NewlyCreatedSeasonsShouldAllowTeamModificationsByDefault()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("Edison");

            var season = await _seasonService.CreateAsync(format!.Id, 1, 3);

            season!.DisableTeamModification.ShouldBeFalse();
        }

        [Fact]
        public async Task Season_WhenTeamsArePresent_CanNotDeleteSeason()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");

            // assert that below call should throw exception because there are teams present in the season
             await Should.ThrowAsync<Exception>(async () =>
             {
                 await _seasonService.DeleteAsync(format!.Id, format.Seasons.First().SeasonNumber);
             });
        }

        [Fact]
        public async Task Season_CanSetASeasonAsActive()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            await _seasonService.CreateAsync(format!.Id, 2, 3);

            await _seasonService.SetActiveAsync(format.Id, 2);

            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(2, format.Id);
            season!.Active.ShouldBeTrue();
        }


        [Fact]
        public async Task Season_CanDisableTeamModificationsForASeason()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);

            await _seasonService.SetTeamModificationsAsync(season!.Id, enabled: false);

            var updatedSeason = await _seasonRepository.GetByIdOrDefault(season.Id);
            updatedSeason!.DisableTeamModification.ShouldBeTrue();
        }

        [Fact]
        public async Task Season_CanEnableTeamModificationsForASeason()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            await _seasonService.SetTeamModificationsAsync(season!.Id, enabled: false);

            await _seasonService.SetTeamModificationsAsync(season.Id, enabled: true);

            var updatedSeason = await _seasonRepository.GetByIdOrDefault(season.Id);
            updatedSeason!.DisableTeamModification.ShouldBeFalse();
        }

        [Fact]
        public async Task Season_WhenTeamModificationsAreDisabled_CannotAddMembersToTeam()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            await _seasonService.SetTeamModificationsAsync(season!.Id, enabled: false);

            // assert that below call should throw exception because team modifications are disabled for the season
             await Should.ThrowAsync<Exception>(async () =>
             {
                 await _teamService.AddMemberAsync(season.Id, 1, "Team Alpha");
             });
        }









        [Fact]
        public async Task Week_CanCreateANewWeek()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            var subCloseDate = DateTime.Parse("2025-01-05");

            var week = await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, subCloseDate);

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
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(season!.Id, 1, startDate, endDate, null);

            var result = await _weekService.CreateAsync(season.Id, 1, startDate, endDate, null);

            result.ShouldBeNull();
        }

        [Fact]
        public async Task Week_NewlyCreatedWeeksShouldHaveNotOpenYetStatus()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");

            var week = await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);

            week!.Status.ShouldBe(WeekStatus.NotOpenYet);
        }

        [Fact]
        public async Task Week_CanDeleteAWeek()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);

            var deletedWeek = await _weekService.DeleteAsync(season.Id, 5);

            deletedWeek.ShouldNotBeNull();
            var checkWeek = await _weekRepository.GetByWeekNumberAndSeasonAsync(5, season.Id);
            checkWeek.ShouldBeNull();
        }

        [Fact]
        public async Task Week_CanUpdateWeekDates()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);
            var newStartDate = DateTime.Parse("2025-02-01");
            var newEndDate = DateTime.Parse("2025-02-07");

            var updatedWeek = await _weekService.UpdateAsync(season.Id, 5, newStartDate, newEndDate, null, null);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.StartDate.ShouldBe(newStartDate);
            updatedWeek.EndDate.ShouldBe(newEndDate);
        }

        [Fact]
        public async Task Week_CanUpdateWeekStatus()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);

            var updatedWeek = await _weekService.UpdateAsync(season.Id, 5, null, null, null, WeekStatus.Open);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.Open);
        }

        [Fact]
        public async Task Week_CanHaveMultipleWeeksInNotOpenYetStatus()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);
            await _weekService.CreateAsync(season.Id, 6, startDate, endDate, null);

            var weeks = await _weekRepository.GetBySeasonAsync(season.Id);
            var notOpenYetWeeks = weeks.Where(w => w.Status == WeekStatus.NotOpenYet).ToList();

            notOpenYetWeeks.Count.ShouldBeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task Week_CanHaveMultipleWeeksInCompletedStatus()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);
            await _weekService.CreateAsync(season.Id, 6, startDate, endDate, null);
            await _weekService.UpdateAsync(season.Id, 5, null, null, null, WeekStatus.Completed);
            await _weekService.UpdateAsync(season.Id, 6, null, null, null, WeekStatus.Completed);

            var weeks = await _weekRepository.GetBySeasonAsync(season.Id);
            var completedWeeks = weeks.Where(w => w.Status == WeekStatus.Completed).ToList();

            completedWeeks.Count.ShouldBeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task Week_CanTransitionFromNotOpenYetToOpen()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            var week = await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);
            week!.Status.ShouldBe(WeekStatus.NotOpenYet);

            var updatedWeek = await _weekService.UpdateAsync(season.Id, 5, null, null, null, WeekStatus.Open);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.Open);
        }

        [Fact]
        public async Task Week_CanTransitionFromOpenToSubmissionsClosed()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);
            await _weekService.UpdateAsync(season.Id, 5, null, null, null, WeekStatus.Open);

            var updatedWeek = await _weekService.UpdateAsync(season.Id, 5, null, null, null, WeekStatus.SubmissionsClosed);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.SubmissionsClosed);
        }

        [Fact]
        public async Task Week_CanTransitionFromSubmissionsClosedToInProgress()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);
            await _weekService.UpdateAsync(season.Id, 5, null, null, null, WeekStatus.SubmissionsClosed);

            var updatedWeek = await _weekService.UpdateAsync(season.Id, 5, null, null, null, WeekStatus.InProgress);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.InProgress);
        }

        [Fact]
        public async Task Week_CanTransitionFromInProgressToCompleted()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(season!.Id, 5, startDate, endDate, null);
            await _weekService.UpdateAsync(season.Id, 5, null, null, null, WeekStatus.InProgress);

            var updatedWeek = await _weekService.UpdateAsync(season.Id, 5, null, null, null, WeekStatus.Completed);

            updatedWeek.ShouldNotBeNull();
            updatedWeek.Status.ShouldBe(WeekStatus.Completed);
        }

        [Fact]
        public async Task Week_CloseSubmissionsReturnsErrorWhenNoOpenWeekExists()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);

            var result = await _weekService.CloseSubmissionsAsync(season!.Id);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("No open week");
        }

        [Fact]
        public async Task Week_CloseWeekReturnsErrorWhenNoInProgressWeekExists()
        {
            Seed.SeedData(Context);
            var format = await _formatRepository.GetByNameAsync("HAT");
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(1, format!.Id);

            var result = await _weekService.CloseAsync(season!.Id);

            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("No InProgress week");
        }
    }

 
}
