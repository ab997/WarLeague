using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Test
{
    public partial class Specifications
    {
        #region Format Behavior Specifications

        [Fact]
        [Trait("Category", "Format")]
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
        [Trait("Category", "Format")]
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
        [Trait("Category", "Format")]
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
        [Trait("Category", "Format")]
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
    }
}
