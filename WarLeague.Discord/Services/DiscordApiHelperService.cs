using Discord.Interactions;
using Discord.WebSocket;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;

namespace WarLeague.Discord.Services
{
    public class DiscordApiHelperService
    {
        private readonly FormatRepository _formatRepository;
        private readonly SeasonRepository _seasonRepository;
        public DiscordApiHelperService(FormatRepository formatRepository, SeasonRepository seasonRepository)
        {
            _formatRepository = formatRepository;
            _seasonRepository = seasonRepository;
        }
        public async Task<Format> GetFormatByCategoryNameAsync(SocketInteractionContext context)
        {
            SocketTextChannel channel = (SocketTextChannel)context.Channel;

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

            return (await _seasonRepository.GetSingleActiveSeasonByFormatNameAsync(categoryName))!;
        }

        /// <summary>
        /// Returns true if the invoking user has a role named "Admin" on the guild.
        /// </summary>
        public bool IsUserAdmin(SocketInteractionContext context)
        {
            var guildUser = context.User as SocketGuildUser;
            if (guildUser == null) return false;

            return guildUser.Roles.Any(r => string.Equals(r.Name, "Admin", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates that the provided URL is an absolute HTTP or HTTPS URL.
        /// </summary>
        public bool IsValidReplayUrl(string? replayUrl)
        {
            if (string.IsNullOrWhiteSpace(replayUrl)) return false;
            if (!Uri.TryCreate(replayUrl, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
