using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services;

public class MatchService
{
    private readonly MatchRepository _matchRepository;
    private readonly WeekRepository _weekRepository;
    private readonly TeamRepository _teamRepository;

    public MatchService(MatchRepository matchRepository, WeekRepository weekRepository, TeamRepository teamRepository)
    {
        _matchRepository = matchRepository;
        _weekRepository = weekRepository;
        _teamRepository = teamRepository;
    }

    public async Task<List<Match>> GenerateRoundRobinMatchesAsync(int weekId)
    {
        var week = await _weekRepository.GetByIdAsync(weekId);
        if (week == null)
        {
            throw new ArgumentException($"Week with ID {weekId} not found.");
        }

        var teams = await _teamRepository.GetAllActiveAsync();
        if (teams.Count < 2)
        {
            throw new InvalidOperationException("At least 2 teams are required to generate matches.");
        }

        var matches = new List<Match>();
        var playersByTeam = new Dictionary<int, List<Player>>();

        // Get all players for each team
        foreach (var team in teams)
        {
            playersByTeam[team.Id] = team.Players.Where(p => p.IsActive).ToList();
        }

        // Generate round-robin matches between teams
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                var team1 = teams[i];
                var team2 = teams[j];

                var players1 = playersByTeam[team1.Id];
                var players2 = playersByTeam[team2.Id];

                // Create matches between players of different teams
                foreach (var player1 in players1)
                {
                    foreach (var player2 in players2)
                    {
                        var match = new Match
                        {
                            WeekId = weekId,
                            Player1Id = player1.Id,
                            Player2Id = player2.Id,
                            Status = MatchStatus.Scheduled
                        };
                        matches.Add(match);
                    }
                }
            }
        }

        await _matchRepository.AddRangeAsync(matches);
        return matches;
    }

    public async Task<Match> ReportMatchResultAsync(int matchId, int winnerId, int reportedBy, string replayUrl)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
        {
            throw new ArgumentException($"Match with ID {matchId} not found.");
        }

        if (match.Status != MatchStatus.Scheduled)
        {
            throw new InvalidOperationException("Match has already been reported.");
        }

        if (winnerId != match.Player1Id && winnerId != match.Player2Id)
        {
            throw new ArgumentException("Winner must be one of the players in the match.");
        }

        if (reportedBy != match.Player1Id && reportedBy != match.Player2Id)
        {
            throw new UnauthorizedAccessException("Only players in the match can report results.");
        }

        // Only losers can report (as per requirements)
        if (reportedBy == winnerId)
        {
            throw new InvalidOperationException("Only the loser can report the match result.");
        }

        if (string.IsNullOrWhiteSpace(replayUrl) || !Uri.TryCreate(replayUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("A valid replay URL is required.");
        }

        match.WinnerId = winnerId;
        match.ReportedDate = DateTime.UtcNow;
        match.ReplayUrl = replayUrl;
        match.Status = MatchStatus.Reported;

        await _matchRepository.UpdateAsync(match);
        return match;
    }

    public async Task<List<Match>> GetMatchesByWeekAsync(int weekId)
    {
        return await _matchRepository.GetByWeekIdAsync(weekId);
    }

    public async Task<List<Match>> GetMatchesByPlayerAsync(int playerId)
    {
        return await _matchRepository.GetByPlayerIdAsync(playerId);
    }
}
