using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.Model
{
    public class WeeklyResult
    {
        public int WeekNumber { get; set; }
        public int Wins { get; set; }
        public int Loses { get; set; }
        public string OpposingTeamName { get; set; } = "";
    }
}
