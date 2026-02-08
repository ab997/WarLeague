using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Test
{
    public partial class Specifications
    {
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
            // TODO: handle bye with BYE table

            // Arrange
            var (seasonId, weekId) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 3, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches!.Count.ShouldBe(2);
            // TODO: assert bye table
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
        }

        #endregion
    }
}
