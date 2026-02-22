namespace WarLeague.Core.Model;

/// <summary>
/// Per-team stats produced by the tiebreaker (for display). All tiebreaker logic is encapsulated in TiebreakerService.
/// </summary>
public record TiebreakerTeamStats(
    int Wins,
    int Losses,
    int SeriesDiff,
    int GameDiff,
    double SeriesPct,
    double GamePct,
    double? SOS
);
