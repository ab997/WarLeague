using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Services;

public class TeamStandingsService
{
    private readonly TiebreakerService _tiebreakerService;
    private readonly TeamStandingsRepository _teamStandingsRepository;
    private readonly RoundRobinMatchupRepository _roundRobinMatchupRepository;
    private readonly MatchRepository _matchRepository;
    private readonly WeekRepository _weekRepository;
    private readonly ConferenceRepository _conferenceRepository;
    private readonly TeamRepository _teamRepository;
    private readonly PlayoffMatchupRepository _playoffMatchupRepository;
    private readonly SeasonRepository _seasonRepository;
    private readonly PlayoffService _playoffService;

    public TeamStandingsService(
        TiebreakerService tiebreakerService,
        TeamStandingsRepository teamStandingsRepository,
        RoundRobinMatchupRepository roundRobinMatchupRepository,
        MatchRepository matchRepository,
        WeekRepository weekRepository,
        ConferenceRepository conferenceRepository,
        TeamRepository teamRepository,
        PlayoffMatchupRepository playoffMatchupRepository,
        SeasonRepository seasonRepository,
        PlayoffService playoffService)
    {
        _tiebreakerService = tiebreakerService;
        _teamStandingsRepository = teamStandingsRepository;
        _roundRobinMatchupRepository = roundRobinMatchupRepository;
        _matchRepository = matchRepository;
        _weekRepository = weekRepository;
        _conferenceRepository = conferenceRepository;
        _teamRepository = teamRepository;
        _playoffMatchupRepository = playoffMatchupRepository;
        _seasonRepository = seasonRepository;
        _playoffService = playoffService;
    }

    /// <summary>
    /// Gets round-robin standings from completed weeks, ordered by tiebreaker ranking.
    /// </summary>
    public async Task<List<RoundRobinStandingsEntry>> GetRoundRobinStandingsForDisplayAsync(int seasonId)
    {
        var teams = await _teamRepository.GetBySeasonAsync(seasonId);
        var matchups = await _roundRobinMatchupRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);
        var matches = await _matchRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);
        var conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
        var conferenceById = conferences.ToDictionary(c => c.Id);

        var result = _tiebreakerService.RankTeams(teams, matchups, matches);

        var entriesByTeamId = teams.ToDictionary(t => t.Id, t =>
        {
            var stats = result.StatsByTeamId.GetValueOrDefault(t.Id);
            return new RoundRobinStandingsEntry
            {
                TeamId = t.Id,
                TeamName = t.Name,
                ConferenceName = conferenceById.TryGetValue(t.ConferenceId, out var conf) ? conf.Name : "",
                Tiebreaker = result.TiebreakerByTeamId.GetValueOrDefault(t.Id, 0),
                Wins = stats?.Wins ?? 0,
                Losses = stats?.Losses ?? 0,
                SeriesDiff = stats?.SeriesDiff ?? 0,
                GameDiff = stats?.GameDiff ?? 0,
                SeriesPct = stats?.SeriesPct ?? 0,
                GamePct = stats?.GamePct ?? 0,
                SOS = stats?.SOS
            };
        });

        return result.OrderedTeams
            .Where(team => entriesByTeamId.ContainsKey(team.Id))
            .Select(team => entriesByTeamId[team.Id])
            .ToList();
    }

    /// <summary>
    /// Generates TeamStandings from round-robin results: computes wins per team,
    /// ranks all teams globally by centralized tiebreaker, and stores one row per team (Seed = global rank 1..N).
    /// Overwrites any existing standings for the season.
    /// </summary>
    public async Task<BaseResult> GenerateStandingsFromRoundRobinAsync(int seasonId)
    {
        var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);
        if (season.Phase != SeasonPhase.Playoffs)
        {
            return new BaseResult(false, "Standings can only be generated when season is in Playoffs phase.");
        }
        var existingPlayoffMatchups = await _playoffMatchupRepository.GetBySeasonIdAsync(seasonId);
        if (existingPlayoffMatchups.Count > 0)
        {
            return new BaseResult(false, "Cannot generate or regenerate standings: playoff matchups already exist for this season.");
        }

        var teams = await _teamRepository.GetBySeasonAsync(seasonId);
        var matchups = await _roundRobinMatchupRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);
        var matches = await _matchRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);

        var result = _tiebreakerService.RankTeams(teams, matchups, matches);
        var orderedTeams = result.OrderedTeams;
        var allTiebreakers = result.TiebreakerByTeamId;

        await _teamStandingsRepository.DeleteBySeasonIdAsync(seasonId);

        var standings = new List<TeamStandings>();
        for (var i = 0; i < orderedTeams.Count; i++)
        {
            var team = orderedTeams[i];
            var wins = result.StatsByTeamId.GetValueOrDefault(team.Id)?.Wins ?? 0;
            standings.Add(new TeamStandings
            {
                SeasonId = seasonId,
                TeamId = team.Id,
                Tiebreaker = allTiebreakers.GetValueOrDefault(team.Id, 0),
                Seed = i + 1,
                Wins = wins
            });
        }

        if (standings.Count > 0)
            await _teamStandingsRepository.AddRangeAsync(standings);

        return new BaseResult(true, $"Generated standings for {standings.Count} team(s).");
    }

    /// <summary>
    /// Returns standings for the season (for display). Only valid when season is in Playoffs.
    /// </summary>
    public async Task<List<TeamStandings>> GetStandingsForSeasonAsync(int seasonId)
    {
        return await _teamStandingsRepository.GetBySeasonIdAsync(seasonId);
    }

    /// <summary>
    /// Returns true if standings are editable: season is Playoffs and no playoff matchups exist yet.
    /// </summary>
    public async Task<bool> CanEditStandingsAsync(int seasonId)
    {
        var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);
        if (season.Phase != SeasonPhase.Playoffs)
            return false;
        var playoffMatchups = await _playoffMatchupRepository.GetBySeasonIdAsync(seasonId);
        return playoffMatchups.Count == 0;
    }

    public async Task<BaseResult> UpdateTiebreakerAsync(int seasonId, int teamId, int tiebreaker)
    {
        var canEdit = await CanEditStandingsAsync(seasonId);
        if (!canEdit)
            return new BaseResult(false, "Standings cannot be edited: season must be in Playoffs phase and no playoff matchups may exist yet.");

        var standings = await _teamStandingsRepository.GetBySeasonIdWithoutTeamAsync(seasonId);
        var entry = standings.FirstOrDefault(s => s.TeamId == teamId);
        if (entry == null)
            return new BaseResult(false, "Team is not in standings.");

        var playoffQualifierIds = await _playoffService.GetPlayoffQualifierTeamIdsAsync(seasonId);
        if (!playoffQualifierIds.Contains(teamId))
            return new BaseResult(false, "Team is not a playoff qualifier.");

        entry.Tiebreaker = tiebreaker;

        // Re-sort all standings by tiebreaker order and reassign Seed 1..N so global order stays consistent.
        var sorted = standings.OrderByDescending(s => s.Tiebreaker).ThenBy(s => s.TeamId).ToList();
        for (var i = 0; i < sorted.Count; i++)
            sorted[i].Seed = i + 1;
        await _teamStandingsRepository.UpdateRangeAsync(sorted);
        return new BaseResult(true, "Tiebreaker updated.");
    }
}
