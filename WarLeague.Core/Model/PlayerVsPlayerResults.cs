using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.Model
{
    public class PlayerVsPlayerResult
    {
        public int WeekNumber { get; set; }
        public string DeckType { get; set; } = "";
        public string OpposingDeckType { get; set; } = "";
        public string Replay { get; set; } = "";
    }
}
