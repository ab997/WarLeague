using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Discord.Preconditions;
using WarLeague.Core.Repositories;

namespace WarLeague.Discord.Autocomplete;

public class FormatNameAutocompleteHandler : AutocompleteHandler
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
        var formatRepository = services.GetRequiredService<FormatRepository>();

        try
        {
            var formats = await formatRepository.GetByNamePrefixAsync(userInput, maxSuggestions);
            var suggestions = formats
                .Select(f => new AutocompleteResult(f.Name, f.Name))
                .Take(maxSuggestions);
            return AutocompletionResult.FromSuccess(suggestions);
        }
        catch
        {
            return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());
        }
    }
}
