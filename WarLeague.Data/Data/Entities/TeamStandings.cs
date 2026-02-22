using WarLeague.Data.Entities;

namespace WarLeague.Data.Data.Entities;

public class TeamStandings
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public int Tiebreaker { get; set; }
    public int Seed { get; set; }
    public int? Wins { get; set; }
}
