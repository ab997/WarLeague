using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.ComponentModel;
using WarLeague.Data.Entities;

namespace WarLeague.Test
{
    /// <summary>
    /// Conference behavior specifications. Covers all ConferenceService behaviours in AAA style;
    /// tests use services only (no direct DB in ConferenceService specs).
    /// </summary>
    public partial class Specifications
    {
        [Fact]
        [Trait("Category", "Conference")]
        public async Task WhenCreatingConferenceWithDuplicateNameInSameSeason_ThenReturnsFail()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            await _conferenceService.CreateAsync(seasonId, "Alpha", 1);

            var result = await _conferenceService.CreateAsync(seasonId, "Alpha", 1);

            result.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "Conference")]
        public async Task WhenCreatingConferenceWithNegativePlayoffTeamsCount_ThenReturnsFail()
        {
            var (_, seasonId) = await CreateFormatAndSeason();

            var result = await _conferenceService.CreateAsync(seasonId, "Alpha", playoffTeamsCount: -1);

            result.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "Conference")]
        public async Task WhenCreatingConferenceWithZeroPlayoffTeamsCount_ThenReturnsFail()
        {
            var (_, seasonId) = await CreateFormatAndSeason();

            var result = await _conferenceService.CreateAsync(seasonId, "Alpha", 0);

            result.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "Conference")]
        public async Task WhenCreatingConferenceWithValidNameAndPlayoffTeams_ThenReturnsSuccess()
        {
            var (_, seasonId) = await CreateFormatAndSeason();

            var result = await _conferenceService.CreateAsync(seasonId, "Alpha", playoffTeamsCount: 8);

            result.Success.ShouldBeTrue();
        }

        [Fact]
        [Trait("Category", "Conference")]
        public async Task WhenUpdatingConferenceWithNewNameThatAlreadyExists_ThenReturnsFail()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            await _conferenceService.CreateAsync(seasonId, "Alpha", 1);
            await _conferenceService.CreateAsync(seasonId, "Beta", 1);

            var result = await _conferenceService.UpdateAsync(seasonId, "Alpha", newName: "Beta");

            result.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "Conference")]
        public async Task WhenUpdatingConferenceWithNegativePlayoffTeamsCount_ThenReturnsFail()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            await _conferenceService.CreateAsync(seasonId, "Alpha", playoffTeamsCount: 4);

            var result = await _conferenceService.UpdateAsync(seasonId, "Alpha", playoffTeamsCount: -1);

            result.Success.ShouldBeFalse();
        }



        [Fact]
        [Trait("Category", "Conference")]
        public async Task WhenDeletingConferenceThatHasTeams_ThenReturnsFail()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            await _conferenceService.CreateAsync(seasonId, "Alpha", 1);
            var captain = await CreatePlayer(910001);
            await _teamService.CreateAsync(seasonId, "Alpha Wolves", captain.Id, "Alpha", canBypassTeamModificationCheck: true);

            var result = await _conferenceService.DeleteAsync(seasonId, "Alpha");

            result.Success.ShouldBeFalse();
        }

        [Fact]
        [Trait("Category", "Conference")]
        public async Task WhenDeletingConferenceWithNoTeams_ThenReturnsSuccess()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            await _conferenceService.CreateAsync(seasonId, "Alpha", 1);

            var result = await _conferenceService.DeleteAsync(seasonId, "Alpha");

            result.Success.ShouldBeTrue();
            result.Message.ShouldContain("deleted");
            var listResult = await _conferenceService.ListAsync(seasonId);
            listResult.Message.ShouldContain("No conferences found");
        }
    }
}
