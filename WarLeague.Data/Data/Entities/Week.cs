using WarLeague.Data.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Data.Entities;

public class Week
{
    public int Id { get; set; }
    public int WeekNumber { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public WeekStatus Status { get; set; } = WeekStatus.Open;
    public DateTime? SubmissionsClosedDate { get; set; }
    public IEnumerable<Match> Matches { get; set; } = null!;
    public IEnumerable<DeckSubmission> DeckSubmissions { get; set; } = null!;
    public int SubmissionsRequired { get; set; }
    public IEnumerable<RoundRobinMatchup> RoundRobinMatchups { get; set; } = null!;
    public IEnumerable<PlayoffMatchup> PlayoffMatchups { get; set; } = null!;
}
