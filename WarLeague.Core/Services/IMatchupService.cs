using WarLeague.Core.Model;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Services
{
    public interface IMatchupService
    {
        (List<Match> createdMatches, List<WeeklyMatchup> matchupOutputs) GetIndividualMatchups(Week week, List<(Team a, Team b)> teamMatchups, Dictionary<int, List<DeckSubmission>> submissionsByTeamId);
        Task<List<(Team a, Team b)>> GetTeamMatchups(IReadOnlyList<Team> teams, int weekNumber);
        Task<BaseResult> SaveTeamMatchupsAsync(Week week, IReadOnlyList<Team> teams, IReadOnlyList<(Team a, Team b)> teamMatchups);
        Task<BaseResult> UpdateMatchupWinnersForWeekAsync(Week week, IReadOnlyList<Match> matches);
    }
}
