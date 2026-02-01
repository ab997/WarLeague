
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Net.Http;
using System.Text.Json;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;
using Format = WarLeague.Core.Data.Entities.Format;

namespace WarLeague.Discord.Commands
{
    [Group("format", "Format commands")]
    [RequireRole("Admin")]
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

            Format? format = await _formatService.CreateFormatAsync(formatName);

            if (format is null)
            {
                await FollowupAsync($"Format with name {formatName} already exists.");
                return;
            }

            await CreateFormatCategoryAndChannelAsync(formatName, Context.Guild);
        }

       

        [SlashCommand("delete", "Deletes format")]
        public async Task DeleteAsync(string formatName)
        {
            await DeferAsync(ephemeral: false);

            var format = await _formatService.DeleteFormatAsync(formatName);

            if (format == null)
            {
                await FollowupAsync($"Format with name {formatName} not found.");
                return;
            }

            await FollowupAsync($"Format '{formatName}' deleted.");
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

            var format = await _formatService.UpdateFormatRulesAsync(formatName, json);
            if (format == null)
            {
                await FollowupAsync($"Format with name {formatName} not found.");
                return;
            }
            await FollowupAsync($"Rules updated for format '{formatName}'.");
        }

        private async Task CreateFormatCategoryAndChannelAsync(string formatName, SocketGuild guild)
        {
            // Normalize channel name if not provided
            string channelName = formatName.ToLowerInvariant().Replace(' ', '-');

            // Optionally verify bot permissions
            if (!guild.CurrentUser.GuildPermissions.ManageChannels)
            {
                await FollowupAsync("I don't have permission to manage channels. Grant the bot Manage Channels permission.");
                return;
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
                await FollowupAsync($"Channel '{existing.Mention}' already exists under category '{category.Name}'.");
                return;
            }

            // Create the text channel and assign it to the category
            var channel = await guild.CreateTextChannelAsync(channelName, props =>
            {
                props.Topic = $"Channel for {formatName}";
                props.CategoryId = category.Id;
            });


            await FollowupAsync($"Format {formatName} created.");
        }
    }
}
