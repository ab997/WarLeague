namespace WarLeague.Data.Entities;

public class Conference
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
}