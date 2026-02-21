using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Model;

namespace WarLeague.Test
{
    public partial class Specifications
    {
        #region Season Behavior Specifications

        [Fact]
        [Trait("Category", "Season")]
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
        [Trait("Category", "Season")]
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
        [Trait("Category", "Season")]
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
        [Trait("Category", "Season")]
        public async Task WhenDisablingTeamModifications_ThenReturnsSuccess()
        {
            // Arrange
            var (formatId, seasonId) = await CreateFormatAndSeason();

            // Act
            SeasonResult result = await _seasonService.SetTeamModificationsAsync(seasonId, enabled: false);

            // Assert
            result.Success.ShouldBeTrue();
            result.Season!.DisableTeamModification.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "Season")]
        public async Task WhenEnablingTeamModifications_ThenReturnsSuccess()
        {
            // Arrange
            var (formatId, seasonId) = await CreateFormatAndSeason();

            // Act
            var result = await _seasonService.SetTeamModificationsAsync(seasonId, enabled: true);

            // Assert
            result.Success.ShouldBeTrue();
            result.Season!.DisableTeamModification.ShouldBeFalse();
        }

        #endregion
    }
}
