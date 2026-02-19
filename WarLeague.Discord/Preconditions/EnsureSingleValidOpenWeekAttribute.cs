
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Services;
using Format = WarLeague.Data.Entities.Format;
using Season = WarLeague.Data.Entities.Season;

namespace WarLeague.Discord.Preconditions
{
    public class EnsureSingleValidOpenWeekAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            SeasonRepository seasonRepository = services.GetRequiredService<SeasonRepository>();
            WeekRepository weekRepository = services.GetRequiredService<WeekRepository>();
            DiscordApiHelperService helperService = services.GetRequiredService<DiscordApiHelperService>();

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

            Season season = seasons.Single();

            Week? openWeek = await weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(season.Id, WeekStatus.Open);

            if (openWeek is null)
            {
                return PreconditionResult.FromError(ErrorMessage ?? "No open week found for the season.");
            }

            if (openWeek.Status != WeekStatus.Open)
            {
                return PreconditionResult.FromError(ErrorMessage ?? "Deck submissions are not open for the current week.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
