
using Discord.Interactions;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;

namespace WarLeague.Discord.Commands
{
    [Group("season", "Season commands")]
    [RequireRole("Admin")]
    public class SeasonCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly SeasonRepository _seasonRepository;
        private readonly FormatRepository _formatRepository;
        public SeasonCommands(SeasonRepository seasonRepository, FormatRepository formatRepository)
        {
            _seasonRepository = seasonRepository;
            _formatRepository = formatRepository;
        }
        [SlashCommand("create-season", "Creates a new season")]
        public async Task CreateSeasonAsync(int seasonNumber, string formatName)
        {
            await DeferAsync(ephemeral: true);

            var format = await _formatRepository.GetByNameAsync(formatName);
            if (format == null)
            {
                await FollowupAsync($"Format with name {formatName} not found.");
                return;
            }

            var existing = format.Seasons.SingleOrDefault(s => s.SeasonNumber == seasonNumber);
            if (existing != null)
            {
                await FollowupAsync($"Season with number {seasonNumber} already exists.");
                return;
            }

            var season = new Season
            {
                SeasonNumber = seasonNumber,
                FormatId = format.Id,
                Format = format,
                Active = false
            };

            await _seasonRepository.AddAsync(season);
            await FollowupAsync($"Season '{seasonNumber}' created (inactive).");
        }

        [SlashCommand("delete-season", "Deletes a season")]
        public async Task DeleteSeasonAsync(int seasonNumber, string formatName)
        {
            await DeferAsync(ephemeral: true);

            var season = await _seasonRepository.GetByNumberAndFormatAsync(seasonNumber, formatName);
            if (season == null)
            {
                await FollowupAsync($"Season with number {seasonNumber} not found.");
                return;
            }

            await _seasonRepository.DeleteAsync(season);
            await FollowupAsync($"Season '{seasonNumber}' deleted.");
        }

        [SlashCommand("enable-season", "Enables a season (sets Active = true)")]
        public async Task EnableSeasonAsync(int seasonNumber, string formatName)
        {
            await DeferAsync(ephemeral: true);

            var season = await _seasonRepository.GetByNumberAndFormatAsync(seasonNumber, formatName);
            if (season == null)
            {
                await FollowupAsync($"Season with number {seasonNumber} not found.");
                return;
            }

            if (season.Active)
            {
                await FollowupAsync($"Season '{seasonNumber}' is already enabled.");
                return;
            }

            season.Active = true;
            await _seasonRepository.UpdateAsync(season);
            await FollowupAsync($"Season '{seasonNumber}' enabled.");
        }

        [SlashCommand("disable-season", "Disables a season (sets Active = false)")]
        public async Task DisableSeasonAsync(int seasonNumber, string formatName)
        {
            await DeferAsync(ephemeral: true);

            var season = await _seasonRepository.GetByNumberAndFormatAsync(seasonNumber, formatName);
            if (season == null)
            {
                await FollowupAsync($"Season with number {seasonNumber} not found.");
                return;
            }

            if (!season.Active)
            {
                await FollowupAsync($"Season '{seasonNumber}' is already disabled.");
                return;
            }

            season.Active = false;
            await _seasonRepository.UpdateAsync(season);
            await FollowupAsync($"Season '{seasonNumber}' disabled.");
        }
    }
}
