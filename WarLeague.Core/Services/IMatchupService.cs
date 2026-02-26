using WarLeague.Core.Model;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Services
{
    public interface IMatchupService
    {
        Task<(List<Match> createdMatches, List<WeeklyMatchup> matchupOutputs)> GetIndividualMatchupsAsync(int weekId);
        Task<List<(Team a, Team b)>> GetTeamMatchupsAsync(int weekId);
        Task<BaseResult> SaveTeamMatchupsAsync(int weekId, IReadOnlyList<(Team a, Team b)> teamMatchups);
        Task<BaseResult> UpdateMatchupWinnersForWeekAsync(int weekId, IReadOnlyList<Match> matches);

        /// <summary>
        /// Returns teams that have a bye this week for pairings display.
        /// Round-robin: teams not in any matchup. Playoffs: teams in a BYE matchup (a.Id == b.Id).
        /// </summary>
        Task<List<Team>> GetByeTeamsForPairingsDisplayAsync(int weekId);

        /// <summary>
        /// Returns the set of team IDs that must have deck submissions for the given week.
        /// Used by close-submissions validation (phase-agnostic: round-robin = all teams, playoffs = participating only).
        /// </summary>
        Task<IReadOnlySet<int>> GetTeamIdsRequiredForSubmissionsAsync(int weekId);

        /// <summary>
        /// Returns round-robin suggestion (rounds per conference, total suggested weeks). Non-round-robin phases return null.
        /// </summary>
        Task<RoundRobinSuggestionResult?> GetSuggestedRoundsAsync(int seasonId);

        /// <summary>
        /// Returns existing team-vs-team matchups for the week if they were pre-generated (e.g. round-robin schedule). Otherwise null.
        /// </summary>
        Task<List<(Team a, Team b)>?> GetExistingTeamMatchupsAsync(int weekId);

        /// <summary>
        /// Validates whether a team is allowed to submit decks for the given week.
        /// Round-robin: any team in the season may submit. Playoffs: only teams that made it to (this round of) playoffs may submit.
        /// </summary>
        Task<BaseResult> ValidateTeamCanSubmitForWeekAsync(int weekId, int teamId);
    }
}
