

namespace WarLeague.Core.Data.Entities
{
    public class PlayerSeasonTeam
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public Player Player { get; set; } = new()!;
        public int SeasonId { get; set; }
        public Season Season { get; set; } = new()!;
        public int TeamId { get; set; }
        public Team Team { get; set; } = new()!;
    }
}
