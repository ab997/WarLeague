using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Model
{
    public class GeneratePairingsResult : BaseResult
    {
        public GeneratePairingsResult()
        {
            
        }
        public GeneratePairingsResult(bool success, string message, Week? week, List<Match>? createdMatches, List<WeeklyMatchup>? weeklyMatchups) : base(success, message)
        {
            Week = week;
            CreatedMatches = createdMatches;
            WeeklyMatchups = weeklyMatchups;
        }
        public Week? Week { get; set; }
        public List<Match>? CreatedMatches { get; set; }
        public List<WeeklyMatchup>? WeeklyMatchups { get; set; }
        public List<Team>? ByeTeams { get; set; }
    }
}
