using WarLeague.Core.Data.Enums;

namespace WarLeague.Core.Data.Entities;

public class Player
{
    public int Id { get; set; }
    public ulong DiscordUserId { get; set; }
    public string UserName { get; set; } = "";
    public IEnumerable<Match> MatchesAsPlayer1 { get; set; } = null!;
    public IEnumerable<Match> MatchesAsPlayer2 { get; set; } = null!;
    public IEnumerable<Match> MatchesWon { get; set; } = null!;
    public IEnumerable<PlayerSeasonTeam> PlayerSeasonTeams { get; set; } = null!;
}
