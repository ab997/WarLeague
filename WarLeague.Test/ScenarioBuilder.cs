using Shouldly;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Core.Services;
using WarLeague.Data.Entities;

namespace WarLeague.Test;

/// <summary>
/// Fluent, async-chainable builder for composing scenario state.
/// Each method returns Task&lt;ScenarioBuilder&gt; so steps can be chained via extension methods.
/// </summary>
public class ScenarioBuilder
{
    private readonly FormatService _formatService;
    private readonly SeasonService _seasonService;
    private readonly ConferenceService _conferenceService;
    private readonly TeamService _teamService;
    private readonly WeekService _weekService;
    private readonly DeckSubmissionService _deckSubmissionService;
    private readonly PlayerRepository _playerRepository;
    private readonly TeamRepository _teamRepository;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;

    private ulong _nextDiscordId = 1;

    public int FormatId { get; private set; }
    public string FormatName { get; private set; } = "";
    public int SeasonId { get; private set; }
    public string CurrentConference { get; private set; } = "";
    public int CurrentWeekNumber { get; private set; }
    public List<int> TeamIds { get; } = new();
    public List<int> CaptainIds { get; } = new();
    public BaseResult? LastResult { get; private set; }

    public ScenarioBuilder(
        FormatService formatService,
        SeasonService seasonService,
        ConferenceService conferenceService,
        TeamService teamService,
        WeekService weekService,
        DeckSubmissionService deckSubmissionService,
        PlayerRepository playerRepository,
        TeamRepository teamRepository,
        PlayerSeasonTeamRepository playerSeasonTeamRepository)
    {
        _formatService = formatService;
        _seasonService = seasonService;
        _conferenceService = conferenceService;
        _teamService = teamService;
        _weekService = weekService;
        _deckSubmissionService = deckSubmissionService;
        _playerRepository = playerRepository;
        _teamRepository = teamRepository;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
    }

    #region Format

    public async Task<ScenarioBuilder> CreateFormat(string? name = null)
    {
        FormatName = name ?? $"Format_{Guid.NewGuid():N}";
        var result = await _formatService.CreateFormatAsync(FormatName);
        result.Success.ShouldBeTrue(result.Message);
        var format = await _formatService.GetFormatAsync(FormatName);
        format.ShouldNotBeNull();
        FormatId = format!.Id;
        return this;
    }

    #endregion

    #region Season

    public async Task<ScenarioBuilder> WithSeason(int seasonNumber = 1, int minTeamMembers = 4)
    {
        var createResult = await _seasonService.CreateAsync(FormatId, seasonNumber, minTeamMembers);
        createResult.Success.ShouldBeTrue(createResult.Message);
        var activateResult = await _seasonService.SetActiveAsync(FormatId, seasonNumber);
        activateResult.Success.ShouldBeTrue(activateResult.Message);
        SeasonId = activateResult.Season!.Id;
        return this;
    }

    #endregion

    #region Conference

    public async Task<ScenarioBuilder> WithConference(string name, int playoffTeams = 1)
    {
        var result = await _conferenceService.CreateAsync(SeasonId, name, playoffTeams);
        result.Success.ShouldBeTrue(result.Message);
        CurrentConference = name;
        return this;
    }

    #endregion

    #region Teams

    public async Task<ScenarioBuilder> WithTeams(int count, string? conferenceName = null)
    {
        var conference = conferenceName ?? CurrentConference;
        for (int i = 0; i < count; i++)
        {
            var captain = new Player
            {
                DiscordUserId = _nextDiscordId++,
                UserName = $"Captain_{TeamIds.Count + 1}"
            };
            await _playerRepository.AddAsync(captain);
            CaptainIds.Add(captain.Id);

            var teamName = $"Team_{TeamIds.Count + 1}";
            var result = await _teamService.CreateAsync(
                SeasonId, teamName, captain.Id, conference,
                canBypassTeamModificationCheck: true);
            result.Success.ShouldBeTrue(result.Message);

            var team = await _teamRepository.GetByNameAndSeasonAsync(teamName, SeasonId);
            team.ShouldNotBeNull();
            TeamIds.Add(team!.Id);
        }
        return this;
    }

    #endregion

    #region Week

    public async Task<ScenarioBuilder> WithWeek(int weekNumber = 1, int submissionsRequired = 1)
    {
        var result = await _weekService.CreateAsync(
            SeasonId, weekNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, submissionsRequired);
        result.Success.ShouldBeTrue(result.Message);
        CurrentWeekNumber = weekNumber;
        return this;
    }

