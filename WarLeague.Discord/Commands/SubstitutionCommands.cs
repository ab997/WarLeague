using Discord;
using Discord.Interactions;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;
using WarLeague.Core.Services;
using WarLeague.Discord.Constants;
using WarLeague.Discord.Helpers;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands
{
    [EnsureChannelIsInFormatCategory]
    [EnsureSingleActiveSeason]
    [RequireRole(DiscordRoleConstants.Admin)]
    public class SubstitutionCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordApiHelperService _helperService;
        private readonly DiscordPlayerService _playerService;
        private readonly SubstitutionService _substitutionService;

        public SubstitutionCommands(
            DiscordApiHelperService helperService,
            DiscordPlayerService playerService,
            SubstitutionService substitutionService)
        {
            _helperService = helperService;
            _playerService = playerService;
            _substitutionService = substitutionService;
        }

        [SlashCommand("substitution", "Substitute a player in a match for another available player from the same team")]
        public async Task SubstitutionAsync(
             [Summary("team-name", "The team for which substitution is being made")] string teamName,
             [Summary("player-in", "Player who will play instead of the current player")] IUser playerIn,
             [Summary("player-out", "Player who is being substituted")] IUser playerOut
            )
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
            Player playerInEntity = await _playerService.EnsurePlayerExistsAsync(playerIn);
            Player playerOutEntity = await _playerService.EnsurePlayerExistsAsync(playerOut);

            BaseResult result = await _substitutionService.SubstitutePlayerAsync(
                season.Id,
                teamName,
                playerInEntity.Id,
                playerOutEntity.Id);

            await FollowupAsync(ResultHelper.Stringify(result));
        }
    }
}
