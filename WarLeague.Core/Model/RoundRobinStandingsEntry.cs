namespace WarLeague.Core.Model;

public class RoundRobinStandingsEntry
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string ConferenceName { get; set; } = string.Empty;
    public int Wins { get; set; }
    public int Losses { get; set; }
}
