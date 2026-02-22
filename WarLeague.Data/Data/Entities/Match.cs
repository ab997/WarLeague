using WarLeague.Data.Enums;

namespace WarLeague.Data.Entities;

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
    public int Team1Id { get; set; }
    public Team Team1 { get; set; } = null!;
    public int Team2Id { get; set; }
    public Team Team2 { get; set; } = null!;
    public int? WinnerTeamId { get; set; }
    public Team? WinnerTeam { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public DateTime? ReportedDate { get; set; }
    public string? ReplayUrl { get; set; }
    public MatchResultType? MatchResultType { get; set; }
    public int? Player1Wins { get; set; }
    public int? Player2Wins { get; set; }
}
