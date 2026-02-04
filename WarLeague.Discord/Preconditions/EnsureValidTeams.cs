using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Preconditions
{
    internal class EnsureValidTeamsAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            SeasonRepository seasonRepository = services.GetRequiredService<SeasonRepository>();
            DiscordApiHelperService helperService = services.GetRequiredService<DiscordApiHelperService>();
            TeamValidationService teamValidationService = services.GetRequiredService<TeamValidationService>();

            Core.Data.Entities.Format format = await helperService.GetFormatByCategoryNameAsync((SocketInteractionContext)context);

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
