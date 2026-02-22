using WarLeague.Data.Data.Entities;

namespace WarLeague.Core.Model;

/// <summary>
/// Wins, losses, and head-to-head wins per team from round-robin completed weeks (for playoff seeding and display).
/// Matchups are included for iterative tiebreaker ordering (TB1/TB2 vs tied teams, then TB3 overall).
/// </summary>
public record RoundRobinWinsAndH2H(
    IReadOnlyDictionary<int, int> WinsByTeamId,
    IReadOnlyDictionary<int, int> LossesByTeamId,
    IReadOnlyDictionary<int, int> H2HByTeamId,
    IReadOnlyList<RoundRobinMatchup> Matchups
);
