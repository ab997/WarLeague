
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Core.Repositories;
using Season = WarLeague.Core.Data.Entities.Season;

namespace WarLeague.Discord.Preconditions
{
    public class EnsureChannelIsInFormatCategory : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            var channel = context.Channel;

            if (channel is not SocketTextChannel)
            {
                return PreconditionResult.FromError(ErrorMessage ?? "This command can only be used in text channels.");
            }

            SocketTextChannel textChannel = (SocketTextChannel)channel;

            string channelName = textChannel.Category.Name;

            FormatRepository formatRepository = services.GetRequiredService<FormatRepository>();

            var hs = (await formatRepository.GetAllAsync())
                .Select(f => f.Name)
                .ToHashSet();

            if (!hs.Contains(channelName))
            {
                return PreconditionResult.FromError(ErrorMessage ?? "This command can only be used in format categories (category name should equal to format name).");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
