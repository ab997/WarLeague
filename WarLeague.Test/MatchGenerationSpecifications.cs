using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.Linq;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
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
            // Arrange: week has no team matchups yet (week not opened)
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsNoTeamMatchupsForPairingsAsync(teamCount: 4, playersPerTeam: 2);

            // Act: ensure team-vs-team pairings for the week, then generate player matches
            var ensureResult = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week, teams);
            var pairingsResult = await _matchService.GeneratePairingsAsync(seasonId, week, teams);

            // Assert
            ensureResult.Success.ShouldBeTrue();
            pairingsResult.Success.ShouldBeTrue();
            pairingsResult.CreatedMatches.ShouldNotBeNull();
            // 4 teams → 2 team matchups → 4 player matches (2 players per team)
            pairingsResult.CreatedMatches!.Count.ShouldBe(4);
            pairingsResult.WeeklyMatchups.ShouldNotBeNull();
            pairingsResult.WeeklyMatchups!.Count.ShouldBe(2);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenEnsureTeamMatchupsForWeek_AndNoExistingMatchups_ThenSavesRoundRobinMatchups()
        {
            // Arrange: week has no team matchups (round-robin phase)
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsNoTeamMatchupsForPairingsAsync(teamCount: 4, playersPerTeam: 2);

            // Act
            var result = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week, teams);

            // Assert
            result.Success.ShouldBeTrue();
            var roundRobinMatchups = _context.RoundRobinMatchups.Where(rm => rm.WeekId == week.Id).ToList();
            roundRobinMatchups.Count.ShouldBe(2); // 4 teams → 2 matchups
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenEnsureTeamMatchupsForWeek_AndMatchupsAlreadyExist_ThenReturnsSuccessWithoutDuplicateSaves()
        {
            // Arrange: ensure once to create round-robin matchups
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsNoTeamMatchupsForPairingsAsync(teamCount: 4, playersPerTeam: 2);
            var firstResult = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week, teams);
            firstResult.Success.ShouldBeTrue();

            // Act: ensure again (e.g. idempotent call)
            var secondResult = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week, teams);

            // Assert
            secondResult.Success.ShouldBeTrue();
            secondResult.Message.ShouldContain("already exist", Case.Insensitive);
            var roundRobinMatchups = _context.RoundRobinMatchups.Where(rm => rm.WeekId == week.Id).ToList();
            roundRobinMatchups.Count.ShouldBe(2); // still only 2, no duplicates
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenEnsureTeamMatchupsForWeek_AndRoundRobinWithSixTeams_ThenSavesThreeMatchups()
        {
            // Arrange: 6 teams in one conference, week not opened
            var (seasonId, week, teams) = await GetSeasonWeekAndTeamsNoTeamMatchupsForPairingsAsync(teamCount: 6, playersPerTeam: 2);
            teams.Count.ShouldBe(6);

            // Act
            var result = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week, teams);

            // Assert
            result.Success.ShouldBeTrue();
            var roundRobinMatchups = _context.RoundRobinMatchups.Where(rm => rm.WeekId == week.Id).ToList();
            roundRobinMatchups.Count.ShouldBe(3); // 6 teams → 3 matchups
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenEnsureTeamMatchupsForWeek_AndPlayoffsPhase_ThenUsesPlayoffServiceAndSavesBracketMatchups()
        {
            // Arrange: season in Playoffs phase, week 1 completed with round-robin winners, week 2 is first playoff week
            var (seasonId, week2, teams) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekAsync(teamsPerConference: 2, playersPerTeam: 2);
            var season = await _seasonRepository.GetById(seasonId);
            season.Phase.ShouldBe(SeasonPhase.Playoffs);
            teams.Count.ShouldBe(4);

            // Act: EnsureTeamMatchupsForWeekAsync resolves to PlayoffService via MatchupServiceFactory
            var result = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week2, teams);

            // Assert
            result.Success.ShouldBeTrue();
            var playoffMatchups = _context.PlayoffMatchups.Where(pm => pm.WeekId == week2.Id).ToList();
            playoffMatchups.Count.ShouldBe(1); // 2 conferences × 1 playoff team each → 1 bracket matchup
            _context.RoundRobinMatchups.Count(rm => rm.WeekId == week2.Id).ShouldBe(0); // PlayoffService, not RoundRobin
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenEnsureTeamMatchupsForWeek_AndPlayoffsPhaseWithFourPlayoffTeams_ThenSavesTwoSemifinalMatchups()
        {
            // Arrange: 4 teams in one conference, week 1 completed with standings, then switch to playoffs
            var (seasonId, week2, teams) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekSingleConferenceAsync(playoffTeamCount: 4, playersPerTeam: 2);
            teams.Count.ShouldBe(4);

            // Act
            var result = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week2, teams);

            // Assert: 4 teams in single-elimination = 2 semifinal matchups, no BYEs
            result.Success.ShouldBeTrue();
            var playoffMatchups = _context.PlayoffMatchups.Where(pm => pm.WeekId == week2.Id).ToList();
            playoffMatchups.Count.ShouldBe(2);
            var normalMatchups = playoffMatchups.Where(pm => pm.MatchupType == MatchupType.Normal).ToList();
            normalMatchups.Count.ShouldBe(2);
            playoffMatchups.Count(pm => pm.MatchupType == MatchupType.Bye).ShouldBe(0);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenEnsureTeamMatchupsForWeek_AndPlayoffsPhaseWithFivePlayoffTeams_ThenSavesOneNormalMatchupAndThreeByes()
        {
            // Arrange: 5 teams in one conference, week 1 completed, then switch to playoffs
            var (seasonId, week2, teams) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekSingleConferenceAsync(playoffTeamCount: 5, playersPerTeam: 2);
            teams.Count.ShouldBe(5);

            // Act
            var result = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week2, teams);

            // Assert: 5 teams → next power of 2 is 8 → 3 byes + 1 normal matchup (teams 4 vs 5)
            result.Success.ShouldBeTrue();
            var playoffMatchups = _context.PlayoffMatchups.Where(pm => pm.WeekId == week2.Id).ToList();
            playoffMatchups.Count.ShouldBe(4);
            var normalMatchups = playoffMatchups.Where(pm => pm.MatchupType == MatchupType.Normal).ToList();
            normalMatchups.Count.ShouldBe(1);
            playoffMatchups.Count(pm => pm.MatchupType == MatchupType.Bye).ShouldBe(3);
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenPlayoffsWithFiveTeams_SimulateSingleEliminationToFinals_ThenEveryStageHasCorrectBracket()
        {
            // Arrange: same as five-team playoffs (round 1 = top-8 bracket: 3 byes + 1 matchup)
            var (seasonId, week2, teams) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekSingleConferenceAsync(playoffTeamCount: 5, playersPerTeam: 2);
            teams.Count.ShouldBe(5);
            var teamOrder = teams.OrderBy(t => t.Id).ToList(); // Team1=seed1 .. Team5=seed5

            // ——— Stage 1: Top 8 (Round 1) ———
            var ensure1 = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week2, teams);
            ensure1.Success.ShouldBeTrue();
            var round1Matchups = _context.PlayoffMatchups.Where(pm => pm.WeekId == week2.Id).OrderBy(pm => pm.BracketPosition).ToList();
            round1Matchups.Count.ShouldBe(4, "Round 1 should have 4 bracket slots (3 byes + 1 game)");
            round1Matchups.Count(pm => pm.MatchupType == MatchupType.Bye).ShouldBe(3);
            round1Matchups.Count(pm => pm.MatchupType == MatchupType.Normal).ShouldBe(1);
            round1Matchups.All(pm => pm.Round == 1).ShouldBeTrue();

            await CompletePlayoffWeekAsync(seasonId, 2, teams, loserTeamIdsPerMatchup: new[] { teamOrder[4].Id }); // Team5 loses in the only game

            // ——— Stage 2: Top 4 (Semifinals) ———
            await CreateWeekAsync(seasonId, 3, 2);
            var week3 = await _weekRepository.GetByWeekNumberAndSeasonAsync(3, seasonId);
            week3.ShouldNotBeNull();
            var ensure2 = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week3!, teams);
            ensure2.Success.ShouldBeTrue();
            var round2Matchups = _context.PlayoffMatchups.Where(pm => pm.WeekId == week3!.Id).OrderBy(pm => pm.BracketPosition).ToList();
            round2Matchups.Count.ShouldBe(2, "Semifinals should have 2 matchups");
            round2Matchups.Count(pm => pm.MatchupType == MatchupType.Bye).ShouldBe(0);
            round2Matchups.Count(pm => pm.MatchupType == MatchupType.Normal).ShouldBe(2);
            round2Matchups.Select(pm => pm.Round).Distinct().Count().ShouldBe(1, "Semifinals should have a single round number");
            round2Matchups.All(pm => pm.Team1Id != pm.Team2Id).ShouldBeTrue("All semifinal matchups should be team vs team (no byes)");

            // Semifinals: Team3 and Team4 lose so Team1 and Team2 advance to finals
            await CompletePlayoffWeekAsync(seasonId, 3, teams, loserTeamIdsPerMatchup: new[] { teamOrder[3].Id, teamOrder[2].Id });

            // ——— Stage 3: Top 2 (Finals) ———
            await CreateWeekAsync(seasonId, 4, 2);
            var week4 = await _weekRepository.GetByWeekNumberAndSeasonAsync(4, seasonId);
            week4.ShouldNotBeNull();
            var ensure3 = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week4!, teams);
            ensure3.Success.ShouldBeTrue();
            var round3Matchups = _context.PlayoffMatchups.Where(pm => pm.WeekId == week4!.Id).OrderBy(pm => pm.BracketPosition).ToList();
            round3Matchups.Count.ShouldBe(1, "Finals should have 1 matchup");
            round3Matchups[0].MatchupType.ShouldBe(MatchupType.Normal);
            round3Matchups[0].Team1Id.ShouldNotBe(round3Matchups[0].Team2Id, "Finals should be one team vs another");
        }

        [Fact]
        [Trait("Category", "MatchGeneration")]
        public async Task WhenGeneratingPairings_AndPlayoffsPhase_ThenCreatesMatchesFromPlayoffMatchups()
        {
            // Arrange: playoffs first week, ensure team matchups then add deck submissions for playoff teams and generate pairings
            var (seasonId, week2, teams) = await GetSeasonWeekAndTeamsForPlayoffsFirstWeekAsync(teamsPerConference: 2, playersPerTeam: 2);
            var ensureResult = await _matchService.EnsureTeamMatchupsForWeekAsync(seasonId, week2, teams);
            ensureResult.Success.ShouldBeTrue();
            var playoffMatchups = _context.PlayoffMatchups.Where(pm => pm.WeekId == week2.Id).ToList();
            var playingTeamIds = playoffMatchups.Where(pm => pm.MatchupType == MatchupType.Normal).SelectMany(pm => new[] { pm.Team1Id, pm.Team2Id }).Distinct().ToList();
            foreach (var teamId in playingTeamIds)
            {
                var playerIds = await GetTeamPlayerIds(seasonId, teamId);
                for (int seat = 1; seat <= playerIds.Count; seat++)
                    await AddDeckSubmissionForWeekAsync(seasonId, 2, playerIds[seat - 1], seat);
            }
            var week2WithSubmissions = await GetWeekWithSubmissionsAsync(seasonId, 2);

            // Act: GeneratePairingsAsync uses PlayoffService when season.Phase is Playoffs
            var pairingsResult = await _matchService.GeneratePairingsAsync(seasonId, week2WithSubmissions!, teams);

            // Assert
            pairingsResult.Success.ShouldBeTrue();
            pairingsResult.CreatedMatches.ShouldNotBeNull();
            // 1 team matchup; each team has captain + playersPerTeam members = 3, so 3 player pairs
            pairingsResult.CreatedMatches!.Count.ShouldBe(3);
            pairingsResult.WeeklyMatchups.ShouldNotBeNull();
            pairingsResult.WeeklyMatchups!.Count.ShouldBe(1);
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
