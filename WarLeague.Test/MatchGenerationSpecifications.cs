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
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 2, playersPerTeam: 2);

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

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
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 4, playersPerTeam: 2);

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

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
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 3, playersPerTeam: 2);

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

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
            // Arrange: no team pairings (IMatchupService returns null/empty for one team)
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsOneTeamForPairingsAsync();

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("team pairings", Case.Insensitive);
            result.Message.ShouldContain("Open the week first", Case.Insensitive);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairingsWithNoTeamMatchupsForWeek_ThenReturnsFail()
        {
            // Arrange: week never opened, so no team matchups saved (IMatchupService.GetExistingTeamMatchupsAsync returns null)
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsNoTeamMatchupsForPairingsAsync();

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("team pairings", Case.Insensitive);
            result.Message.ShouldContain("Open the week first", Case.Insensitive);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingTeamVsTeamPairings_ThenCreatesCorrectNumberOfMatches()
        {
            // Arrange: 4 teams → 2 team-vs-team pairings (round-robin via IMatchupService), each with 2 player matches
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 4, playersPerTeam: 2);

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

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
            // Arrange: conferences set before week is opened so IMatchupService (round-robin) generates per-conference pairings
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsTwoConferencesForPairingsAsync(teamsPerConference: 2, playersPerTeam: 2);
            teams.Count.ShouldBe(4);

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

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
            // Arrange: week has team matchups and submissions but matches already exist (duplicate guard)
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 2, playersPerTeam: 2);
            var p1Team1 = await GetTeamPlayerIds(seasonId, teams[0].Id);
            var p1Team2 = await GetTeamPlayerIds(seasonId, teams[1].Id);
            var pid1 = Math.Min(p1Team1[0], p1Team2[0]);
            var pid2 = Math.Max(p1Team1[0], p1Team2[0]);
            await _matchRepository.AddRangeAsync(new[]
            {
                new Match
                {
                    WeekId = week.Id,
                    Player1Id = pid1,
                    Player2Id = pid2,
                    Team1Id = teams[0].Id,
                    Team2Id = teams[1].Id,
                    Status = MatchStatus.Scheduled
                }
            });

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already exist", Case.Insensitive);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairings_ThenEachMatchHasDistinctPlayersAndCorrectWeek()
        {
            // Arrange
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 2, playersPerTeam: 2);

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

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
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 2, playersPerTeam: 2);

            // Act
            var result = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

            // Assert
            result.Success.ShouldBeTrue();
            result.WeeklyMatchups.ShouldNotBeNull();
            result.WeeklyMatchups!.Count.ShouldBe(1); // one team-vs-team pairing (from IMatchupService.GetIndividualMatchups)
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
            // Arrange: generate pairings via MatchService, set week InProgress so ReportLossAsync can find it
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 2, playersPerTeam: 2);
            var pairResult = await _matchService.GeneratePairingsAsync(seasonId, week, teams);
            pairResult.Success.ShouldBeTrue();
            await SetWeekStatusInProgress(seasonId, 1);
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
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 2, playersPerTeam: 2);
            await _matchService.GeneratePairingsAsync(seasonId, week, teams);
            await SetWeekStatusInProgress(seasonId, 1);
            var matches = await _matchRepository.GetByWeekIdAsync(week.Id);
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
            // Arrange: generate pairings, report both (same winner), set week to Completed so no InProgress week
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 2, playersPerTeam: 2);
            var pairResult = await _matchService.GeneratePairingsAsync(seasonId, week, teams);
            pairResult.Success.ShouldBeTrue();
            await SetWeekStatusInProgress(seasonId, 1);
            var matches = pairResult.CreatedMatches!;
            await _matchService.ReportResultAsync(seasonId, matches[0].Player1Id, matches[0].Player2Id, "https://example.com/r1");
            await _matchService.ReportResultAsync(seasonId, matches[1].Player1Id, matches[1].Player2Id, "https://example.com/r2");
            await SetWeekStatusCompleted(seasonId, 1);

            // Act: try to report again (no InProgress week)
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
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsForPairingsAsync(teamCount: 2, playersPerTeam: 2);
            var pairResult = await _matchService.GeneratePairingsAsync(seasonId, week, teams);
            pairResult.Success.ShouldBeTrue();
            await SetWeekStatusInProgress(seasonId, 1);
            var match = pairResult.CreatedMatches!.First();
            var loserId = match.Player1Id;
            var winnerId = match.Player2Id;

            // Act
            var result = await _matchService.ReportLossAsync(seasonId, loserId, "https://example.com/replay");

            // Assert
            result.Success.ShouldBeTrue();
            var weekMatches = await _matchRepository.GetByWeekIdAsync(week.Id);
            var updated = weekMatches.First(m => m.Id == match.Id);
            updated.ShouldNotBeNull();
            updated!.WinnerId.ShouldBe(winnerId);
            updated.Status.ShouldBe(MatchStatus.Reported);
        }

        #endregion
    }
}
