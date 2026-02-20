using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;

namespace WarLeague.Test
{
    public partial class Specifications
    {
        #region Match Generation Behavior Specifications

        [Fact]
        [Trait("Category", "MatchGeneration")]
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
        [Trait("Category", "MatchGeneration")]
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
        [Trait("Category", "MatchGeneration")]
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
        [Trait("Category", "MatchGeneration")]
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
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairingsWithNoSubmissionsClosedWeek_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, weekId) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairings_ThenRoundRobinMatchupsAreSaved()
        {
            // Arrange
            var (seasonId, weekId) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 4, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();

            var roundRobinMatchups = _context.RoundRobinMatchups
                .Where(m => m.WeekId == weekId)
                .ToList();

            roundRobinMatchups.Count.ShouldBe(2);
            roundRobinMatchups.All(m => m.MatchupType == MatchupType.Normal).ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairingsWithConferences_ThenOnlyPairsTeamsWithinSameConference()
        {
            // Arrange
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 4, playersPerTeam: 2);

            var teams = _context.Teams
                .Where(t => t.SeasonId == seasonId)
                .OrderBy(t => t.Id)
                .ToList();

            teams.Count.ShouldBe(4);

            var alphaConference = new Conference { SeasonId = seasonId, Name = "Alpha" };
            var betaConference = new Conference { SeasonId = seasonId, Name = "Beta" };

            _context.Conferences.Add(alphaConference);
            _context.Conferences.Add(betaConference);
            await _context.SaveChangesAsync();

            teams[0].ConferenceId = alphaConference.Id;
            teams[1].ConferenceId = alphaConference.Id;
            teams[2].ConferenceId = betaConference.Id;
            teams[3].ConferenceId = betaConference.Id;
            await _context.SaveChangesAsync();

            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches.ShouldNotBeNull();
            result.CreatedMatches!.Count.ShouldBe(4);

            var conferenceIdByTeamId = teams.ToDictionary(t => t.Id, t => t.ConferenceId);
            var hasCrossConferenceMatch = result.CreatedMatches.Any(m =>
                conferenceIdByTeamId[m.Team1Id] != conferenceIdByTeamId[m.Team2Id]);

            hasCrossConferenceMatch.ShouldBeFalse();
        }

        #endregion
    }
}
