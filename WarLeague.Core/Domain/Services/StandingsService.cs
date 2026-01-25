using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Models;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services;

public class StandingsService
{
    private readonly MatchRepository _matchRepository;
    private readonly TeamRepository _teamRepository;
    private readonly WeekRepository _weekRepository;
    private readonly DeckSubmissionRepository _deckSubmissionRepository;

    public StandingsService(
        MatchRepository matchRepository,
        TeamRepository teamRepository,
        WeekRepository weekRepository,
        DeckSubmissionRepository deckSubmissionRepository)
    {
        _matchRepository = matchRepository;
        _teamRepository = teamRepository;
        _weekRepository = weekRepository;
        _deckSubmissionRepository = deckSubmissionRepository;
    }

    public async Task<List<TeamStanding>> GetTeamStandingsAsync(int weekId)
    {
        var matches = await _matchRepository.GetByWeekIdAsync(weekId);
        var teams = await _teamRepository.GetAllActiveAsync();

        var standings = new Dictionary<int, TeamStanding>();

        // Initialize standings for all teams
        foreach (var team in teams)
        {
            standings[team.Id] = new TeamStanding
            {
                TeamId = team.Id,
                TeamName = team.Name,
                Wins = 0,
                Losses = 0,
                TieBreaker = 0.0
            };
        }

        // Calculate wins and losses
        foreach (var match in matches.Where(m => m.WinnerId.HasValue))
        {
            var winner = match.Winner!;
            var loser = match.Player1Id == winner.Id ? match.Player2 : match.Player1;

            if (winner.TeamId.HasValue && standings.ContainsKey(winner.TeamId.Value))
            {
                standings[winner.TeamId.Value].Wins++;
            }

            if (loser.TeamId.HasValue && standings.ContainsKey(loser.TeamId.Value))
            {
                standings[loser.TeamId.Value].Losses++;
            }
        }

        // Calculate tiebreakers (placeholder formula)
        foreach (var standing in standings.Values)
        {
            // Placeholder: Simple win rate with small tiebreaker adjustment
            var totalGames = standing.Wins + standing.Losses;
            if (totalGames > 0)
            {
                standing.TieBreaker = (double)standing.Wins / totalGames;
            }
        }

        // Sort by wins (desc), then tiebreaker (desc)
        var sortedStandings = standings.Values
            .OrderByDescending(s => s.Wins)
            .ThenByDescending(s => s.TieBreaker)
            .ToList();

        // Assign ranks
        for (int i = 0; i < sortedStandings.Count; i++)
        {
            sortedStandings[i].Rank = i + 1;
        }

        return sortedStandings;
    }

    public async Task<List<IndividualStanding>> GetIndividualStandingsAsync(int weekId)
    {
        var matches = await _matchRepository.GetByWeekIdAsync(weekId);
        var players = new Dictionary<int, IndividualStanding>();

        // Initialize standings for all players who have matches
        foreach (var match in matches)
        {
            if (!players.ContainsKey(match.Player1Id))
            {
                players[match.Player1Id] = new IndividualStanding
                {
                    PlayerId = match.Player1Id,
                    PlayerName = match.Player1.DiscordUsername,
                    TeamId = match.Player1.TeamId ?? 0,
                    TeamName = match.Player1.Team?.Name ?? "No Team",
                    Wins = 0,
                    Losses = 0
                };
            }

            if (!players.ContainsKey(match.Player2Id))
            {
                players[match.Player2Id] = new IndividualStanding
                {
                    PlayerId = match.Player2Id,
                    PlayerName = match.Player2.DiscordUsername,
                    TeamId = match.Player2.TeamId ?? 0,
                    TeamName = match.Player2.Team?.Name ?? "No Team",
                    Wins = 0,
                    Losses = 0
                };
            }
        }

        // Calculate wins and losses
        foreach (var match in matches.Where(m => m.WinnerId.HasValue))
        {
            var winner = match.Winner!;
            var loser = match.Player1Id == winner.Id ? match.Player2 : match.Player1;

            if (players.ContainsKey(winner.Id))
            {
                players[winner.Id].Wins++;
            }

            if (players.ContainsKey(loser.Id))
            {
                players[loser.Id].Losses++;
            }
        }

        // Calculate win rates
        foreach (var standing in players.Values)
        {
            var totalGames = standing.Wins + standing.Losses;
            if (totalGames > 0)
            {
                standing.WinRate = (double)standing.Wins / totalGames;
            }
        }

        // Sort by wins (desc), then win rate (desc)
        var sortedStandings = players.Values
            .OrderByDescending(s => s.Wins)
            .ThenByDescending(s => s.WinRate)
            .ToList();

        // Assign ranks
        for (int i = 0; i < sortedStandings.Count; i++)
        {
            sortedStandings[i].Rank = i + 1;
        }

        return sortedStandings;
    }

