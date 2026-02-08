using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data.Enums;

namespace WarLeague.Test
{
    public partial class Specifications
    {
        #region Week Behavior Specifications

        [Fact]
        public async Task WhenCreatingWeekWithValidParameters_ThenReturnsSuccess()
        {
            // Arrange
            var (formatId, seasonId) = await CreateFormatAndSeason();
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");

            // Act
            var result = await _weekService.CreateAsync(seasonId, weekNumber: 1, startDate, endDate, null, submissionsRequired: 3);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenCreatingDuplicateWeekNumber_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            var startDate = DateTime.Parse("2025-01-01");
            var endDate = DateTime.Parse("2025-01-07");
            await _weekService.CreateAsync(seasonId, 1, startDate, endDate, null, 3);

            // Act
            var result = await _weekService.CreateAsync(seasonId, 1, startDate.AddDays(7), endDate.AddDays(7), null, 3);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("already exists", Case.Insensitive);
        }

        //________________________________________________________________________
        // WEEK LIFECYCLE TRANSITIONS - Each transition has tests for both valid and invalid scenarios
        //________________________________________________________________________

        //________________________________________________________________________
        // NotOpenYet -> Open
        //________________________________________________________________________

        [Fact]
        public async Task WhenOpeningWeek_InStatusNotOpenYet_ThenReturnsSuccess()
        {
            // Arrange
            int weekNumber = 1;
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, weekNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);

            // Act
            var result = await _weekService.TransitionToOpenWeekAsync(seasonId, weekNumber);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenOpeningWeek_NotInStatusNotOpenYet_ThenReturnsFail()
        {
            // Arrange
            int weekNumber = 1;
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, weekNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);
            await _weekService.UpdateAsync(seasonId, weekNumber, null, null, null, WeekStatus.InProgress, 3);

            // Act
            var result = await _weekService.TransitionToOpenWeekAsync(seasonId, weekNumber);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenOpeningWeek_WithOpenWeekAlreadyExists_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);
            await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);
            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.Open, 3);

            // Act
            var result = await _weekService.TransitionToOpenWeekAsync(seasonId, 1);

            // Assert
            result.Success.ShouldBeFalse();
        }

        //________________________________________________________________________
        // Open -> CloseSubmissions
        //________________________________________________________________________

        [Fact]
        public async Task WhenClosingSubmissions_WithNoOpenWeek_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();

            // Act
            var result = await _weekService.TransitionToCloseSubmissionsAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }
        [Fact]
        public async Task WhenClosingSubmissions_WithOpenWeek_ThenReturnsSuccess()
        {
            // Arrange
            int seasonId = await PrepareWeek_ReadyForClosingSubmissions();

            // Act
            var result = await _weekService.TransitionToCloseSubmissionsAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
        }



        [Fact]
        public async Task WhenClosingSubmissions_WithSubmissionClosedWeekAlreadyExists_ThenReturnsFail()
        {
            // Arrange
            int seasonId = await PrepareWeek_ReadyForClosingSubmissions();

            await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);
            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.SubmissionsClosed, 3);

            // Act
            var result = await _weekService.TransitionToCloseSubmissionsAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenClosingSubmissions_WithNoTeams_ThenReturnsFail()
        {
            // Arrange
            int weekNumber = 1;
            int submissionRequired = 1;
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, weekNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, submissionRequired);
            await _weekService.TransitionToOpenWeekAsync(seasonId, weekNumber);

            // Act
            var result = await _weekService.TransitionToCloseSubmissionsAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenClosingSubmissions_WithNotEnoughSubmissions_ThenReturnsFail()
        {
            // Arrange
            int weekNumber = 1;
            int submissionRequired = 1;
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, weekNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, submissionRequired);
            await _weekService.TransitionToOpenWeekAsync(seasonId, weekNumber);
            int playerId1 = await CreateTeamWithPlayer(seasonId, "Team1");
            int playerId2 = await CreateTeamWithPlayer(seasonId, "Team2");
            await _deckSubmissionService.SubmitAsync(seasonId, (int)playerId1, "deck content", 1);

            // Act
            var result = await _weekService.TransitionToCloseSubmissionsAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }

        //________________________________________________________________________
        // CloseSubmission -> InProgress
        //________________________________________________________________________

        [Fact]
        public async Task WhenMovingToInProgressWeek_WithCloseSubmissionWeek_ThenReturnsSuccess()
        {
            // Arrange
            int seasonId = await PrepareWeek_ReadyForClosingSubmissions();
            await _weekService.TransitionToCloseSubmissionsAsync(seasonId);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
        }
        [Fact]
        public async Task WhenMovingToInProgressWeek_WithNoCloseSubmissionWeek_ThenReturnsSuccess()
        {
            // Arrange
            int seasonId = await PrepareWeek_ReadyForClosingSubmissions();

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenMovingToInProgressWeek_WithInProgressWeekAlreadyExists_ThenReturnsFail()
        {
            // Arrange
            int seasonId = await PrepareWeek_ReadyForClosingSubmissions();
            await _weekService.TransitionToCloseSubmissionsAsync(seasonId);

            await _weekService.CreateAsync(seasonId, 2, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
            await _weekService.UpdateAsync(seasonId, 2, null, null, null, WeekStatus.InProgress, null);

            // Act
            var result = await _weekService.TransitionToInProgressAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }

        //________________________________________________________________________
        // InProgress -> Completed
        //________________________________________________________________________

        [Fact]
        public async Task WhenCompletingWeek_WithNoInProgressWeek_ThenReturnsFail()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();

            // Act
            var result = await _weekService.TransitionToCompletedAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task WhenCompletingWeek_WithInProgressWeek_ThenReturnsSuccess()
        {
            // Arrange
            int seasonId = await PrepareReadyToCloseWeek();

            // Act
            var result = await _weekService.TransitionToCompletedAsync(seasonId);

            // Assert
            result.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenCompletingWeek_WithNotAllMatchesReported_ThenReturnsFail()
        {
            // Arrange
            int seasonId = await PrepareWeek_ReadyForClosingSubmissions();
            await _weekService.TransitionToCloseSubmissionsAsync(seasonId);
            await _weekService.TransitionToInProgressAsync(seasonId);

            // Act
            var result = await _weekService.TransitionToCompletedAsync(seasonId);

            // Assert
            result.Success.ShouldBeFalse();
        }




        [Fact]
        public async Task WhenDeletingExistingWeek_ThenReturnsSuccess()
        {
            // Arrange
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 3);

            // Act
            var result = await _weekService.DeleteAsync(seasonId, 1);

            // Assert
            result.Success.ShouldBeTrue();
        }

        #endregion
    }
}
