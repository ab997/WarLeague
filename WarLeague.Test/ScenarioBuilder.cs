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
    private readonly MatchService _matchService;
    private readonly MatchupServiceFactory _matchupServiceFactory;
    private readonly SubstitutionService _substitutionService;
    private readonly PlayerRepository _playerRepository;
    private readonly TeamRepository _teamRepository;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    private readonly SeasonRepository _seasonRepository;

    private ulong _nextDiscordId = 1;

    public int FormatId { get; private set; }
    public string FormatName { get; private set; } = "";
    public int SeasonId { get; private set; }
    public string CurrentConference { get; private set; } = "";
    public int CurrentWeekNumber { get; private set; }
    public List<int> TeamIds { get; } = new();
    public List<string> TeamNames { get; } = new();
    public List<int> CaptainIds { get; } = new();
    public List<Match> Matches { get; } = new();
    public int TotalRoundsPlayed { get; private set; }
    public (int PlayerOutId, int PlayerInId)? LastSubstitution { get; private set; }
    public BaseResult? LastResult { get; private set; }

    public ScenarioBuilder(
        FormatService formatService,
        SeasonService seasonService,
        ConferenceService conferenceService,
        TeamService teamService,
        WeekService weekService,
        DeckSubmissionService deckSubmissionService,
        MatchService matchService,
        MatchupServiceFactory matchupServiceFactory,
        SubstitutionService substitutionService,
        PlayerRepository playerRepository,
        TeamRepository teamRepository,
        PlayerSeasonTeamRepository playerSeasonTeamRepository,
        SeasonRepository seasonRepository)
    {
        _formatService = formatService;
        _seasonService = seasonService;
        _conferenceService = conferenceService;
        _teamService = teamService;
        _weekService = weekService;
        _deckSubmissionService = deckSubmissionService;
        _matchService = matchService;
        _matchupServiceFactory = matchupServiceFactory;
        _substitutionService = substitutionService;
        _playerRepository = playerRepository;
        _teamRepository = teamRepository;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _seasonRepository = seasonRepository;
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

    public async Task<ScenarioBuilder> SetPhaseToPlayoffs()
    {
        LastResult = await _seasonService.SetPhaseToPlayoffsAsync(SeasonId);
        LastResult.Success.ShouldBeTrue(LastResult.Message);
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
            TeamNames.Add(teamName);
        }
        return this;
    }

    /// <summary>
    /// Adds extra (non-captain) players to every team so each team has the given total.
    /// Call after WithTeams. Skips teams that already have enough members.
    /// </summary>
    public async Task<ScenarioBuilder> WithPlayersPerTeam(int totalPerTeam)
    {
        foreach (var teamId in TeamIds)
        {
            var existing = await _playerSeasonTeamRepository.GetPlayerIdsByTeamAndSeasonAsync(teamId, SeasonId);
            for (int i = existing.Count; i < totalPerTeam; i++)
            {
                var player = new Player
                {
                    DiscordUserId = _nextDiscordId++,
                    UserName = $"Player_T{teamId}_M{i + 1}"
                };
                await _playerRepository.AddAsync(player);
                var result = await _teamService.AddMemberAsync(
                    SeasonId, player.Id, teamId, canBypassTeamModificationCheck: true);
                result.Success.ShouldBeTrue(result.Message);
            }
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

    public async Task<ScenarioBuilder> MoveToInProgress()
    {
        var result = await _weekService.TransitionToInProgressAsync(SeasonId);
        result.Success.ShouldBeTrue(result.Message);
        Matches.Clear();
        Matches.AddRange(result.CreatedMatches ?? []);
        return this;
    }

    public async Task<ScenarioBuilder> CompleteWeek()
    {
        LastResult = await _weekService.TransitionToCompletedAsync(SeasonId);
        LastResult.Success.ShouldBeTrue(LastResult.Message);
        return this;
    }

    /// <summary>
    /// Attempts to complete the week without asserting success.
    /// Use for failure scenarios — inspect LastResult afterwards.
    /// </summary>
    public async Task<ScenarioBuilder> TryCompleteWeek()
    {
        LastResult = await _weekService.TransitionToCompletedAsync(SeasonId);
        return this;
    }

    /// <summary>
    /// Runs a full round-robin season: queries suggested rounds, then for each week
    /// runs the complete happy path (create → open → submit → close → in-progress → report → complete).
    /// </summary>
    public async Task<ScenarioBuilder> PlayFullRoundRobin(int submissionsPerTeam = 1)
    {
        var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(SeasonId);
        var matchupService = _matchupServiceFactory.GetMatchupService(season);
        var suggestion = await matchupService.GetSuggestedRoundsAsync(SeasonId);
        suggestion.ShouldNotBeNull("GetSuggestedRoundsAsync returned null — need at least 2 teams in a conference.");
        suggestion!.TotalSuggestedWeeks.ShouldBeGreaterThan(0);

        TotalRoundsPlayed = suggestion.TotalSuggestedWeeks;

        for (int week = 1; week <= TotalRoundsPlayed; week++)
        {
            await WithWeek(week, submissionsPerTeam);
            await OpenWeek();
            await SubmitDecksForAllTeams(submissionsPerTeam);
            await CloseSubmissions();
            await MoveToInProgress();
            await ReportAllMatchResults();
            await CompleteWeek();
        }

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

    #region Match reporting

    /// <summary>
    /// Reports Player1 as the winner for every generated match.
    /// Requires the week to be InProgress (call MoveToInProgress first).
    /// </summary>
    public async Task<ScenarioBuilder> ReportAllMatchResults()
    {
        foreach (var match in Matches)
        {
            var result = await _matchService.ReportWinAsync(
                SeasonId, match.Player1Id, $"https://example.com/replay/{match.Id}");
            result.Success.ShouldBeTrue(result.Message);
        }
        return this;
    }

    /// <summary>
    /// Reports Player1 as the winner only for specific matches (by index into Matches).
    /// Use for partial-reporting scenarios.
    /// </summary>
    public async Task<ScenarioBuilder> ReportMatchResults(int[] matchIndices)
    {
        foreach (var idx in matchIndices)
        {
            var match = Matches[idx];
            var result = await _matchService.ReportWinAsync(
                SeasonId, match.Player1Id, $"https://example.com/replay/{match.Id}");
            result.Success.ShouldBeTrue(result.Message);
        }
        return this;
    }

    #endregion

    #region Substitution

    /// <summary>
    /// Substitutes a bench player in for a matched player on a specific team.
    /// Requires InProgress week (call MoveToInProgress first).
    /// The bench player is the first team member not in any current match.
    /// </summary>
    public async Task<ScenarioBuilder> SubstitutePlayer(int teamIndex, int playerOutSeat)
    {
        var teamId = TeamIds[teamIndex];
        var teamName = TeamNames[teamIndex];
        var playerIds = await _playerSeasonTeamRepository.GetPlayerIdsByTeamAndSeasonAsync(teamId, SeasonId);

        int playerOutId = playerIds[playerOutSeat - 1];

        var playingPlayerIds = Matches
            .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
            .ToHashSet();

        int playerInId = playerIds.First(id => !playingPlayerIds.Contains(id));

        var result = await _substitutionService.SubstitutePlayerAsync(SeasonId, teamName, playerInId, playerOutId);
        result.Success.ShouldBeTrue(result.Message);

        LastSubstitution = (playerOutId, playerInId);
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

    public static async Task<ScenarioBuilder> SetPhaseToPlayoffs(this Task<ScenarioBuilder> task)
        => await (await task).SetPhaseToPlayoffs();

    public static async Task<ScenarioBuilder> WithConference(this Task<ScenarioBuilder> task, string name, int playoffTeams = 1)
        => await (await task).WithConference(name, playoffTeams);

    public static async Task<ScenarioBuilder> WithTeams(this Task<ScenarioBuilder> task, int count, string? conferenceName = null)
        => await (await task).WithTeams(count, conferenceName);

    public static async Task<ScenarioBuilder> WithPlayersPerTeam(this Task<ScenarioBuilder> task, int totalPerTeam)
        => await (await task).WithPlayersPerTeam(totalPerTeam);

    public static async Task<ScenarioBuilder> WithWeek(this Task<ScenarioBuilder> task, int weekNumber = 1, int submissionsRequired = 1)
        => await (await task).WithWeek(weekNumber, submissionsRequired);

    public static async Task<ScenarioBuilder> OpenWeek(this Task<ScenarioBuilder> task)
        => await (await task).OpenWeek();

    public static async Task<ScenarioBuilder> CloseSubmissions(this Task<ScenarioBuilder> task)
        => await (await task).CloseSubmissions();

    public static async Task<ScenarioBuilder> TryCloseSubmissions(this Task<ScenarioBuilder> task)
        => await (await task).TryCloseSubmissions();

    public static async Task<ScenarioBuilder> MoveToInProgress(this Task<ScenarioBuilder> task)
        => await (await task).MoveToInProgress();

    public static async Task<ScenarioBuilder> CompleteWeek(this Task<ScenarioBuilder> task)
        => await (await task).CompleteWeek();

    public static async Task<ScenarioBuilder> TryCompleteWeek(this Task<ScenarioBuilder> task)
        => await (await task).TryCompleteWeek();

    public static async Task<ScenarioBuilder> PlayFullRoundRobin(this Task<ScenarioBuilder> task, int submissionsPerTeam = 1)
        => await (await task).PlayFullRoundRobin(submissionsPerTeam);

    public static async Task<ScenarioBuilder> ReportAllMatchResults(this Task<ScenarioBuilder> task)
        => await (await task).ReportAllMatchResults();

    public static async Task<ScenarioBuilder> ReportMatchResults(this Task<ScenarioBuilder> task, int[] matchIndices)
        => await (await task).ReportMatchResults(matchIndices);

    public static async Task<ScenarioBuilder> SubstitutePlayer(this Task<ScenarioBuilder> task, int teamIndex, int playerOutSeat)
        => await (await task).SubstitutePlayer(teamIndex, playerOutSeat);

    public static async Task<ScenarioBuilder> SubmitDecksForAllTeams(this Task<ScenarioBuilder> task, int submissionsPerTeam = 1)
        => await (await task).SubmitDecksForAllTeams(submissionsPerTeam);

    public static async Task<ScenarioBuilder> SubmitDecksForTeams(this Task<ScenarioBuilder> task, int[] teamIndices, int submissionsPerTeam = 1)
        => await (await task).SubmitDecksForTeams(teamIndices, submissionsPerTeam);
}
