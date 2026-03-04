using System.ComponentModel.DataAnnotations.Schema;

namespace WarLeague.Data.Data.Entities;

public class Card
{
    // ygopro id
    public int Id { get; set; }
    public string YgoproId { get; set; } = string.Empty;
    public DateTime FirstReleaseDate { get; set; } = DateTime.Now.Date;
    public string Utf8Name { get; set; } = string.Empty;
    public IEnumerable<BanlistEntry> BanlistEntries = null!;
}