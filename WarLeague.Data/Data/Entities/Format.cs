using WarLeague.Data.Data.Entities;

namespace WarLeague.Data.Entities;

public class Format
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // HAT, GOAT, Edison, etc.
    public string Rules { get; set; } = "{}"; // JSON for extensibility
    public IEnumerable<Season> Seasons { get; set; } = null!;
    public bool SingleFormatMode { get; set; }
    public ulong GuildId { get; set; }
    public DateTime LastLegalReleaseDate { get; set; } = DateTime.Now.Date;
    public IEnumerable<BanlistEntry> BanlistEntries { get; set; } = null!;
}