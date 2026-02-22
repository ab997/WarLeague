using WarLeague.Data.Data.Entities;

namespace WarLeague.Core.Model;

/// <summary>
/// Wins, losses, and tiebreaker aggregates from round-robin completed weeks (for playoff seeding and display).
/// Matchups are used to compute H2H among tied teams only (not stored globally).
/// </summary>
public record RoundRobinWinsAndH2H(
    IReadOnlyDictionary<int, int> WinsByTeamId,
    IReadOnlyDictionary<int, int> LossesByTeamId,
    IReadOnlyDictionary<int, int> H2HByTeamId,
    IReadOnlyList<RoundRobinMatchup> Matchups,
    IReadOnlyDictionary<int, int> SeriesWByTeamId,
    IReadOnlyDictionary<int, int> SeriesLByTeamId,
    IReadOnlyDictionary<int, int> GamesWByTeamId,
    IReadOnlyDictionary<int, int> GamesLByTeamId,
    IReadOnlyDictionary<int, double>? SOSByTeamId = null
);
