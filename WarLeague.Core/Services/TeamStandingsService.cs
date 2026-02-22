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
    private readonly WeekRepository _weekRepository;
    private readonly ConferenceRepository _conferenceRepository;
    private readonly TeamRepository _teamRepository;
    private readonly PlayoffMatchupRepository _playoffMatchupRepository;
    private readonly SeasonRepository _seasonRepository;

    public TeamStandingsService(
        TiebreakerService tiebreakerService,
        TeamStandingsRepository teamStandingsRepository,
        RoundRobinMatchupRepository roundRobinMatchupRepository,
        WeekRepository weekRepository,
        ConferenceRepository conferenceRepository,
        TeamRepository teamRepository,
        PlayoffMatchupRepository playoffMatchupRepository,
        SeasonRepository seasonRepository)
    {
        _tiebreakerService = tiebreakerService;
        _teamStandingsRepository = teamStandingsRepository;
        _roundRobinMatchupRepository = roundRobinMatchupRepository;
        _weekRepository = weekRepository;
        _conferenceRepository = conferenceRepository;
        _teamRepository = teamRepository;
        _playoffMatchupRepository = playoffMatchupRepository;
        _seasonRepository = seasonRepository;
    }

    /// <summary>
    /// Gets round-robin standings (W-L per team) from completed weeks ordered by centralized tiebreaker ranking.
    /// </summary>
    public async Task<List<RoundRobinStandingsEntry>> GetRoundRobinStandingsForDisplayAsync(int seasonId)
    {
        var data = await GetRoundRobinWinsAndH2HAsync(seasonId);
        var teams = await _teamRepository.GetBySeasonAsync(seasonId);
        var conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
        var conferenceById = conferences.ToDictionary(c => c.Id);
        var ranking = _tiebreakerService.RankTeams(teams, data);

        var entriesByTeamId = teams
            .Select(t => new RoundRobinStandingsEntry
            {
                TeamId = t.Id,
                TeamName = t.Name,
                ConferenceName = conferenceById.TryGetValue(t.ConferenceId, out var conf) ? conf.Name : "",
                Tiebreaker = ranking.TiebreakerByTeamId.GetValueOrDefault(t.Id, 0),
                Wins = data.WinsByTeamId.GetValueOrDefault(t.Id, 0),
                Losses = data.LossesByTeamId.GetValueOrDefault(t.Id, 0)
            })
            .ToDictionary(entry => entry.TeamId);

        return ranking.OrderedTeams
            .Where(team => entriesByTeamId.ContainsKey(team.Id))
            .Select(team => entriesByTeamId[team.Id])
            .ToList();
    }

    /// <summary>
    /// Generates TeamStandings from round-robin results: computes wins per team,
    /// ranks each conference by centralized tiebreaker, takes top N by PlayoffTeamsCount,
    /// then stores those tiebreaker values in TeamStandings.
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

        var data = await GetRoundRobinWinsAndH2HAsync(seasonId);
        var winsByTeamId = data.WinsByTeamId;
        var teams = await _teamRepository.GetBySeasonAsync(seasonId);
        var conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
        var allTiebreakers = _tiebreakerService.RankTeams(teams, data).TiebreakerByTeamId;
        var playoffTeams = new List<Team>();

        foreach (var conference in conferences.Where(c => c.PlayoffTeamsCount > 0))
        {
            var conferenceTeams = teams.Where(t => t.ConferenceId == conference.Id).ToList();
            var conferenceRanking = _tiebreakerService.RankTeams(conferenceTeams, data);
            var seeded = conferenceRanking.OrderedTeams
                .Take(conference.PlayoffTeamsCount)
                .ToList();
            playoffTeams.AddRange(seeded);
        }

        // Global seed order uses centralized tiebreaker values.
        var seededOrder = playoffTeams
            .OrderByDescending(t => allTiebreakers.GetValueOrDefault(t.Id, 0))
            .ThenBy(t => t.Id)
            .ToList();

        await _teamStandingsRepository.DeleteBySeasonIdAsync(seasonId);

        var standings = new List<TeamStandings>();
        for (var i = 0; i < seededOrder.Count; i++)
        {
            var team = seededOrder[i];
            var wins = winsByTeamId.GetValueOrDefault(team.Id, 0);
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

        return new BaseResult(true, $"Generated standings for {standings.Count} playoff team(s).");
    }
    /// <summary>
    /// Single source for round-robin wins (and h2h) from completed weeks. Uses GetBySeasonIdForCompletedWeeksAsync once.
    /// </summary>
    private async Task<RoundRobinWinsAndH2H> GetRoundRobinWinsAndH2HAsync(int seasonId)
    {
        var matchups = await _roundRobinMatchupRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);
        var teams = await _teamRepository.GetBySeasonAsync(seasonId);
        var winsByTeamId = new Dictionary<int, int>();
        foreach (var t in teams)
            winsByTeamId[t.Id] = 0;

        foreach (var m in matchups)
        {
            if (!m.TeamWinnerId.HasValue)
                continue;
            if (m.MatchupType == MatchupType.Bye)
                winsByTeamId[m.TeamWinnerId.Value] = winsByTeamId.GetValueOrDefault(m.TeamWinnerId.Value, 0) + 1;
            else
            {
                winsByTeamId[m.TeamWinnerId.Value] = winsByTeamId.GetValueOrDefault(m.TeamWinnerId.Value, 0) + 1;
                var loserId = m.Team1Id == m.TeamWinnerId.Value ? m.Team2Id : m.Team1Id;
                winsByTeamId[loserId] = winsByTeamId.GetValueOrDefault(loserId, 0); // ensure key exists (losses not stored here)
            }
        }

        var h2hByTeamId = new Dictionary<int, int>();
        foreach (var t in teams)
            h2hByTeamId[t.Id] = 0;

        var lossesByTeamId = new Dictionary<int, int>();
        foreach (var t in teams)
            lossesByTeamId[t.Id] = 0;
        foreach (var m in matchups.Where(m => m.TeamWinnerId.HasValue && m.MatchupType != MatchupType.Bye))
        {
            var loserId = m.Team1Id == m.TeamWinnerId!.Value ? m.Team2Id : m.Team1Id;
            lossesByTeamId[loserId] = lossesByTeamId.GetValueOrDefault(loserId, 0) + 1;
        }

        return new RoundRobinWinsAndH2H(winsByTeamId, lossesByTeamId, h2hByTeamId, matchups);
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
            return new BaseResult(false, "Team is not in playoff standings.");
        entry.Tiebreaker = tiebreaker;

        // Re-sort by tiebreaker order and reassign Seed so seed always equals tiebreaker-ordered standing.
        var sorted = standings.OrderByDescending(s => s.Tiebreaker).ThenBy(s => s.TeamId).ToList();
        for (var i = 0; i < sorted.Count; i++)
            sorted[i].Seed = i + 1;
        await _teamStandingsRepository.UpdateRangeAsync(sorted);
        return new BaseResult(true, "Tiebreaker updated.");
    }
}
