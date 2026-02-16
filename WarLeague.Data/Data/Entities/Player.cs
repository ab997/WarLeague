using WarLeague.Data.Enums;

namespace WarLeague.Data.Entities;

public class Player
{
    public int Id { get; set; }
    public ulong DiscordUserId { get; set; }
    public string UserName { get; set; } = "";
    public IEnumerable<PlayerSeasonTeam> PlayerSeasonTeams { get; set; } = null!;
}
