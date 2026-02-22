using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using WarLeague.Data.Data;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using WarLeague.Core.Repositories;

namespace WarLeague.Discord.Autocomplete;

public class TeamAutocompleteHandler : AutocompleteHandler
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
        var teamRepository = services.GetRequiredService<TeamRepository>();
        var helperService = services.GetRequiredService<DiscordApiHelperService>();

        if (context is not SocketInteractionContext socketContext || context.Channel is not SocketTextChannel channel || channel.Category is null)
            return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());

        try
        {
            var season = await helperService.GetSeasonByCategoryNameAsync(socketContext);
            var teams = await teamRepository.GetBySeasonAndNamePrefixAsync(season.Id, userInput, maxSuggestions);
            var suggestions = teams
                .Select(t => new AutocompleteResult(t.Name, t.Name))
                .Take(maxSuggestions);
            return AutocompletionResult.FromSuccess(suggestions);
        }
        catch
        {
            return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());
        }
    }
}
