
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Core.Repositories;

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

            // because order of attribute execution is not deterministic, add this to every precondition just in case
            InitializeGuildContextAttribute.SetGuildIdFromContext(context, services);

            SocketTextChannel textChannel = (SocketTextChannel)channel;

            string channelName = textChannel.Category.Name;

            FormatRepository formatRepository = services.GetRequiredService<FormatRepository>();

            // check if there is Single Format Mode enabled

            (bool isSingleFormatMode, _) = await formatRepository.GetSingleFormatModeFormatAsync();

            if (isSingleFormatMode)
            {
                return PreconditionResult.FromSuccess();
            }

            // else proceed to check if the channel category name matches any format name

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
