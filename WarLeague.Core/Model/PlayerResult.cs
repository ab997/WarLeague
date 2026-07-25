using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.Model
{
    public class PlayerResult
    {
        public List<PlayerVsPlayerResult> PlayerVsPlayerResults { get; set; } = [];
        public string Name { get; set; } = "";
        public int Wins { get; set; }
        public int Loses { get; set; }
    }
}
