
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Core.Repositories;
using Season = WarLeague.Core.Data.Entities.Season;

namespace WarLeague.Discord.Preconditions
{
    public class EnsureSingleActiveSeasonAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            SeasonRepository seasonRepository = services.GetRequiredService<SeasonRepository>();

            Season? singleActiveSeason = await seasonRepository.GetSingleActiveSeasonOrDefaultAsync();

            if (singleActiveSeason is null)
            {
                return PreconditionResult.FromError(ErrorMessage ?? "There is no active season set. Please set an active season before using this command.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
