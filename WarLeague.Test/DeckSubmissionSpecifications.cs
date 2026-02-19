using Microsoft.EntityFrameworkCore;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data.Enums;

namespace WarLeague.Test
{
    public partial class Specifications
    {
        #region Deck Submission Behavior Specifications

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenSubmittingFirstDeck_ThenReturnsSuccess()
        {
            // Arrange
            var (seasonId, playerId, _, _) = await CreateSeasonWithTeamAndOpenWeek();

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck content", seatNumber: 1);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenResubmittingDeck_ThenUpdatesExistingSubmission()
        {
            // Arrange
            var (seasonId, playerId, _, _) = await CreateSeasonWithTeamAndOpenWeek();
            await _deckSubmissionService.SubmitAsync(seasonId, playerId, "original deck", 1);

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, playerId, "updated deck", 2);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenSubmittingToOccupiedSeatOnSameTeam_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, player1Id, _, cpt1Id) = await CreateSeasonWithTeamAndOpenWeek();
            await _deckSubmissionService.SubmitAsync(seasonId, player1Id, "deck1", seatNumber: 1);

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, cpt1Id, "deck2", seatNumber: 1);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenSubmittingToSameSeatOnDifferentTeams_ThenBothSucceed()
        {
            // Arrange
            var (seasonId, player1Id, player2Id, _) = await CreateSeasonWithTeamAndOpenWeek();

            // Act
            var result1 = await _deckSubmissionService.SubmitAsync(seasonId, player1Id, "deck1", 1);
            var result2 = await _deckSubmissionService.SubmitAsync(seasonId, player2Id, "deck2", 1);

            // Assert
            result1.Success.ShouldBeTrue();
            result2.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenSubmittingWithInvalidSeatNumber_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, playerId, _, _) = await CreateSeasonWithTeamAndOpenWeek(submissionsRequired: 3);

            // Act
            var result0 = await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck", seatNumber: 0);
            var result4 = await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck", seatNumber: 4);

            // Assert
            result0.Success.ShouldBeFalse();
            result4.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenSubmittingWithNoOpenWeek_ThenThrowException()
        {
            // Arrange
            var (formatId, seasonId) = await CreateFormatAndSeason();
            var (playerId, _) = await CreateTeamWithPlayer(seasonId, "Team1");
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);

            // Act Assert
            Should.Throw<Exception>(async () => await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck", 1));
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenSubmittingAsPlayerNotOnTeam_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, _, _, _) = await CreateSeasonWithTeamAndOpenWeek();
            var unassignedPlayer = await CreatePlayer(999999);

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, unassignedPlayer.Id, "deck", 1);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenDeletingExistingSubmission_ThenReturnsSuccess()
        {
            // Arrange
            var (seasonId, playerId, _, _) = await CreateSeasonWithTeamAndOpenWeek();
            await _deckSubmissionService.SubmitAsync(seasonId, playerId, "deck", 1);

            // Act
            var result = await _deckSubmissionService.DeleteSubmissionAsync(seasonId, playerId);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenDeletingNonExistentSubmission_ThenReturnsFail()
        {
            // Arrange
            var (seasonId, playerId, _, _) = await CreateSeasonWithTeamAndOpenWeek();

            // Act
            var result = await _deckSubmissionService.DeleteSubmissionAsync(seasonId, playerId);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenDeletingSubmissionWithMultipleWeeks_ThenDeletesFromOpenWeekOnly()
        {
            // Arrange - Create season with team and players
            var (_, seasonId) = await CreateFormatAndSeason();
            var (player1, player2, _) = await CreateTwoPlayersOnSameTeam(seasonId, "Team1");
            var (player3, player4, _) = await CreateTwoPlayersOnSameTeam(seasonId, "Team2");

            // Week 1: Create, submit, and close
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 1);
            await _weekService.TransitionToOpenWeekAsync(seasonId, 1);
            await _deckSubmissionService.SubmitAsync(seasonId, player1.Id, "week1 deck", 1);
            await _weekService.UpdateAsync(seasonId, 1, null, null, null, WeekStatus.Completed, 2);

            // Week 2: Create, make open, and submit
            await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(14), null, 1);
            await _weekService.TransitionToOpenWeekAsync(seasonId, 2);
            await _deckSubmissionService.SubmitAsync(seasonId, player1.Id, "week2 deck", 1);

            // Act - Delete submission
            var result = await _deckSubmissionService.DeleteSubmissionAsync(seasonId, player1.Id);

            // Assert - Verify deletion was successful
            result.Success.ShouldBeTrue();

            // Verify week 1 submission still exists
            var week1 = await _context.Weeks.FirstAsync(w => w.SeasonId == seasonId && w.WeekNumber == 1);
            var week1Submission = await _context.DeckSubmissions
                .FirstOrDefaultAsync(ds => ds.WeekId == week1.Id && ds.PlayerId == player1.Id);
            week1Submission.ShouldNotBeNull();
            week1Submission.DeckFile.ShouldBe("week1 deck");

            // Verify week 2 submission was deleted
            var week2 = await _context.Weeks.FirstAsync(w => w.SeasonId == seasonId && w.WeekNumber == 2);
            var week2Submission = await _context.DeckSubmissions
                .FirstOrDefaultAsync(ds => ds.WeekId == week2.Id && ds.PlayerId == player1.Id);
            week2Submission.ShouldBeNull();
        }

        [Fact]
        [Trait("Category", "DeckSubmission")]
        public async Task WhenDeletingSubmission_ThenFreesSeatForOtherPlayers()
        {
            // Arrange
            var (seasonId, player1Id, player2Id, _) = await CreateSeasonWithTeamAndOpenWeek();
            await _deckSubmissionService.SubmitAsync(seasonId, player1Id, "deck1", 1);
            await _deckSubmissionService.DeleteSubmissionAsync(seasonId, player1Id);

            // Act
            var result = await _deckSubmissionService.SubmitAsync(seasonId, player2Id, "deck2", 1);

            // Assert
            result.Success.ShouldBeTrue();
        }

        #endregion

    }
}
