using WarLeague.Core.Model;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Services;

public class TiebreakerService
{
    private const int WinsMultiplier = 1_000_000;

    /// <summary>
    /// Computes deterministic tiebreaker values and ranking order for a set of teams.
    /// Current rule: wins desc, then Team.Id asc.
    /// </summary>
    public TiebreakerRankingResult RankTeams(
        IEnumerable<Team> teams,
        RoundRobinWinsAndH2H data)
    {
        var tiebreakerByTeamId = teams
            .ToDictionary(
                team => team.Id,
                team => GetTiebreakerValue(data.WinsByTeamId.GetValueOrDefault(team.Id, 0), team.Id));

        var orderedTeams = teams
            .OrderByDescending(team => tiebreakerByTeamId[team.Id])
            .ThenBy(team => team.Id)
            .ToList();

        return new TiebreakerRankingResult(orderedTeams, tiebreakerByTeamId);
    }

    private static int GetTiebreakerValue(int wins, int teamId)
    {
        return wins * WinsMultiplier - teamId;
    }
}

public record TiebreakerRankingResult(
    IReadOnlyList<Team> OrderedTeams,
    IReadOnlyDictionary<int, int> TiebreakerByTeamId);
