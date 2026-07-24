using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.Model
{
    public class RoundSummaryResult : BaseResult
    {
        public RoundSummaryResult()
        {
            
        }
        public List<WeeklyResult> WeeklyResults { get; set; } = [];
    }
}
