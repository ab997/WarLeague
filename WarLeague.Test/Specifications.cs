using Microsoft.EntityFrameworkCore;
using Shouldly;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;

namespace WarLeague.Test
{
    

    /// <summary>
    /// Format Management Specifications
    /// These tests describe what users should expect when managing formats in the War League system.
    /// </summary>
    public class FormatManagementSpecifications : TransactionalTestBase
    {
        private readonly FormatService _formatService;
        private readonly FormatRepository _formatRepository;
        private readonly SeasonService _seasonService;
        private readonly SeasonRepository _seasonRepository;
        public FormatManagementSpecifications(DatabaseFixtureSeeded fixture) : base(fixture)
        {
            _formatRepository = new FormatRepository(Context);
            _formatService = new FormatService(_formatRepository, Context);
            _seasonRepository = new SeasonRepository(Context);
            _seasonService = new SeasonService(_seasonRepository, _formatRepository);
        }

        [Fact]
        public async Task Format_CanCreateANewFormat()
        {
            var formatName = "Speed Duel";

            await _formatService.CreateFormatAsync(formatName);

            var createdFormat = await _formatRepository.GetByNameAsync(formatName);
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

            var deletedFormat = await _formatRepository.GetByNameAsync("GOAT");
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
    }

 
}
