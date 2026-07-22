using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace WarLeague.Core.Model
{
    public class ReportWinResult : BaseResult
    {
        public ReportWinResult()
        {
            
        }
        public string Winner { get; set; } = "";
        public string WinnerTeam { get; set; } = "";
        public string Loser { get; set; } = "";
        public string LoserTeam { get; set; } = "";
        public string ReplayUrl { get; set; } = "";
    }
}
