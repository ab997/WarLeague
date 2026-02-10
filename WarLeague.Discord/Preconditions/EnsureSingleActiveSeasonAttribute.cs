
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Data.Entities;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Services;
using Format = WarLeague.Data.Entities.Format;
using Season = WarLeague.Data.Entities.Season;

namespace WarLeague.Discord.Preconditions
{
    public class EnsureSingleActiveSeasonAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            SeasonRepository seasonRepository = services.GetRequiredService<SeasonRepository>();
            DiscordApiHelperService helperService = services.GetRequiredService<DiscordApiHelperService>();

            // because order of attribute execution is not deterministic, add this to every precondition just in case
            InitializeGuildContextAttribute.SetGuildIdFromContext(context, services);

            Format format = await helperService.GetFormatByCategoryNameAsync((SocketInteractionContext)context);

            List<Season> seasons = await seasonRepository.GetActiveSeasonsByFormatAsync(format.Id);

            if (seasons.Count == 0)
            {
                return PreconditionResult.FromError(ErrorMessage ?? "There is no active season set. Please set an active season before using this command.");
            }

            if (seasons.Count > 1)
            {
                return PreconditionResult.FromError(ErrorMessage ?? "There are multiple active seasons set. Please ensure only one active season exists before using this command.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
