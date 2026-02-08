using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Services;

namespace WarLeague.Test
{
    public partial class Specifications
    {
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
        }

        #endregion
    }
}
