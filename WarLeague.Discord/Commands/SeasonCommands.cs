
using Discord.Interactions;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Domain.Services;
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
        private readonly SeasonService _seasonService;
        private readonly DiscordApiHelperService _helperService;
        public SeasonCommands(SeasonService seasonService, DiscordApiHelperService helperService)
        {
            _seasonService = seasonService;
            _helperService = helperService;
        }
        [SlashCommand("create", "Creates a new season")]
        public async Task CreateAsync(int seasonNumber)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            var season = _seasonService.CreateAsync(format.Id, seasonNumber);

            if (season is null)
            {
                await FollowupAsync($"Season with number {seasonNumber} already exists.");
                return;
            }
          
            await FollowupAsync($"Season '{seasonNumber}' created (inactive).");
        }

        [SlashCommand("delete", "Deletes a season")]
        public async Task DeleteAsync(int seasonNumber)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            var season = await _seasonService.DeleteAsync(seasonNumber, format.Id);

            if (season == null)
            {
                await FollowupAsync($"Season with number {seasonNumber} not found.");
                return;
            }

            await FollowupAsync($"Season '{seasonNumber}' deleted.");
        }

        [SlashCommand("set-active", "Sets a season to active (all other to inactive)")]
        public async Task SetActive(int seasonNumber)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            var season = await _seasonService.SetActiveAsync(seasonNumber, format.Id);

            if (season == null)
            {
                await FollowupAsync($"Season with number {seasonNumber} not found.");
                return;
            }

            await FollowupAsync($"Season '{seasonNumber}' is now active.");
        }

        [SlashCommand("admin-set-team-modifications", "Enables or disables captain team modifications for the current season (Admin only)")]
        [EnsureSingleActiveSeason]
        public async Task AdminSetTeamModificationsAsync(
        [Summary("enabled", "True to enable captain add/drop, false to disable")] bool enabled)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            Season? result = await _seasonService.SetTeamModificationsAsync(season.Id, enabled);

            if (result == null)
            {
                await FollowupAsync($"Failed to update team modifications for season {season.SeasonNumber}.");
                return;
            }

            await FollowupAsync($"Captain team modifications have been {(enabled ? "enabled" : "disabled")} for season {season.SeasonNumber}.");
        }
    }
}
