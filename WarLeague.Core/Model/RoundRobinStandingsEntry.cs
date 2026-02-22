namespace WarLeague.Core.Model;

public class RoundRobinStandingsEntry
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string ConferenceName { get; set; } = string.Empty;
    public int Tiebreaker { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    /// <summary>Individual series differential (series won − series lost).</summary>
    public int SeriesDiff { get; set; }
    /// <summary>Individual game differential (games won − games lost).</summary>
    public int GameDiff { get; set; }
    /// <summary>Individual series win percentage (0–1), or 0 if no series played.</summary>
    public double SeriesPct { get; set; }
    /// <summary>Individual game win percentage (0–1), or 0 if no games played.</summary>
    public double GamePct { get; set; }
    /// <summary>Strength of schedule (opponents' average team match win %), or null if not computed.</summary>
    public double? SOS { get; set; }
}
