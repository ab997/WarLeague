using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarLeague.Core.Domain.Models
{
    public class TeamStanding
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double TieBreaker { get; set; } // Placeholder formula
        public int Rank { get; set; }
    }

    public class IndividualStanding
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get; set; }
        public int Rank { get; set; }
    }

    public class DeckStanding
    {
        public int FormatId { get; set; }
        public string FormatName { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get; set; }
        public int Rank { get; set; }
    }

    public class WeekProgress
    {
        public int WeekId { get; set; }
        public int TotalMatches { get; set; }
        public int CompletedMatches { get; set; }
        public int PendingMatches { get; set; }
        public double CompletionPercentage { get; set; }
    }
}
