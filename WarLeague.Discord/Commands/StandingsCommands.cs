using Discord.Interactions;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;
using WarLeague.Core.Services;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Helpers;
using WarLeague.Discord.Autocomplete;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using WarLeague.Data.Data.Enums;

namespace WarLeague.Discord.Commands;

[Group("standings", "Playoff standings (tiebreaker ordering)")]
[RequireAppPermission(PermissionType.Admin)]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
[InitializeGuildContext]
public class StandingsCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TeamStandingsService _teamStandingsService;
    private readonly DiscordApiHelperService _helperService;
    private readonly TeamRepository _teamRepository;

    public StandingsCommands(
        TeamStandingsService teamStandingsService,
        DiscordApiHelperService helperService,
        TeamRepository teamRepository)
    {
        _teamStandingsService = teamStandingsService;
        _helperService = helperService;
        _teamRepository = teamRepository;
    }

    [SlashCommand("list", "List playoff standings (position, team, tiebreaker, wins) for the active season")]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        var standings = await _teamStandingsService.GetStandingsForSeasonAsync(season.Id);

        if (standings.Count == 0)
        {
            await FollowupAsync("No playoff standings for this season. Use `/season switch-to-playoffs` first, or `/standings generate` to populate from round-robin results.");
            return;
        }

        var lines = standings
            .Select((s, i) => $"**{i + 1}.** {s.Team.Name} — Tiebreaker: {s.Tiebreaker}" + (s.Wins.HasValue ? $", Wins: {s.Wins}" : ""));
        var message = "**Playoff standings**\n" + string.Join("\n", lines);
        if (message.Length > 1900)
            message = message[..1900] + "...";
        await FollowupAsync(message);
    }

    [SlashCommand("set-tiebreaker", "Set a team's tiebreaker value (used for ordering).")]
    public async Task SetTiebreakerAsync(
        [Summary("team", "Team to update")] [Autocomplete(typeof(TeamAutocompleteHandler))] string teamName,
        [Summary("tiebreaker", "Tiebreaker value (e.g. round-robin wins)")] int tiebreaker)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        var team = await _teamRepository.GetByNameAndSeasonAsync(teamName, season.Id);
        if (team == null)
        {
            await FollowupAsync($"Team '{teamName}' not found in this season.");
            return;
        }

        BaseResult result = await _teamStandingsService.UpdateTiebreakerAsync(season.Id, team.Id, tiebreaker);
        await FollowupAsync(ResultHelper.Stringify(result));
    }

    [SlashCommand("generate", "Regenerate playoff standings from round-robin results (overwrites current tiebreakers).")]
    public async Task GenerateAsync()
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        BaseResult result = await _teamStandingsService.GenerateStandingsFromRoundRobinAsync(season.Id);
        await FollowupAsync(ResultHelper.Stringify(result));
    }
}
