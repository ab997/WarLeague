

using Discord.Interactions;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Core.Services;
using WarLeague.Discord.Helpers;
using WarLeague.Discord.Autocomplete;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using WarLeague.Data.Data.Enums;

namespace WarLeague.Discord.Commands
{
    [Group("season", "Season commands")]
    [RequireAppPermission(PermissionType.Admin)]
    [EnsureChannelIsInFormatCategory]
    [InitializeGuildContext]
    public class SeasonCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly SeasonService _seasonService;
        private readonly SeasonRepository _seasonRepository;
        private readonly DiscordApiHelperService _helperService;

        public SeasonCommands(SeasonService seasonService, SeasonRepository seasonRepository, DiscordApiHelperService helperService)
        {
            _seasonService = seasonService;
            _seasonRepository = seasonRepository;
            _helperService = helperService;
        }
        [SlashCommand("create", "Creates a new season")]
        public async Task CreateAsync( int seasonNumber, int minimumTeamMembers)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            BaseResult result = await _seasonService.CreateAsync(format.Id, seasonNumber, minimumTeamMembers);

            await FollowupAsync(ResultHelper.Stringify(result));
        }

        [SlashCommand("delete", "Deletes a season")]
        public async Task DeleteAsync([Autocomplete(typeof(SeasonNumberAutocompleteHandler))] int seasonNumber)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            BaseResult result = await _seasonService.DeleteAsync(format.Id, seasonNumber);

            await FollowupAsync(ResultHelper.Stringify(result));
        }

        [SlashCommand("set-active", "Sets a season to active (all other to inactive)")]
        public async Task SetActive([Autocomplete(typeof(SeasonNumberAutocompleteHandler))] int seasonNumber)
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);

            BaseResult result = await _seasonService.SetActiveAsync(format.Id, seasonNumber);

            await FollowupAsync(ResultHelper.Stringify(result));
        }

        [SlashCommand("admin-set-team-modifications", "Enables or disables captain team modifications for the current season (Admin only)")]
        [EnsureSingleActiveSeason]
        public async Task AdminSetTeamModificationsAsync(
        [Summary("enabled", "True to enable captain add/drop, false to disable")] bool enabled)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            BaseResult result = await _seasonService.SetTeamModificationsAsync(season.Id, enabled);

            await FollowupAsync(ResultHelper.Stringify(result));
        }

        [SlashCommand("switch-to-playoffs", "Switch the active season to Playoffs phase (cannot be reversed)")]
        [EnsureSingleActiveSeason]
        public async Task SwitchToPlayoffsAsync()
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            BaseResult result = await _seasonService.SetPhaseToPlayoffsAsync(season.Id);

            await FollowupAsync(ResultHelper.Stringify(result));
        }

        [SlashCommand("list", "Lists seasons in the current format")]
        public async Task ListAsync()
        {
            await DeferAsync(ephemeral: false);

            Format format = await _helperService.GetFormatByCategoryNameAsync(Context);
            var seasons = await _seasonRepository.GetAllByFormatAsync(format.Id);

            if (seasons.Count == 0)
            {
                await FollowupAsync($"Format **{format.Name}**: no seasons yet.");
                return;
            }

            var lines = seasons
                .OrderBy(s => s.SeasonNumber)
                .Select(s =>
                {
                    var phaseText = s.Phase == SeasonPhase.Playoffs ? "Playoffs" : "Round Robin";
                    var activeText = s.Active ? " • Active" : "";
                    return $"• Season **{s.SeasonNumber}**{activeText} — {phaseText}";
                });
            await FollowupAsync($"**Format: {format.Name}**\n**Seasons:**\n" + string.Join("\n", lines));
        }
    }
}
