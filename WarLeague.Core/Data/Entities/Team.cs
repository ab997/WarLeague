namespace WarLeague.Core.Data.Entities;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CaptainId { get; set; }
    public Player Captain { get; set; } = null!;
    public List<Player> Players { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
