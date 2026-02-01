using WarLeague.Core.Data.Enums;

namespace WarLeague.Core.Data.Entities;

public class Week
{
    public int Id { get; set; }
    public int WeekNumber { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public WeekStatus Status { get; set; } = WeekStatus.Open;
    public DateTime? SubmissionsClosedDate { get; set; }
    public List<Match> Matches { get; set; } = null!;
    public List<DeckSubmission> DeckSubmissions { get; set; } = null!;
}
