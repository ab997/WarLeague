using WarLeague.Core.Model;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Services;

public class TiebreakerService
{
    public IReadOnlyDictionary<int, decimal> RankTeams(
        IEnumerable<Team> teams,
        IReadOnlyList<RoundRobinMatchup> matchups,
        IReadOnlyList<Match> matches)
    {
        var teamList = teams.ToList();
        var teamIds = teamList.Select(t => t.Id).ToHashSet();

        // Overall round-robin wins / losses (includes Byes as wins, like display stats).
        var winsByTeamId = teamList.ToDictionary(t => t.Id, _ => 0);
        var lossesByTeamId = teamList.ToDictionary(t => t.Id, _ => 0);

        foreach (var m in matchups)
        {
            if (!m.TeamWinnerId.HasValue)
                continue;

            var winnerId = m.TeamWinnerId.Value;
            if (teamIds.Contains(winnerId))
                winsByTeamId[winnerId] = winsByTeamId.GetValueOrDefault(winnerId, 0) + 1;

            if (m.MatchupType == MatchupType.Bye)
                continue;

            var loserId = m.Team1Id == winnerId ? m.Team2Id : m.Team1Id;
            if (teamIds.Contains(loserId))
                lossesByTeamId[loserId] = lossesByTeamId.GetValueOrDefault(loserId, 0) + 1;
        }

        // Series wins / losses from individual matches (used as secondary tiebreaker),
        // and per-game wins / losses using Player1Wins / Player2Wins (used as an
        // additional, more granular tiebreaker).
        var seriesWByTeamId = teamList.ToDictionary(t => t.Id, _ => 0);
        var seriesLByTeamId = teamList.ToDictionary(t => t.Id, _ => 0);
        var gamesWByTeamId = teamList.ToDictionary(t => t.Id, _ => 0);
        var gamesLByTeamId = teamList.ToDictionary(t => t.Id, _ => 0);

        foreach (var match in matches.Where(m => m.WinnerTeamId.HasValue))
        {
            var winnerId = match.WinnerTeamId!.Value;
            var loserId = match.Team1Id == winnerId ? match.Team2Id : match.Team1Id;

            if (teamIds.Contains(winnerId))
                seriesWByTeamId[winnerId] = seriesWByTeamId.GetValueOrDefault(winnerId, 0) + 1;

            if (teamIds.Contains(loserId))
                seriesLByTeamId[loserId] = seriesLByTeamId.GetValueOrDefault(loserId, 0) + 1;

            // Per-game wins/losses using Player1Wins / Player2Wins.
            var p1Wins = match.Player1Wins ?? 0;
            var p2Wins = match.Player2Wins ?? 0;

            if (teamIds.Contains(match.Team1Id))
            {
                gamesWByTeamId[match.Team1Id] = gamesWByTeamId.GetValueOrDefault(match.Team1Id, 0) + p1Wins;
                gamesLByTeamId[match.Team1Id] = gamesLByTeamId.GetValueOrDefault(match.Team1Id, 0) + p2Wins;
            }
            if (teamIds.Contains(match.Team2Id))
            {
                gamesWByTeamId[match.Team2Id] = gamesWByTeamId.GetValueOrDefault(match.Team2Id, 0) + p2Wins;
                gamesLByTeamId[match.Team2Id] = gamesLByTeamId.GetValueOrDefault(match.Team2Id, 0) + p1Wins;
            }
        }

        // Head-to-head matrix for all teams in the candidate set (only non-bye matchups).
        var h2h = teamList.ToDictionary(t => t.Id, _ => new Dictionary<int, int>());

        foreach (var m in matchups.Where(m => m.MatchupType != MatchupType.Bye && m.TeamWinnerId.HasValue))
        {
            var t1 = m.Team1Id;
            var t2 = m.Team2Id;

            if (!teamIds.Contains(t1) || !teamIds.Contains(t2))
                continue;

            var winnerId = m.TeamWinnerId!.Value;
            var loserId = winnerId == t1 ? t2 : t1;

            if (!h2h[winnerId].ContainsKey(loserId))
                h2h[winnerId][loserId] = 0;
            if (!h2h[loserId].ContainsKey(winnerId))
                h2h[loserId][winnerId] = 0;

            h2h[winnerId][loserId] += 1;
            h2h[loserId][winnerId] -= 1;
        }

        // Primary: overall wins-losses; Secondary: H2H among tied teams;
        // Tertiary: series diff (seriesW - seriesL); Quaternary: game diff using Player1Wins/Player2Wins.
        const decimal WinsWeight = 1_000_000m;
        const decimal H2HWeight = 1_000m;
        const decimal SeriesWeight = 1m;
        const decimal GameWeight = 0.001m;

        var rawScores = new Dictionary<int, decimal>();

        // Group by overall record so that H2H is only applied within tied groups.
        var teamsByRecord = teamList
            .GroupBy(t =>
            {
                var id = t.Id;
                var w = winsByTeamId.GetValueOrDefault(id, 0);
                var l = lossesByTeamId.GetValueOrDefault(id, 0);
                return w - l;
            });

        foreach (var group in teamsByRecord)
        {
            var groupList = group.ToList();
            var groupIds = groupList.Select(t => t.Id).ToHashSet();

            foreach (var team in groupList)
            {
                var id = team.Id;
                var w = winsByTeamId.GetValueOrDefault(id, 0);
                var l = lossesByTeamId.GetValueOrDefault(id, 0);
                var recordScore = w - l;

                // Head-to-head score only against other teams with same record.
                var h2hScore = 0;
                if (h2h.TryGetValue(id, out var vs))
                {
                    foreach (var kvp in vs)
                    {
                        var oppId = kvp.Key;
                        var diff = kvp.Value;
                        if (groupIds.Contains(oppId))
                            h2hScore += diff;
                    }
                }

                var seriesW = seriesWByTeamId.GetValueOrDefault(id, 0);
                var seriesL = seriesLByTeamId.GetValueOrDefault(id, 0);
                var seriesDiff = seriesW - seriesL;
                var gamesW = gamesWByTeamId.GetValueOrDefault(id, 0);
                var gamesL = gamesLByTeamId.GetValueOrDefault(id, 0);
                var gameDiff = gamesW - gamesL;

                var score = recordScore * WinsWeight
                            + h2hScore * H2HWeight
                            + seriesDiff * SeriesWeight
                            + gameDiff * GameWeight;

                rawScores[id] = score;
            }
        }

        // Normalize to dense tiebreaker values so that, for the ordered list of teams,
        // tiebreaker = (n - rankIndex) * 1_000_000 - TeamId. This keeps all tiebreaker
        // semantics centralized here; callers only see the final ranking values.
        var ordered = teamList
            .OrderByDescending(t => rawScores.GetValueOrDefault(t.Id, 0))
            .ThenBy(t => t.Id)
            .ToList();

        var n = ordered.Count;
        var normalized = new Dictionary<int, decimal>(n);
        for (var i = 0; i < n; i++)
        {
            var team = ordered[i];
            var score = (n - i) * 1_000_000m - team.Id;
            normalized[team.Id] = score;
        }

        return normalized;
    }
}

