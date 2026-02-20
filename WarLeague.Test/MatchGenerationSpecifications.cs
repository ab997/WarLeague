using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.Linq;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

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
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches.ShouldNotBeNull();
            result.CreatedMatches!.Count.ShouldBe(2); // 2 teams → 1 team matchup → 2 player matches (2 players per team)
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairingsWithFourTeams_ThenCreatesCorrectNumberOfMatches()
        {
            // Arrange
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 4, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches.ShouldNotBeNull();
            result.CreatedMatches!.Count.ShouldBe(4); // 4 teams → 2 team matchups → 4 player matches
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairingsWithOddNumberOfTeams_ThenHandlesByeCorrectly()
        {
            // Arrange
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 3, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches.ShouldNotBeNull();
            result.CreatedMatches!.Count.ShouldBe(2); // 3 teams → 1 matchup + 1 bye → 2 player matches
            result.ByeTeams.ShouldNotBeNull();
            result.ByeTeams!.Count.ShouldBe(1);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairingsWithOneTeam_ThenReturnsFail()
        {
            // Arrange
            var seasonId = await CreateSeasonWithOneTeamAndSubmissionsClosedWeek();

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("at least 2 teams", Case.Insensitive);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairingsWithNoSubmissionsClosedWeek_ThenReturnsFail()
        {
            // Arrange: week is still Open (submissions not closed)
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("SubmissionsClosed", Case.Insensitive);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingTeamVsTeamPairings_ThenCreatesCorrectNumberOfMatches()
        {
            // Arrange: 4 teams → 2 team-vs-team pairings (round-robin), each with 2 player matches
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 4, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.WeeklyMatchups.ShouldNotBeNull();
            result.WeeklyMatchups!.Count.ShouldBe(2); // 2 team pairings
            result.CreatedMatches!.Count.ShouldBe(4); // 2 matches per pairing
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairingsWithConferences_ThenOnlyPairsTeamsWithinSameConference()
        {
            // Arrange: conferences set before week is opened so round-robin generates per-conference pairings
            var (seasonId, _) = await CreateSeasonWithTwoConferencesAndSubmissions(teamsPerConference: 2, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            var teams = await _context.Teams
                .Where(t => t.SeasonId == seasonId)
                .OrderBy(t => t.Id)
                .ToListAsync();
            teams.Count.ShouldBe(4);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches.ShouldNotBeNull();
            result.CreatedMatches!.Count.ShouldBe(4);

            var conferenceIdByTeamId = teams.ToDictionary(t => t.Id, t => t.ConferenceId);
            var hasCrossConferenceMatch = result.CreatedMatches!.Any(m =>
                conferenceIdByTeamId[m.Team1Id] != conferenceIdByTeamId[m.Team2Id]);

            hasCrossConferenceMatch.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairingsWhenMatchesAlreadyExistForWeek_ThenReturnsFail()
        {
            // Arrange: week in SubmissionsClosed but matches already exist (e.g. duplicate or manual insert)
            var (seasonId, weekId) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            await CloseSubmissions(seasonId);
            var teams = await _context.Teams.Where(t => t.SeasonId == seasonId).OrderBy(t => t.Id).ToListAsync();
            var p1Team1 = await GetTeamPlayerIds(seasonId, teams[0].Id);
            var p1Team2 = await GetTeamPlayerIds(seasonId, teams[1].Id);
            var pid1 = Math.Min(p1Team1[0], p1Team2[0]);
            var pid2 = Math.Max(p1Team1[0], p1Team2[0]);
            _context.Matches.Add(new Match
            {
                WeekId = weekId,
                Player1Id = pid1,
                Player2Id = pid2,
                Team1Id = teams[0].Id,
                Team2Id = teams[1].Id,
                Status = MatchStatus.Scheduled
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already exist", Case.Insensitive);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairings_ThenEachMatchHasDistinctPlayersAndCorrectWeek()
        {
            // Arrange
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.CreatedMatches.ShouldNotBeNull();
            result.Week.ShouldNotBeNull();
            foreach (var m in result.CreatedMatches!)
            {
                m.Player1Id.ShouldNotBe(m.Player2Id);
                m.WeekId.ShouldBe(result.Week!.Id);
                m.Team1Id.ShouldNotBe(0);
                m.Team2Id.ShouldNotBe(0);
            }
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairings_ThenWeeklyMatchupsReflectTeamPairings()
        {
            // Arrange
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            await CloseSubmissions(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
            result.WeeklyMatchups.ShouldNotBeNull();
            result.WeeklyMatchups!.Count.ShouldBe(1); // one team-vs-team pairing
            var mu = result.WeeklyMatchups[0];
            mu.TeamA.Id.ShouldNotBe(mu.TeamB.Id);
            mu.Pairs.Count.ShouldBe(2); // 2 players per team
        }

        #endregion

        #region Match reporting (MatchService) behaviour

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenReportingLoss_WithInvalidReplayUrl_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            await CloseSubmissions(seasonId);
            var pairResult = await _weekService.TransitionToInProgressAsync(seasonId);
            pairResult.Success.ShouldBeTrue();
            var loserId = pairResult.CreatedMatches!.First().Player1Id;

            // Act
            var result = await _matchService.ReportLossAsync(seasonId, loserId, "not-a-valid-url");

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("valid", Case.Insensitive);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenReportingResult_WithWinnerSameAsLoser_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            await CloseSubmissions(seasonId);
            await _weekService.TransitionToInProgressAsync(seasonId);
            var matches = await _context.Matches.Where(m => m.Week!.SeasonId == seasonId).ToListAsync();
            var playerId = matches.First().Player1Id;

            // Act
            var result = await _matchService.ReportResultAsync(seasonId, playerId, playerId, "https://example.com/replay");

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("different", Case.Insensitive);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenReportingResult_WithNoInProgressWeek_ThenReturnsFail()
        {
            // Arrange: report all matches (one team wins 2-0 so week can complete) and complete the week
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            await CloseSubmissions(seasonId);
            var pairResult = await _weekService.TransitionToInProgressAsync(seasonId);
            pairResult.Success.ShouldBeTrue();
            var matches = pairResult.CreatedMatches!;
            // Same team wins both matches so there is no tie and week can transition to Completed
            await _matchService.ReportResultAsync(seasonId, matches[0].Player1Id, matches[0].Player2Id, "https://example.com/r1");
            await _matchService.ReportResultAsync(seasonId, matches[1].Player1Id, matches[1].Player2Id, "https://example.com/r2");
            var closeResult = await _weekService.TransitionToCompletedAsync(seasonId);
            closeResult.Success.ShouldBeTrue(); // now no InProgress week

            // Act: try to report again (e.g. mistaken command)
            var result = await _matchService.ReportResultAsync(seasonId, matches[0].Player1Id, matches[0].Player2Id, "https://example.com/r");

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("InProgress", Case.Insensitive);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenReportingLoss_ThenMatchMovesToReportedAndWinnerSet()
        {
            // Arrange
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount: 2, playersPerTeam: 2);
            await CloseSubmissions(seasonId);
            var pairResult = await _weekService.TransitionToInProgressAsync(seasonId);
            pairResult.Success.ShouldBeTrue();
            var match = pairResult.CreatedMatches!.First();
            var loserId = match.Player1Id;
            var winnerId = match.Player2Id;

            // Act
            var result = await _matchService.ReportLossAsync(seasonId, loserId, "https://example.com/replay");

            // Assert
            result.Success.ShouldBeTrue();
            var updated = await _context.Matches.FindAsync(match.Id);
            updated.ShouldNotBeNull();
            updated!.WinnerId.ShouldBe(winnerId);
            updated.Status.ShouldBe(MatchStatus.Reported);
        }

        #endregion
    }
}
