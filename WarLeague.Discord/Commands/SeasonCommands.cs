
using Discord.Interactions;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands
{
    [Group("season", "Season commands")]
    [RequireRole("Admin")]
    [EnsureChannelIsInFormatCategory]
    public class SeasonCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly SeasonRepository _seasonRepository;
        private readonly HelperService _helperService;
        public SeasonCommands(SeasonRepository seasonRepository, HelperService helperService)
        {
            _seasonRepository = seasonRepository;
            _helperService = helperService;
        }
        [SlashCommand("create", "Creates a new season")]
        public async Task CreateAsync(int seasonNumber)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            var existing = format.Seasons.SingleOrDefault(s => s.SeasonNumber == seasonNumber);
            if (existing != null)
            {
                await FollowupAsync($"Season with number {seasonNumber} already exists.");
                return;
            }

            var season = new Season
            {
                SeasonNumber = seasonNumber,
                Format = format,
                Active = false
            };

            await _seasonRepository.AddAsync(season);
            await FollowupAsync($"Season '{seasonNumber}' created (inactive).");
        }

        [SlashCommand("delete", "Deletes a season")]
        public async Task DeleteAsync(int seasonNumber)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(seasonNumber, format.Id);
            if (season == null)
            {
                await FollowupAsync($"Season with number {seasonNumber} not found.");
                return;
            }

            await _seasonRepository.DeleteAsync(season);
            await FollowupAsync($"Season '{seasonNumber}' deleted.");
        }

        [SlashCommand("set-active", "Sets a season to active (all other to inactive)")]
        public async Task SetActive(int seasonNumber)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(seasonNumber, format.Id);
            if (season == null)
            {
                await FollowupAsync($"Season with number {seasonNumber} not found.");
                return;
            }

            var allSeasons = await _seasonRepository.GetAllByFormatAsync(format.Id);

            foreach (var s in allSeasons)
                s.Active = false;

            await _seasonRepository.UpdateRangeAsync(allSeasons);

            season.Active = true;
            await _seasonRepository.UpdateAsync(season);


            await FollowupAsync($"Season '{seasonNumber}' is now active.");
        }
    }
}
