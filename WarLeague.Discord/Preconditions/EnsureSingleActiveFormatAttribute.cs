
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;
using Format = WarLeague.Core.Data.Entities.Format;

namespace WarLeague.Discord.Preconditions
{
    public class EnsureSingleActiveFormatAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            FormatRepository formatRepository = services.GetRequiredService<FormatRepository>();

            Format? singleActiveFormat = await formatRepository.GetSingleActiveFormatOrDefaultAsync();

            if (singleActiveFormat is null)
            {
                return PreconditionResult.FromError(ErrorMessage ?? "There is no active format set. Please set an active format before using this command.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
