using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Services;

public class TeamStandingsService
{
    private readonly TiebreakerService _tiebreakerService;
    private readonly TeamStandingsRepository _teamStandingsRepository;
    private readonly RoundRobinMatchupRepository _roundRobinMatchupRepository;
    private readonly MatchRepository _matchRepository;
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
        var teams = (await _teamRepository.GetBySeasonAsync(seasonId)).ToList();
        var matchups = await _roundRobinMatchupRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);
        var matches = await _matchRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);
        var conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
        var conferenceById = conferences.ToDictionary(c => c.Id);

        var tiebreakers = _tiebreakerService.RankTeams(teams, matchups, matches);

        var statsByTeamId = ComputeDisplayStats(teams, matchups, matches);

        var orderedTeams = teams
            .OrderByDescending(t => tiebreakers.GetValueOrDefault(t.Id, 0))
            .ThenBy(t => t.Id)
            .ToList();

        var entriesByTeamId = teams.ToDictionary(t => t.Id, t =>
        {
            var stats = statsByTeamId.GetValueOrDefault(t.Id);
            return new RoundRobinStandingsEntry
            {
                TeamId = t.Id,
                TeamName = t.Name,
                ConferenceName = conferenceById.TryGetValue(t.ConferenceId, out var conf) ? conf.Name : "",
                Tiebreaker = (int)tiebreakers.GetValueOrDefault(t.Id, 0),
                Wins = stats.Wins,
                Losses = stats.Losses
            };
        });

        return orderedTeams
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

        var teams = (await _teamRepository.GetBySeasonAsync(seasonId)).ToList();
        var matchups = await _roundRobinMatchupRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);
        var matches = await _matchRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);

        var tiebreakers = _tiebreakerService.RankTeams(teams, matchups, matches);

        var orderedTeams = teams
            .OrderByDescending(t => tiebreakers.GetValueOrDefault(t.Id, 0))
            .ThenBy(t => t.Id)
            .ToList();

        var statsByTeamId = ComputeDisplayStats(teams, matchups, matches);

        await _teamStandingsRepository.DeleteBySeasonIdAsync(seasonId);

        var standings = new List<TeamStandings>();
        for (var i = 0; i < orderedTeams.Count; i++)
        {
            var team = orderedTeams[i];
            var wins = statsByTeamId.GetValueOrDefault(team.Id).Wins;
            standings.Add(new TeamStandings
            {
                SeasonId = seasonId,
                TeamId = team.Id,
                Tiebreaker = (int)tiebreakers.GetValueOrDefault(team.Id, 0),
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

    private static IReadOnlyDictionary<int, (int Wins, int Losses)> ComputeDisplayStats(
        IReadOnlyList<Team> teams,
        IReadOnlyList<RoundRobinMatchup> matchups,
        IReadOnlyList<Match> matches)
    {
        var winsByTeamId = teams.ToDictionary(t => t.Id, _ => 0);
        var lossesByTeamId = teams.ToDictionary(t => t.Id, _ => 0);

        foreach (var m in matchups)
        {
            if (!m.TeamWinnerId.HasValue) continue;
            if (m.MatchupType == MatchupType.Bye)
                winsByTeamId[m.TeamWinnerId.Value] = winsByTeamId.GetValueOrDefault(m.TeamWinnerId.Value, 0) + 1;
            else
            {
                winsByTeamId[m.TeamWinnerId.Value] = winsByTeamId.GetValueOrDefault(m.TeamWinnerId.Value, 0) + 1;
                var loserId = m.Team1Id == m.TeamWinnerId.Value ? m.Team2Id : m.Team1Id;
                lossesByTeamId[loserId] = lossesByTeamId.GetValueOrDefault(loserId, 0) + 1;
            }
        }

        var result = new Dictionary<int, (int Wins, int Losses)>();
        foreach (var t in teams)
        {
            result[t.Id] = (
                Wins: winsByTeamId.GetValueOrDefault(t.Id, 0),
                Losses: lossesByTeamId.GetValueOrDefault(t.Id, 0));
        }

        return result;
    }
}
