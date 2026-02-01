using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data.Entities;

namespace WarLeague.Core.Domain.Model
{
    public readonly record struct GeneratePairingsResult(
        bool Success,
        string Message,
        Week? Week,
        List<Match>? CreatedMatches,
        List<WeeklyMatchup>? WeeklyMatchups);
}
