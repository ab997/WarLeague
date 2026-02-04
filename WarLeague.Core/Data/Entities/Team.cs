namespace WarLeague.Core.Data.Entities;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CaptainId { get; set; }
    public Player Captain { get; set; } = null!;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public ulong? DiscordRoleId { get; set; }
}
