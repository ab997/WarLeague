using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Repositories;

namespace WarLeague.Discord.Preconditions
{
    public sealed class RequireAppPermissionAttribute : PreconditionAttribute
    {
        private readonly PermissionType _permission;

        public RequireAppPermissionAttribute(PermissionType permission)
        {
            _permission = permission;
        }

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.Guild == null)
                return PreconditionResult.FromError("Guild-only command.");

            var user = context.User as SocketGuildUser;
            if (user == null)
                return PreconditionResult.FromError("Invalid user.");

            // because order of attribute execution is not deterministic, add this to every precondition just in case
            InitializeGuildContextAttribute.SetGuildIdFromContext(context, services);

            var repo = services.GetRequiredService<PermissionRepository>();

            // TODO: this is worth caching
            IReadOnlyCollection<ulong> allowedRoleIds =
                await repo.GetRoleIdsAsync(context.Guild.Id, _permission);

            if (allowedRoleIds.Count == 0)
                return PreconditionResult.FromError("Permission not configured for this server. Use one-time-setup commands to configure it.");

            bool hasRole = user.Roles.Any(r => allowedRoleIds.Contains(r.Id));

            return hasRole
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("Missing required permission.");
        }
    }
}
