using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Repositories;

namespace WarLeague.Discord.Commands
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("one-time-setup", "Bind discord roles to app roles (admin, captain)")]
    public class OneTimeSetupCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PermissionRepository _permissionRepository;
        public OneTimeSetupCommands(PermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }
        [SlashCommand("bind", "Binds permission to role. Only 1 role can be bound to a given permission.")]
        public async Task BindAsync(
            PermissionType permission,
            SocketRole role)
        {
            await DeferAsync(ephemeral: false);

            _ = await _permissionRepository.BindAsync(Context.Guild.Id, permission, role.Id);

            await FollowupAsync($"Bound `{permission}` to role `{role.Name}`");
        }

        [SlashCommand("unbind", "Removes the role binding for a permission.")]
        public async Task UnbindAsync(
            PermissionType permission)
        {
            await DeferAsync(ephemeral: false);

            bool success = await _permissionRepository.UnbindAsync(Context.Guild.Id, permission);

            if (!success)
            {
                await FollowupAsync($"No binding found for `{permission}`.");
                return;
            }

            await FollowupAsync($"Unbound `{permission}` successfully.");
        }
    }
}
