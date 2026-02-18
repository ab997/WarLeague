using WarLeague.Data.Enums;

namespace WarLeague.Data.Entities;

public class Season
{
    public int Id { get; set; }
    public int SeasonNumber { get; set; }
    public int FormatId { get; set; }
    public Format Format { get; set; } = null!;
    public IEnumerable<Week> Weeks { get; set; } = null!;
    public IEnumerable<Team> Teams { get; set; } = null!;
    public IEnumerable<Conference> Conferences { get; set; } = null!;
    public bool Active { get; set; }
    public bool DisableTeamModification { get; set; }
    public int MinimumTeamMembers { get; set; }
    public SeasonPhase Phase { get; set; } = SeasonPhase.RoundRobin;
}
