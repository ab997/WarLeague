
using Discord;
using Discord.Interactions;
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
            await DeferAsync(ephemeral: true);

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

            await FollowupAsync($"Format created.");
        }

        [SlashCommand("delete", "Deletes format")]
        public async Task DeleteAsync(string formatName)
        {
            await DeferAsync(ephemeral: true);

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
            await DeferAsync(ephemeral: true);

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
