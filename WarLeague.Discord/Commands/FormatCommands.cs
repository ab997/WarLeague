

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Text.Json;
using WarLeague.Core.Model;
using WarLeague.Core.Services;
using WarLeague.Data.Data.Enums;
using WarLeague.Discord.Helpers;
using WarLeague.Discord.Preconditions;
using Format = WarLeague.Data.Entities.Format;

namespace WarLeague.Discord.Commands
{
    [Group("format", "Format commands")]
    [RequireAppPermission(PermissionType.Admin)]
    [InitializeGuildContext]
    public class FormatCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly FormatService _formatService;
        private readonly HttpClient _httpClient;
        public FormatCommands(FormatService service, HttpClient httpClient)
        {
            _formatService = service;
            _httpClient = httpClient;
        }
        [SlashCommand("create", "Creates a new format")]
        public async Task CreateAsync(string formatName)
        {
            await DeferAsync(ephemeral: false);

            BaseResult result = await _formatService.CreateFormatAsync(formatName);

            if (!result.Success)
            {
                await FollowupAsync(ResultHelper.Stringify(result));
                return;
            }

            string message=await CreateFormatCategoryAndChannelAsync(formatName, Context.Guild);
            await FollowupAsync(message);
        }

        [SlashCommand("single-format-mode", "Enable single format mode for entire server.")]
        public async Task SingleFormatModeAsync(string formatName)
        {
            await DeferAsync(ephemeral: false);

            Format? format = await _formatService.GetFormatAsync(formatName);

            if (format is null)
            {
                await FollowupAsync($"Format with name {formatName} does not exists.");
                return;
            }

            BaseResult result = await _formatService.SetSingleFormatModeAsync(format.Id);

            await FollowupAsync(ResultHelper.Stringify(result));
        }



        [SlashCommand("delete", "Deletes format")]
        public async Task DeleteAsync(string formatName)
        {
            await DeferAsync(ephemeral: false);

            BaseResult result = await _formatService.DeleteFormatAsync(formatName);

            await FollowupAsync(ResultHelper.Stringify(result));
        }

        [SlashCommand("update-rules", "Update format rules from a .json file")]
        public async Task UpdateRulesAsync(
           [Summary("format", "Format name")] string formatName,
           [Summary("rules-file", "JSON file containing rules")] IAttachment rulesFile)
        {
            await DeferAsync(ephemeral: false);

            if (rulesFile == null)
            {
                await FollowupAsync("No file provided. Please attach a .json file.");
                return;
            }

            if (!rulesFile.Filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                await FollowupAsync("Please upload a file with a .json extension.");
                return;
            }

            string json = "";

            try
            {
                var jsonContent = await _httpClient.GetStringAsync(rulesFile.Url);

                // Validate JSON
                using var doc = JsonDocument.Parse(jsonContent);

                // Re-serialize to normalized/minified JSON to store consistently
                json = JsonSerializer.Serialize(doc.RootElement);
            }
            catch (JsonException)
            {
                await FollowupAsync("Provided file is not valid JSON.");
            }
            catch (HttpRequestException ex)
            {
                await FollowupAsync($"Failed to download the file: {ex.Message}");
            }
            catch (Exception ex)
            {
                await FollowupAsync($"Unexpected error: {ex.Message}");
            }

            BaseResult result = await _formatService.UpdateFormatRulesAsync(formatName, json);
            await FollowupAsync(ResultHelper.Stringify(result));
        }

        private async Task<string> CreateFormatCategoryAndChannelAsync(string formatName, SocketGuild guild)
        {
            // Normalize channel name if not provided
            string channelName = formatName.ToLowerInvariant().Replace(' ', '-');

            // Optionally verify bot permissions
            if (!guild.CurrentUser.GuildPermissions.ManageChannels)
            {
                return "I don't have permission to manage channels. Grant the bot Manage Channels permission.";
            }
            // Find or create category
            var category = guild.CategoryChannels
                .FirstOrDefault(c => string.Equals(c.Name, formatName, StringComparison.OrdinalIgnoreCase));

            ulong categoryId = 0;
            if (category == null)
            {
                var newCategory = await guild.CreateCategoryChannelAsync(formatName);
                categoryId = newCategory.Id;
                category = guild.GetCategoryChannel(categoryId);
            }

            // If a channel with the same name already exists under that category, return early
            var existing = guild.TextChannels
                .FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase) && c.CategoryId == category.Id);

            if (existing != null)
            {
                return $"Channel '{existing.Mention}' already exists under category '{category.Name}'.";
            }

            // Create the text channel and assign it to the category
            var channel = await guild.CreateTextChannelAsync(channelName, props =>
            {
                props.Topic = $"Channel for {formatName}";
                props.CategoryId = category.Id;
            });

            return $"Format {formatName} created.";
        }
    }
}
