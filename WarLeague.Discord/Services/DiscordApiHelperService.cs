using Discord.Interactions;
using Discord;
using Discord.WebSocket;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;
using Format = WarLeague.Core.Data.Entities.Format;

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

        /// <summary>
        /// Sends embeds in batches to respect Discord's limit of up to 10 embeds per message.
        /// </summary>
        public async Task SendEmbedsInBatchesAsync(SocketInteractionContext context, IReadOnlyList<Embed> embeds)
        {
            if (embeds == null || embeds.Count == 0)
            {
                await context.Interaction.FollowupAsync("Nothing to show.");
                return;
            }

            const int batchSize = 10;
            for (int i = 0; i < embeds.Count; i += batchSize)
            {
                Embed[] batch = embeds.Skip(i).Take(batchSize).ToArray();
                await context.Interaction.FollowupAsync(embeds: batch);
            }
        }

        /// <summary>
        /// Splits a long text into chunks and sends multiple follow-up messages to avoid message length limits.
        /// </summary>
        public async Task SendMessageInChunksAsync(SocketInteractionContext context, string text, int maxChunkSize = 1800)
        {
            if (string.IsNullOrEmpty(text))
            {
                await context.Interaction.FollowupAsync("_<empty>_");
                return;
            }

            // Discord max message length is ~2000; keep margin for safety
            if (maxChunkSize < 500) maxChunkSize = 500;
            if (maxChunkSize > 1900) maxChunkSize = 1900;

            int start = 0;
            while (start < text.Length)
            {
                int len = Math.Min(maxChunkSize, text.Length - start);
                int end = start + len;

                // Prefer to break on newline to keep structure
                int lastNewLine = text.LastIndexOf('\n', end - 1, len);
                if (lastNewLine > start)
                {
                    end = lastNewLine + 1;
                }

                var chunk = text.Substring(start, end - start).TrimEnd();
                if (string.IsNullOrWhiteSpace(chunk))
                {
                    chunk = "…";
                }

                await context.Interaction.FollowupAsync(chunk);
                start = end;
            }
        }
    }
}
