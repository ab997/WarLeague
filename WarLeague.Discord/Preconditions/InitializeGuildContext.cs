
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Data.Entities;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Services;
using Format = WarLeague.Data.Entities.Format;
using Season = WarLeague.Data.Entities.Season;
using WarLeague.Data.Data;

namespace WarLeague.Discord.Preconditions
{
    public class InitializeGuildContextAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            SetGuildIdFromContext(context, services);
            return PreconditionResult.FromSuccess();
        }

        public static void SetGuildIdFromContext(IInteractionContext context, IServiceProvider services)
        {
            GuildContextService guildContextService = services.GetRequiredService<GuildContextService>();
            guildContextService.SetGuildId(context.Guild.Id);
        }
    }
}
