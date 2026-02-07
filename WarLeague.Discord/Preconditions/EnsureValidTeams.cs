using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Core.Services;
using WarLeague.Discord.Services;
using Format = WarLeague.Data.Entities.Format;

namespace WarLeague.Discord.Preconditions
{
    internal class EnsureValidTeamsAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            SeasonRepository seasonRepository = services.GetRequiredService<SeasonRepository>();
            DiscordApiHelperService helperService = services.GetRequiredService<DiscordApiHelperService>();
            TeamValidationService teamValidationService = services.GetRequiredService<TeamValidationService>();

            Format format = await helperService.GetFormatByCategoryNameAsync((SocketInteractionContext)context);

            List<Season> seasons = await seasonRepository.GetActiveSeasonsByFormatAsync(format.Id);

            // assume one single active season - season validation should be called before this precondition

            Season season = seasons.Single();

            BaseResult validationResult = await teamValidationService.ValidateAllTeamsInSeasonAsync(season.Id);

            if (!validationResult.Success)
            {
                return PreconditionResult.FromError(ErrorMessage ?? validationResult.Message);
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
