using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Security;
using WarLeague.Core.Repositories;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;
using WarLeague.Data.Repositories;
using Format = WarLeague.Data.Entities.Format;

namespace WarLeague.Discord.Services
{
    public class DiscordApiHelperService
    {
        private readonly FormatRepository _formatRepository;
        private readonly SeasonRepository _seasonRepository;
        private readonly PermissionRepository _permissionRepository;
        public DiscordApiHelperService(FormatRepository formatRepository, SeasonRepository seasonRepository, PermissionRepository permissionRepository)
        {
            _formatRepository = formatRepository;
            _seasonRepository = seasonRepository;
            _permissionRepository = permissionRepository;
        }
        public async Task<Format> GetFormatByCategoryNameAsync(SocketInteractionContext context)
        {
            SocketTextChannel channel = (SocketTextChannel)context.Channel;

            (bool isSingleFormatMode, Format? format) = await _formatRepository.GetSingleFormatModeFormatAsync();

            if (isSingleFormatMode && format != null)
            {
                return format;
            }

            string categoryName = channel.Category.Name;

            return (await _formatRepository.GetByNameAsync(categoryName))!;
        }
        /// <summary>
        /// assumes that category name is format name is ensured
        /// assumes that single active season per format is ensured
        /// </summary>
        public async Task<Season> GetSeasonByCategoryNameAsync(SocketInteractionContext context)
        {
            SocketTextChannel channel = (SocketTextChannel)context.Channel;

            string categoryName = channel.Category.Name;

            (bool isSingleFormatMode, Format? format) = await _formatRepository.GetSingleFormatModeFormatAsync();

            if (isSingleFormatMode && format != null)
            {
                categoryName = format.Name;
            }

            return (await _seasonRepository.GetSingleActiveSeasonByFormatNameAsync(categoryName))!;
        }

        /// <summary>
        /// Returns true if the invoking user has a role named "Admin" on the guild.
        /// </summary>
        public bool IsUserAdmin(SocketInteractionContext context)
        {
            var guildUser = context.User as SocketGuildUser;
            if (guildUser == null) return false;

            ulong guildId = context.Guild.Id;
            PermissionType permission = PermissionType.Admin;

            IReadOnlyCollection<ulong> allowedRoleIds =
               _permissionRepository.GetRoleIds(context.Guild.Id, permission);

            return guildUser.Roles.Any(r => allowedRoleIds.Contains(r.Id));
        }


        public async Task SendEmbedsInBatchesAsync(
            SocketInteractionContext context,
            IReadOnlyList<Embed> embeds)
        {
            if (embeds == null || embeds.Count == 0)
            {
                await context.Interaction.FollowupAsync("Nothing to show.");
                return;
            }

            const int maxEmbeds = 10;
            const int maxCharacters = 6000;

            var batch = new List<Embed>();
            var characterCount = 0;

            foreach (var embed in embeds)
            {
                // Discord.Net already calculates the total length of this embed.
                var length = embed.Length;

                if (batch.Count > 0 &&
                    (batch.Count >= maxEmbeds ||
                     characterCount + length > maxCharacters))
                {
                    await context.Interaction.FollowupAsync(
                        embeds: batch.ToArray());

                    batch.Clear();
                    characterCount = 0;
                }

                batch.Add(embed);
                characterCount += length;
            }

            if (batch.Count > 0)
            {
                await context.Interaction.FollowupAsync(
                    embeds: batch.ToArray());
            }
        }
        public async Task SendEmbedInBatchesAsync(SocketInteractionContext context, Embed embed)
        {
            IReadOnlyList<Embed> embeds = [embed];
            await SendEmbedsInBatchesAsync(context, embeds);
        }
    }
}
