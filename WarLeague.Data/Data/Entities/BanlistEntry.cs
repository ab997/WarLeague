using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;

namespace WarLeague.Data.Data.Entities;

public class BanlistEntry
{
    public int Id { get; set; }
    public int FormatId { get; set; }
    public int CardId { get; set; }
    public Format Format { get; set; } = null!;
    public Card Card { get; set; } = null!;
    public BanlistEntryCategory BanlistEntryCategory { get; set; }
}