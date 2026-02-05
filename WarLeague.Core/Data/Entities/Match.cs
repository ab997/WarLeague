using WarLeague.Core.Data.Enums;

namespace WarLeague.Core.Data.Entities;

public class Match
{
    public int Id { get; set; }
    public int WeekId { get; set; }
    public Week Week { get; set; } = null!;
    public int Player1Id { get; set; }
    public Player Player1 { get; set; } = null!;
    public int Player2Id { get; set; }
    public Player Player2 { get; set; } = null!;
    public int? WinnerId { get; set; }
    public Player? Winner { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public DateTime? ReportedDate { get; set; }
    public string? ReplayUrl { get; set; }
    public MatchResultType? MatchResultType { get; set; }
}
