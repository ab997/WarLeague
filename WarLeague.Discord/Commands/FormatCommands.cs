
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Text.Json;
using WarLeague.Core.Repositories;
using Format = WarLeague.Core.Data.Entities.Format;

namespace WarLeague.Discord.Commands
{
    [Group("format", "Format commands")]
    [RequireRole("Admin")]
    public class FormatCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly FormatRepository _formatRepository;
        public FormatCommands(FormatRepository formatRepository)
        {
            _formatRepository = formatRepository;
        }
        [SlashCommand("create", "Creates a new format")]
        public async Task CreateAsync(string formatName)
        {
            await DeferAsync(ephemeral: false);

            Format? format = await _formatRepository.GetByNameAsync(formatName);

            if (format != null)
            {
                await FollowupAsync($"Format with name {formatName} already exists.");
                return;
            }

            await _formatRepository.AddAsync(new Format
            {
                Name = formatName
            });

            var guild = Context.Guild as SocketGuild;
            if (guild == null)
            {
                await FollowupAsync("This command can only be used inside a guild.");
                return;
            }

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

        [SlashCommand("delete", "Deletes format")]
        public async Task DeleteAsync(string formatName)
        {
            await DeferAsync(ephemeral: false);

            var format = await _formatRepository.GetByNameAsync(formatName);
            if (format == null)
            {
                await FollowupAsync($"Format with name {formatName} not found.");
                return;
            }

            await _formatRepository.DeleteAsync(format);
            await FollowupAsync($"Format '{formatName}' deleted.");
        }

     

        [SlashCommand("update-rules", "Update format rules from a .json file")]
        public async Task UpdateRulesAsync(
           [Summary("format", "Format name")] string formatName,
           [Summary("rules-file", "JSON file containing rules")] IAttachment rulesFile)
        {
            await DeferAsync(ephemeral: false);

            var format = await _formatRepository.GetByNameAsync(formatName);
            if (format == null)
            {
                await FollowupAsync($"Format with name {formatName} not found.");
                return;
            }

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

            try
            {
                using var http = new HttpClient();
                var jsonContent = await http.GetStringAsync(rulesFile.Url);

                // Validate JSON
                using var doc = JsonDocument.Parse(jsonContent);

                // Re-serialize to normalized/minified JSON to store consistently
                var normalizedJson = JsonSerializer.Serialize(doc.RootElement);

                format.Rules = normalizedJson;
                await _formatRepository.UpdateAsync(format);

                await FollowupAsync($"Rules updated for format '{formatName}'.");
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
        }
    }
}
