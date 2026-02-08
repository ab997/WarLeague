
using Microsoft.SqlServer.Server;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Services
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

        public async Task<BaseResult> CreateAsync(int formatId, int seasonNumber, int minTeamMembers)
        {
            var format = await _formatRepository.GetByIdAsync(formatId);
            var existing = format.Seasons.SingleOrDefault(s => s.SeasonNumber == seasonNumber);

            if (existing is not null)
            {
                return new BaseResult(false, $"Season with number {seasonNumber} already exists.");
            }

            var season = new Season
            {
                SeasonNumber = seasonNumber,
                Format = format,
                MinimumTeamMembers = minTeamMembers,
                Active = false
            };

            await _seasonRepository.AddAsync(season);

            return new BaseResult(true, $"Season '{seasonNumber}' created (inactive).");
        }

        public async Task<BaseResult> DeleteAsync(int formatId, int seasonNumber)
        {
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(seasonNumber, formatId);

            if (season == null)
            {
                return new BaseResult(false, $"Season with number {seasonNumber} not found.");
            }

            await _seasonRepository.DeleteAsync(season);

            return new BaseResult(true, $"Season '{seasonNumber}' deleted.");
        }

        public async Task<SeasonResult> SetActiveAsync(int formatId, int seasonNumber)
        {
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(seasonNumber, formatId);

            if (season == null)
            {
                return new SeasonResult { Success = false,  Message = $"Season with number {seasonNumber} not found." };
            }
            var allSeasons = await _seasonRepository.GetAllByFormatAsync(formatId);

            foreach (var s in allSeasons)
                s.Active = false;

            await _seasonRepository.UpdateRangeAsync(allSeasons);

            season.Active = true;
            await _seasonRepository.UpdateAsync(season);

            return new SeasonResult { Success = true, Message = $"Season '{seasonNumber}' is now active.", Season = season };
        }

        public async Task<SeasonResult> SetTeamModificationsAsync(int seasonId, bool enabled) 
        {
            var season =  await _seasonRepository.GetByIdOrDefault(seasonId);
            if (season == null)
            {
                return new SeasonResult { Success = false, Message = $"Failed to update team modifications for season." };
            }
            season.DisableTeamModification = !enabled;

            await _seasonRepository.UpdateAsync(season);
            return new SeasonResult{ Success = true, Message = $"Captain team modifications have been {(enabled ? "enabled" : "disabled")} for season {season.SeasonNumber}.", Season = season };
        }
    }
}
