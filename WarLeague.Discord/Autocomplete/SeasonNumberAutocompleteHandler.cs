using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using WarLeague.Core.Repositories;

namespace WarLeague.Discord.Autocomplete;

public class SeasonNumberAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        const int maxSuggestions = 25;
        string userInput = autocompleteInteraction.Data.Current.Value?.ToString()?.Trim() ?? "";

        InitializeGuildContextAttribute.SetGuildIdFromContext(context, services);
        var seasonRepository = services.GetRequiredService<SeasonRepository>();
        var formatRepository = services.GetRequiredService<FormatRepository>();
        var helperService = services.GetRequiredService<DiscordApiHelperService>();

        int? formatId = null;
        if (context is SocketInteractionContext socketContext && context.Channel is SocketTextChannel channel && channel.Category is not null)
        {
            var (isSingle, format) = await formatRepository.GetSingleFormatModeFormatAsync();
            if (isSingle && format is not null)
                formatId = format.Id;
            else
            {
                var formatTmp = await helperService.GetFormatByCategoryNameAsync(socketContext);
                formatId = formatTmp.Id;
            }
        }

        if (formatId is null)
            return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());

        var seasons = await seasonRepository.GetAllByFormatAsync(formatId.Value);
        var filtered = string.IsNullOrWhiteSpace(userInput)
            ? seasons
            : seasons.Where(s => s.SeasonNumber.ToString().StartsWith(userInput, StringComparison.OrdinalIgnoreCase));
        var suggestions = filtered
            .OrderByDescending(s => s.SeasonNumber)
            .Take(maxSuggestions)
            .Select(s => new AutocompleteResult($"Season {s.SeasonNumber}", s.SeasonNumber.ToString()));
        return AutocompletionResult.FromSuccess(suggestions);
    }
}
