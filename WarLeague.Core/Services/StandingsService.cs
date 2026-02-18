using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Services;

public class StandingsService
{
    private readonly RoundRobinMatchupRepository _roundRobinMatchupRepository;
    private readonly TeamRepository _teamRepository;
    private readonly ConferenceRepository _conferenceRepository;

    public StandingsService(
        RoundRobinMatchupRepository roundRobinMatchupRepository,
        TeamRepository teamRepository,
        ConferenceRepository conferenceRepository)
    {
        _roundRobinMatchupRepository = roundRobinMatchupRepository;
        _teamRepository = teamRepository;
        _conferenceRepository = conferenceRepository;
    }

    /// <summary>
    /// Gets round-robin standings (W-L per team) from completed weeks. Tiebreaker: wins desc, losses asc, then TeamId.
    /// </summary>
    public async Task<List<RoundRobinStandingsEntry>> GetRoundRobinStandingsAsync(int seasonId)
    {
        var matchups = await _roundRobinMatchupRepository.GetBySeasonIdForCompletedWeeksAsync(seasonId);
        var teams = await _teamRepository.GetBySeasonAsync(seasonId);
        var conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
        var conferenceById = conferences.ToDictionary(c => c.Id);

        var wins = new Dictionary<int, int>();
        var losses = new Dictionary<int, int>();

        foreach (var team in teams)
        {
            wins[team.Id] = 0;
            losses[team.Id] = 0;
        }

        foreach (var m in matchups)
        {
            if (!m.TeamWinnerId.HasValue)
                continue;

            if (m.MatchupType == MatchupType.Bye)
            {
                wins[m.TeamWinnerId.Value] = wins.GetValueOrDefault(m.TeamWinnerId.Value, 0) + 1;
            }
            else
            {
                wins[m.TeamWinnerId.Value] = wins.GetValueOrDefault(m.TeamWinnerId.Value, 0) + 1;
                var loserId = m.Team1Id == m.TeamWinnerId.Value ? m.Team2Id : m.Team1Id;
                losses[loserId] = losses.GetValueOrDefault(loserId, 0) + 1;
            }
        }

        return teams
            .Select(t => new RoundRobinStandingsEntry
            {
                TeamId = t.Id,
                TeamName = t.Name,
                ConferenceName = conferenceById.TryGetValue(t.ConferenceId, out var conf) ? conf.Name : "",
                Wins = wins.GetValueOrDefault(t.Id, 0),
                Losses = losses.GetValueOrDefault(t.Id, 0)
            })
            .OrderByDescending(e => e.Wins)
            .ThenBy(e => e.Losses)
            .ThenBy(e => e.TeamId)
            .ToList();
    }
}
