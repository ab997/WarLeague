using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using WarLeague.Core.Repositories;

namespace WarLeague.Discord.Autocomplete;

public class WeekNumberAutocompleteHandler : AutocompleteHandler
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
        var weekRepository = services.GetRequiredService<WeekRepository>();
        var helperService = services.GetRequiredService<DiscordApiHelperService>();

        if (context is not SocketInteractionContext socketContext || context.Channel is not SocketTextChannel channel || channel.Category is null)
            return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());

        try
        {
            var season = await helperService.GetSeasonByCategoryNameAsync(socketContext);
            var weeks = await weekRepository.GetBySeasonAsync(season.Id);
            var filtered = string.IsNullOrWhiteSpace(userInput)
                ? weeks
                : weeks.Where(w => w.WeekNumber.ToString().StartsWith(userInput, StringComparison.OrdinalIgnoreCase));
            var suggestions = filtered
                .OrderBy(w => w.WeekNumber)
                .Take(maxSuggestions)
                .Select(w => new AutocompleteResult($"Week {w.WeekNumber}", w.WeekNumber.ToString()));
            return AutocompletionResult.FromSuccess(suggestions);
        }
        catch
        {
            return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());
        }
    }
}