    public async Task<List<DeckStanding>> GetDeckStandingsAsync(int weekId)
    {
        await Task.Delay(0);
		throw new NotImplementedException("GetDeckStandingsAsync");
        //var matches = await _matchRepository.GetByWeekIdAsync(weekId);
        //var submissions = await _deckSubmissionRepository.GetByWeekIdAsync(weekId);

        //var deckStandings = new Dictionary<int, DeckStanding>();

        //// Initialize standings for all formats used
        //foreach (var submission in submissions)
        //{
        //    if (!deckStandings.ContainsKey(submission.FormatId))
        //    {
        //        deckStandings[submission.FormatId] = new DeckStanding
        //        {
        //            FormatId = submission.FormatId,
        //            FormatName = submission.Format.Name,
        //            Wins = 0,
        //            Losses = 0
        //        };
        //    }
        //}

        //// Calculate wins and losses by format
        //foreach (var match in matches.Where(m => m.WinnerId.HasValue))
        //{
        //    var winner = match.Winner!;
        //    var loser = match.Player1Id == winner.Id ? match.Player2 : match.Player1;

        //    // Find winner's deck submission
        //    var winnerSubmission = submissions.SingleOrDefault(s => s.PlayerId == winner.Id);
        //    if (winnerSubmission != null && deckStandings.ContainsKey(winnerSubmission.FormatId))
        //    {
        //        deckStandings[winnerSubmission.FormatId].Wins++;
        //    }

        //    // Find loser's deck submission
        //    var loserSubmission = submissions.SingleOrDefault(s => s.PlayerId == loser.Id);
        //    if (loserSubmission != null && deckStandings.ContainsKey(loserSubmission.FormatId))
        //    {
        //        deckStandings[loserSubmission.FormatId].Losses++;
        //    }
        //}

        //// Calculate win rates
        //foreach (var standing in deckStandings.Values)
        //{
        //    var totalGames = standing.Wins + standing.Losses;
        //    if (totalGames > 0)
        //    {
        //        standing.WinRate = (double)standing.Wins / totalGames;
        //    }
        //}

        //// Sort by wins (desc), then win rate (desc)
        //var sortedStandings = deckStandings.Values
        //    .OrderByDescending(s => s.Wins)
        //    .ThenByDescending(s => s.WinRate)
        //    .ToList();

        //// Assign ranks
        //for (int i = 0; i < sortedStandings.Count; i++)
        //{
        //    sortedStandings[i].Rank = i + 1;
        //}

        //return sortedStandings;
    }

    public async Task<WeekProgress> GetWeekProgressAsync(int weekId)
    {
        var matches = await _matchRepository.GetByWeekIdAsync(weekId);
        var totalMatches = matches.Count;
        var completedMatches = matches.Count(m => m.Status != MatchStatus.Scheduled);
        var pendingMatches = totalMatches - completedMatches;

        var completionPercentage = totalMatches > 0
            ? (double)completedMatches / totalMatches * 100
            : 0;

        return new WeekProgress
        {
            WeekId = weekId,
            TotalMatches = totalMatches,
            CompletedMatches = completedMatches,
            PendingMatches = pendingMatches,
            CompletionPercentage = completionPercentage
        };
    }
}
