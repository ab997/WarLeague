
using Microsoft.SqlServer.Server;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services
{
    public class SeasonService
    {
        private readonly SeasonRepository _seasonRepository;
        private readonly FormatRepository _formatRepository;
        public SeasonService(SeasonRepository seasonRepository, FormatRepository formatRepository)
        {
            _seasonRepository = seasonRepository;
            _formatRepository = formatRepository;
        }

        /// <summary>
        /// creates a new season for the given format if season with seasonNumber  does not already exist
        /// </summary>
        /// <param name="format"></param>
        /// <param name="seasonNumber"></param>
        /// <returns></returns>
        public async Task<Season?> CreateAsync(int formatId, int seasonNumber, int minTeamMembers)
        {
            var format = await _formatRepository.GetByIdAsync(formatId);
            var existing = format.Seasons.SingleOrDefault(s => s.SeasonNumber == seasonNumber);

            if (existing is not null)
            {
                return null;
            }

            var season = new Season
            {
                SeasonNumber = seasonNumber,
                Format = format,
                MinimumTeamMembers = minTeamMembers,
                Active = false
            };

            await _seasonRepository.AddAsync(season);

            return season;
        }

        public async Task<Season?> DeleteAsync(int formatId, int seasonNumber)
        {
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(seasonNumber, formatId);

            if (season == null)
            {
                return null;
            }

            await _seasonRepository.DeleteAsync(season);

            return season;
        }

        public async Task<Season?> SetActiveAsync(int formatId, int seasonNumber)
        {
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(seasonNumber, formatId);

            if (season == null)
            {
                return null;
            }
            var allSeasons = await _seasonRepository.GetAllByFormatAsync(formatId);

            foreach (var s in allSeasons)
                s.Active = false;

            await _seasonRepository.UpdateRangeAsync(allSeasons);

            season.Active = true;
            await _seasonRepository.UpdateAsync(season);

            return season;
        }

        public async Task<Season?> SetTeamModificationsAsync(int seasonId, bool enabled) 
        {
            var season =  await _seasonRepository.GetByIdOrDefault(seasonId);
            if (season == null)
            {
                return null;
            }
            season.DisableTeamModification = !enabled;

            await _seasonRepository.UpdateAsync(season);
            return season;
        }
    }
}
