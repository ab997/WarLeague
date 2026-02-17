

using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;

namespace WarLeague.Data.Data.Entities
{
    public class RoundRobinMatchup
    {
        public int Id { get; set; }
        public int WeekId { get; set; }
        public Week Week { get; set; } = null!;
        public int Team1Id { get; set; }
        public Team Team1 { get; set; } = null!;
        public int Team2Id { get; set; }
        public Team Team2 { get; set; } = null!;
        public MatchupType MatchupType { get; set; }
        public int? TeamWinnerId { get; set; }
        public Team? TeamWinner { get; set; }
    }
}
