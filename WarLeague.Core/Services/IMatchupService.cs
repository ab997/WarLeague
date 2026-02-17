using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Model;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Services
{
    public interface IMatchupService
    {
        (List<Match> createdMatches, List<WeeklyMatchup> matchupOutputs) GetIndividualMatchups(Week week, List<(Team a, Team b)> teamMatchups, Dictionary<int, List<DeckSubmission>> submissionsByTeamId);
        List<(Team a, Team b)> GetTeamMatchups(IReadOnlyList<Team> teams, int weekNumber);
    }
}
