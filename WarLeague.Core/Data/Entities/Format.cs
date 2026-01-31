namespace WarLeague.Core.Data.Entities;

public class Format
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // HAT, GOAT, Edison, etc.
    public string Rules { get; set; } = "{}"; // JSON for extensibility
    public IEnumerable<Season> Seasons { get; set; } = null!;
}
