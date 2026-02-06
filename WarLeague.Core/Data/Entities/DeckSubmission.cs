namespace WarLeague.Core.Data.Entities;

public class DeckSubmission
{
    public int Id { get; set; }
    public int WeekId { get; set; }
    public Week Week { get; set; } = null!;
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public string DeckFile { get; set; } = string.Empty;
    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
    public int SeatNumber { get; set; }
}
