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
}
