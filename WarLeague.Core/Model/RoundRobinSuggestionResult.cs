namespace WarLeague.Core.Model;

/// <summary>
/// Result of round-robin schedule suggestion (rounds per conference and total suggested weeks).
/// </summary>
public class RoundRobinSuggestionResult
{
    public List<RoundRobinConferenceSuggestion> Conferences { get; set; } = new();
    public int TotalSuggestedWeeks { get; set; }
}

public class RoundRobinConferenceSuggestion
{
    public string ConferenceName { get; set; } = string.Empty;
    public int TeamCount { get; set; }
    public int Rounds { get; set; }
}
