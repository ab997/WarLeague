using System.ComponentModel.DataAnnotations.Schema;

namespace WarLeague.Data.Data.Entities;

public class Card
{
    // ygopro id
    public int Id { get; set; }
    public string YgoproId { get; set; } = string.Empty;
    public DateOnly? FirstReleaseDate { get; set; }
    public string Utf8Name { get; set; } = string.Empty;
    public IEnumerable<BanlistEntry> BanlistEntries = null!;

    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }
}