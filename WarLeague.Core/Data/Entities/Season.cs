namespace WarLeague.Core.Data.Entities;

public class Season
{
    public int Id { get; set; }
    public int SeasonNumber { get; set; }
    public int FormatId { get; set; }
    public Format Format { get; set; } = null!;
    public IEnumerable<Week> Weeks { get; set; } = new List<Week>();
    public bool Active { get; set; }
}