    public async Task<ScenarioBuilder> OpenWeek()
    {
        var result = await _weekService.TransitionToOpenWeekAsync(SeasonId, CurrentWeekNumber);
        result.Success.ShouldBeTrue(result.Message);
        return this;
    }

    public async Task<ScenarioBuilder> CloseSubmissions()
    {
        LastResult = await _weekService.TransitionToCloseSubmissionsAsync(SeasonId);
        LastResult.Success.ShouldBeTrue(LastResult.Message);
        return this;
    }

    /// <summary>
    /// Attempts to close submissions without asserting success.
    /// Use for failure scenarios — inspect LastResult afterwards.
    /// </summary>
    public async Task<ScenarioBuilder> TryCloseSubmissions()
    {
        LastResult = await _weekService.TransitionToCloseSubmissionsAsync(SeasonId);
        return this;
    }

    #endregion

    #region Deck submissions

    /// <summary>
    /// Every captain submits a deck for seat 1..submissionsRequired.
    /// Requires the week to be Open.
    /// </summary>
    public async Task<ScenarioBuilder> SubmitDecksForAllTeams(int submissionsPerTeam = 1)
    {
        foreach (var teamId in TeamIds)
        {
            var playerIds = await _playerSeasonTeamRepository.GetPlayerIdsByTeamAndSeasonAsync(teamId, SeasonId);
            for (int seat = 1; seat <= submissionsPerTeam; seat++)
            {
                var playerId = playerIds[seat - 1];
                var result = await _deckSubmissionService.SubmitAsync(SeasonId, playerId, $"deck_{playerId}_seat{seat}", seat);
                result.Success.ShouldBeTrue(result.Message);
            }
        }
        return this;
    }

    /// <summary>
    /// Submit decks only for specific teams (by index into TeamIds).
    /// Use for partial-submission scenarios.
    /// </summary>
    public async Task<ScenarioBuilder> SubmitDecksForTeams(int[] teamIndices, int submissionsPerTeam = 1)
    {
        foreach (var idx in teamIndices)
        {
            var teamId = TeamIds[idx];
            var playerIds = await _playerSeasonTeamRepository.GetPlayerIdsByTeamAndSeasonAsync(teamId, SeasonId);
            for (int seat = 1; seat <= submissionsPerTeam; seat++)
            {
                var playerId = playerIds[seat - 1];
                var result = await _deckSubmissionService.SubmitAsync(SeasonId, playerId, $"deck_{playerId}_seat{seat}", seat);
                result.Success.ShouldBeTrue(result.Message);
            }
        }
        return this;
    }

    #endregion
}

/// <summary>
/// Async extension methods that enable fluent chaining on Task&lt;ScenarioBuilder&gt;.
/// </summary>
public static class ScenarioBuilderExtensions
{
    public static async Task<ScenarioBuilder> CreateFormat(this Task<ScenarioBuilder> task, string? name = null)
        => await (await task).CreateFormat(name);

    public static async Task<ScenarioBuilder> WithSeason(this Task<ScenarioBuilder> task, int seasonNumber = 1, int minTeamMembers = 4)
        => await (await task).WithSeason(seasonNumber, minTeamMembers);

    public static async Task<ScenarioBuilder> WithConference(this Task<ScenarioBuilder> task, string name, int playoffTeams = 1)
        => await (await task).WithConference(name, playoffTeams);

    public static async Task<ScenarioBuilder> WithTeams(this Task<ScenarioBuilder> task, int count, string? conferenceName = null)
        => await (await task).WithTeams(count, conferenceName);

    public static async Task<ScenarioBuilder> WithWeek(this Task<ScenarioBuilder> task, int weekNumber = 1, int submissionsRequired = 1)
        => await (await task).WithWeek(weekNumber, submissionsRequired);

    public static async Task<ScenarioBuilder> OpenWeek(this Task<ScenarioBuilder> task)
        => await (await task).OpenWeek();

    public static async Task<ScenarioBuilder> CloseSubmissions(this Task<ScenarioBuilder> task)
        => await (await task).CloseSubmissions();

    public static async Task<ScenarioBuilder> TryCloseSubmissions(this Task<ScenarioBuilder> task)
        => await (await task).TryCloseSubmissions();

    public static async Task<ScenarioBuilder> SubmitDecksForAllTeams(this Task<ScenarioBuilder> task, int submissionsPerTeam = 1)
        => await (await task).SubmitDecksForAllTeams(submissionsPerTeam);

    public static async Task<ScenarioBuilder> SubmitDecksForTeams(this Task<ScenarioBuilder> task, int[] teamIndices, int submissionsPerTeam = 1)
        => await (await task).SubmitDecksForTeams(teamIndices, submissionsPerTeam);
}
