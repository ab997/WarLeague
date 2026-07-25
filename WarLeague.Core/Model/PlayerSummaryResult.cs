using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.Model
{
    public class PlayerSummaryResult : BaseResult
    {
        public PlayerSummaryResult()
        {
            
        }

        public List<PlayerResult> PlayerResults { get; set; } = [];
    }

    public class PlayerResult
    {
        public List<PlayerVsPlayerResult> PlayerVsPlayerResults { get; set; } = [];
        public string Name { get; set; } = "";
        public int Wins { get; set; }
        public int Loses { get; set; }
    }

    public class PlayerVsPlayerResult
    {
        public int WeekNumber { get; set; }
        public string DeckType { get; set; } = "";
        public string OpposingDeckType { get; set; } = "";
        public string OpponentName { get; set; } = "";
        public string Replay { get; set; } = "";
        public int GameWins { get; set; }
        public int GameLoses { get; set; }
    }
}
