using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Discord.Constants;
using WarLeague.Discord.Preconditions;

namespace WarLeague.Discord.Commands
{
    [EnsureChannelIsInFormatCategory]
    [EnsureSingleActiveSeason]
    [RequireRole(DiscordRoleConstants.Admin)]
    public class SubstitutionCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("substitution", "Substitute a player in a match for another available player from the same team")]
        public async Task SubstitutionAsync(IUserMessage message)
        {
            // Implementation for submitting a .ydk file
        }
    }
}
