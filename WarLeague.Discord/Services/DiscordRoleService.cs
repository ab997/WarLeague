using Discord;
using Discord.WebSocket;
using WarLeague.Core.Data.Entities;
using WarLeague.Discord.Model;

namespace WarLeague.Discord.Services
{
    public class DiscordRoleService
    {
        private readonly DiscordSocketClient _discordClient;

        public DiscordRoleService(DiscordSocketClient discordClient)
        {
            _discordClient = discordClient;
        }
        public async Task<SocketRoleResult> CreateAndAssignTeamRoleAsync(SocketGuild guild, string teamName, Player player)
        {
            SocketRole? role = await CreateTeamRoleAsync(guild, teamName);

            if (role is null) return new SocketRoleResult { Message = "Failed to create role." };

            bool roleResult = await AssignRoleToPlayerAsync(guild, player, role!);

            if (!roleResult)
            {
                return new SocketRoleResult { Role = role, Message = "Failed to assign role, but role was created." };
            }

            return new SocketRoleResult { Success = true, Role = role, Message = $"Role created and assigned to {player.UserName}."};
        }
        /// <summary>
        /// Creates a Discord role for a team in the specified guild.
        /// Returns the created role's ID or null if creation fails.
        /// </summary>
        public async Task<SocketRole?> CreateTeamRoleAsync(SocketGuild guild, string teamName)
        {
            // Create role with default color and permissions
            IRole role = await guild.CreateRoleAsync(
                name: teamName,
                permissions: GuildPermissions.None,
                color: GenerateRandomColor(),
                isHoisted: false,
                isMentionable: true);

            return guild.GetRole(role.Id);
        }

        /// <summary>
        /// Deletes a Discord role for a team from the specified guild.
        /// </summary>
        public async Task<bool> DeleteTeamRoleAsync(SocketGuild guild, Team team)
        {
            if (guild == null || team?.DiscordRoleId == null)
            {
                return false;
            }

            SocketRole? role = guild.GetRole(team.DiscordRoleId.Value);
            if (role == null)
            {
                return false;
            }

            await role.DeleteAsync();
            return true;
        }

        /// <summary>
        /// Changes the color of a team's Discord role.
        /// </summary>
        public async Task<bool> ChangeRoleColorAsync(SocketGuild guild, Team team, Color color)
        {
            if (guild == null || team?.DiscordRoleId == null)
            {
                return false;
            }

            SocketRole? role = guild.GetRole(team.DiscordRoleId.Value);
            if (role == null)
            {
                return false;
            }

            await role.ModifyAsync(properties => properties.Color = color);
            return true;
        }

        /// <summary>
        /// Assigns a team role to a player (guild member).
        /// </summary>
        public async Task<bool> AssignRoleToPlayerAsync(SocketGuild guild, Player player, SocketRole role)
        {
            SocketGuildUser? user = guild.GetUser(player.DiscordUserId);
            if (user == null)
            {
                return false;
            }
            await user.AddRoleAsync(role);
            return true;
        }

        /// <summary>
        /// Removes a team role from a player (guild member).
        /// </summary>
        public async Task<bool> RemoveRoleFromPlayerAsync(SocketGuild guild, Player player, Team team)
        {
            if (guild == null || player == null || team?.DiscordRoleId == null)
            {
                return false;
            }

            SocketGuildUser? user = guild.GetUser(player.DiscordUserId);
            if (user == null)
            {
                return false;
            }

            SocketRole? role = guild.GetRole(team.DiscordRoleId.Value);
            if (role == null)
            {
                return false;
            }

            await user.RemoveRoleAsync(role);
            return true;
        }

        /// <summary>
        /// Assigns the Captain role to a player (guild member).
        /// </summary>
        public async Task<bool> AssignCaptainRoleAsync(SocketGuild guild, Player player, SocketRole captainRole)
        {
            if (guild == null || player == null || captainRole == null)
            {
                return false;
            }

            SocketGuildUser? user = guild.GetUser(player.DiscordUserId);
            if (user == null)
            {
                return false;
            }

            await user.AddRoleAsync(captainRole);
            return true;
        }

        /// <summary>
        /// Removes the Captain role from a player (guild member).
        /// </summary>
        public async Task<bool> RemoveCaptainRoleAsync(SocketGuild guild, Player player, SocketRole captainRole)
        {
            if (guild == null || player == null || captainRole == null)
            {
                return false;
            }

            SocketGuildUser? user = guild.GetUser(player.DiscordUserId);
            if (user == null)
            {
                return false;
            }

            await user.RemoveRoleAsync(captainRole);
            return true;
        }

        /// <summary>
        /// Generates a random Discord color for team roles.
        /// </summary>
        private static Color GenerateRandomColor()
        {
            Random random = new Random();
            return new Color((uint)random.Next(0x1000000));
        }
    }
}
