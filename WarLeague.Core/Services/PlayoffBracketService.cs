using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data.Data.Enums;

namespace WarLeague.Core.Services;

public class PlayoffBracketService
{
    private readonly PlayoffMatchupRepository _playoffMatchupRepository;
    private readonly TeamRepository _teamRepository;

    public PlayoffBracketService(
        PlayoffMatchupRepository playoffMatchupRepository,
        TeamRepository teamRepository)
    {
        _playoffMatchupRepository = playoffMatchupRepository;
        _teamRepository = teamRepository;
    }

    /// <summary>
    /// Gets the playoff bracket for the season: all playoff matchups by round/week with team names and winners.
    /// </summary>
    public async Task<List<PlayoffBracketMatchupDisplay>> GetBracketAsync(int seasonId)
    {
        var matchups = await _playoffMatchupRepository.GetBySeasonIdAsync(seasonId);
        if (matchups.Count == 0)
            return new List<PlayoffBracketMatchupDisplay>();

        var teamIds = matchups.SelectMany(m => new[] { m.Team1Id, m.Team2Id }).Distinct().ToHashSet();
        var teams = await _teamRepository.GetBySeasonAsync(seasonId);
        var teamById = teams.Where(t => teamIds.Contains(t.Id)).ToDictionary(t => t.Id);

        string Name(int teamId) => teamById.TryGetValue(teamId, out var t) ? t.Name : $"Team#{teamId}";

        return matchups
            .OrderBy(m => m.Week?.WeekNumber ?? 0)
            .ThenBy(m => m.BracketPosition)
            .Select(m => new PlayoffBracketMatchupDisplay
            {
                Round = m.Round,
                WeekNumber = m.Week?.WeekNumber ?? 0,
                BracketPosition = m.BracketPosition,
                Team1Name = Name(m.Team1Id),
                Team2Name = Name(m.Team2Id),
                WinnerName = m.TeamWinnerId.HasValue ? Name(m.TeamWinnerId.Value) : null,
                IsBye = m.MatchupType == MatchupType.Bye
            })
            .ToList();
    }
}
