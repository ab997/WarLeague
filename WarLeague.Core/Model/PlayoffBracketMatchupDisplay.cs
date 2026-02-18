namespace WarLeague.Core.Model;

public class PlayoffBracketMatchupDisplay
{
    public int Round { get; set; }
    public int WeekNumber { get; set; }
    public int BracketPosition { get; set; }
    public string Team1Name { get; set; } = string.Empty;
    public string Team2Name { get; set; } = string.Empty;
    public string? WinnerName { get; set; }
    public bool IsBye { get; set; }
}
