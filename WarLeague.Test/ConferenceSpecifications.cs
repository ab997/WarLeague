using Microsoft.EntityFrameworkCore;
using Shouldly;
using WarLeague.Data.Entities;

namespace WarLeague.Test
{
    public partial class Specifications
    {
        [Fact]
        public async Task WhenCreatingTeamWithValidConference_ThenConferenceIsAssigned()
        {
            var (_, seasonId) = await CreateFormatAndSeason();

            var alphaConference = new Conference
            {
                SeasonId = seasonId,
                Name = "Alpha"
            };

            _context.Conferences.Add(alphaConference);
            await _context.SaveChangesAsync();

            Player captain = await CreatePlayer(910001);

            var result = await _teamService.CreateAsync(seasonId, "Alpha Wolves", captain.Id, "Alpha", canBypassTeamModificationCheck: true);

            result.Success.ShouldBeTrue();

            Team team = await _context.Teams.SingleAsync(t => t.SeasonId == seasonId && t.Name == "Alpha Wolves");
            team.ConferenceId.ShouldBe(alphaConference.Id);
        }

        [Fact]
        public async Task WhenUpdatingTeamConferenceToAnotherConference_ThenConferenceIsUpdated()
        {
            var (_, seasonId) = await CreateFormatAndSeason();

            var alphaConference = new Conference
            {
                SeasonId = seasonId,
                Name = "Alpha"
            };

            var betaConference = new Conference
            {
                SeasonId = seasonId,
                Name = "Beta"
            };

            _context.Conferences.Add(alphaConference);
            _context.Conferences.Add(betaConference);
            await _context.SaveChangesAsync();

            Player captain = await CreatePlayer(910002);

            var createResult = await _teamService.CreateAsync(seasonId, "Conference Movers", captain.Id, "Alpha", canBypassTeamModificationCheck: true);
            createResult.Success.ShouldBeTrue();

            var updateResult = await _teamService.UpdateConferenceAsync(seasonId, "Conference Movers", "Beta", canBypassTeamModificationCheck: true);

            updateResult.Success.ShouldBeTrue();

            Team team = await _context.Teams.SingleAsync(t => t.SeasonId == seasonId && t.Name == "Conference Movers");
            team.ConferenceId.ShouldBe(betaConference.Id);
        }
    }
}
