using Discord.Interactions;
using WarLeague.Core.Model;
using WarLeague.Core.Services;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;
using WarLeague.Discord.Helpers;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("conference", "Conference commands")]
[RequireAppPermission(PermissionType.Admin)]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
[InitializeGuildContext]
public class ConferenceCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ConferenceService _conferenceService;
    private readonly DiscordApiHelperService _helperService;

    public ConferenceCommands(ConferenceService conferenceService, DiscordApiHelperService helperService)
    {
        _conferenceService = conferenceService;
        _helperService = helperService;
    }

    [SlashCommand("create", "Creates a conference in the active season")]
    public async Task CreateAsync([Summary("name", "Conference name")] string name)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _conferenceService.CreateAsync(season.Id, name);

        await FollowupAsync(ResultHelper.Stringify(result));
    }

    [SlashCommand("update", "Renames a conference in the active season")]
    public async Task UpdateAsync(
        [Summary("current-name", "Current conference name")] string currentName,
        [Summary("new-name", "New conference name")] string newName)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _conferenceService.UpdateAsync(season.Id, currentName, newName);

        await FollowupAsync(ResultHelper.Stringify(result));
    }

    [SlashCommand("delete", "Deletes a conference in the active season")]
    public async Task DeleteAsync([Summary("name", "Conference name")] string name)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _conferenceService.DeleteAsync(season.Id, name);

        await FollowupAsync(ResultHelper.Stringify(result));
    }

    [SlashCommand("list", "Lists conferences in the active season")]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        BaseResult result = await _conferenceService.ListAsync(season.Id);

        await FollowupAsync(ResultHelper.Stringify(result));
    }
}
