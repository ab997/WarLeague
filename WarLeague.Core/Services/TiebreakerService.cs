using WarLeague.Core.Model;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Services;

/// <summary>
/// Deterministic tiebreaker ranking: (1) team match W-L, (2) H2H among tied teams,
/// (3) series diff, (4) game diff, (5) series pct, (6) game pct, (7) SOS, (8) Team.Id asc.
/// Call with teams + completed matchups + matches; all aggregation and TB logic is internal.
/// </summary>
public class TiebreakerService
{
    private const int RankMultiplier = 1_000_000;

    /// <summary>
    /// Ranks teams using round-robin results. Pass completed team matchups and individual matches;
    /// returns ordered teams, tiebreaker values, and per-team stats for display.
    /// </summary>
    public TiebreakerRankingResult RankTeams(
        IEnumerable<Team> teams,
        IReadOnlyList<RoundRobinMatchup> matchups,
        IReadOnlyList<Match> matches)
    {
        var teamList = teams.ToList();
        var data = BuildAggregates(teamList, matchups, matches);

        var ordered = new List<Team>();
        var winsByTeamId = data.WinsByTeamId;
        var lossesByTeamId = data.LossesByTeamId;

        var groupsByWl = teamList
            .GroupBy(t => (W: winsByTeamId.GetValueOrDefault(t.Id, 0), L: lossesByTeamId.GetValueOrDefault(t.Id, 0)))
            .OrderByDescending(g => g.Key.W)
            .ThenBy(g => g.Key.L)
            .ToList();

        foreach (var group in groupsByWl)
        {
            var g = group.ToList();
            if (g.Count == 1)
            {
                ordered.Add(g[0]);
                continue;
            }

            var tiedIds = g.Select(t => t.Id).ToHashSet();
            var h2hByTeamId = ComputeH2HWithinSet(tiedIds, data.Matchups);
            var h2hSubgroups = g
                .GroupBy(t => h2hByTeamId.GetValueOrDefault(t.Id, 0))
                .OrderByDescending(x => x.Key)
                .Select(x => x.ToList())
                .ToList();

            foreach (var sub in h2hSubgroups)
                AppendResolved(ordered, sub, data);
        }

        var n = ordered.Count;
        var tiebreakerByTeamId = new Dictionary<int, int>();
        for (var i = 0; i < ordered.Count; i++)
            tiebreakerByTeamId[ordered[i].Id] = (n - i) * RankMultiplier - ordered[i].Id;

        var statsByTeamId = BuildStatsByTeamId(teamList, data);
        return new TiebreakerRankingResult(ordered, tiebreakerByTeamId, statsByTeamId);
    }

    private static RoundRobinWinsAndH2H BuildAggregates(
        List<Team> teams,
        IReadOnlyList<RoundRobinMatchup> matchups,
        IReadOnlyList<Match> matches)
    {
        var winsByTeamId = teams.ToDictionary(t => t.Id, _ => 0);
        var lossesByTeamId = teams.ToDictionary(t => t.Id, _ => 0);
        var h2hByTeamId = teams.ToDictionary(t => t.Id, _ => 0);
        var seriesWByTeamId = teams.ToDictionary(t => t.Id, _ => 0);
        var seriesLByTeamId = teams.ToDictionary(t => t.Id, _ => 0);
        var gamesWByTeamId = teams.ToDictionary(t => t.Id, _ => 0);
        var gamesLByTeamId = teams.ToDictionary(t => t.Id, _ => 0);

        foreach (var m in matchups)
        {
            if (!m.TeamWinnerId.HasValue) continue;
            if (m.MatchupType == MatchupType.Bye)
                winsByTeamId[m.TeamWinnerId.Value] = winsByTeamId.GetValueOrDefault(m.TeamWinnerId.Value, 0) + 1;
            else
            {
                winsByTeamId[m.TeamWinnerId.Value] = winsByTeamId.GetValueOrDefault(m.TeamWinnerId.Value, 0) + 1;
                var loserId = m.Team1Id == m.TeamWinnerId.Value ? m.Team2Id : m.Team1Id;
                lossesByTeamId[loserId] = lossesByTeamId.GetValueOrDefault(loserId, 0) + 1;
            }
        }

        foreach (var match in matches.Where(m => m.WinnerTeamId.HasValue))
        {
            var winnerId = match.WinnerTeamId!.Value;
            seriesWByTeamId[winnerId] = seriesWByTeamId.GetValueOrDefault(winnerId, 0) + 1;
            var loserTeamId = match.Team1Id == winnerId ? match.Team2Id : match.Team1Id;
            seriesLByTeamId[loserTeamId] = seriesLByTeamId.GetValueOrDefault(loserTeamId, 0) + 1;
            var p1Wins = match.Player1Wins ?? 0;
            var p2Wins = match.Player2Wins ?? 0;
            gamesWByTeamId[match.Team1Id] = gamesWByTeamId.GetValueOrDefault(match.Team1Id, 0) + p1Wins;
            gamesLByTeamId[match.Team1Id] = gamesLByTeamId.GetValueOrDefault(match.Team1Id, 0) + p2Wins;
            gamesWByTeamId[match.Team2Id] = gamesWByTeamId.GetValueOrDefault(match.Team2Id, 0) + p2Wins;
            gamesLByTeamId[match.Team2Id] = gamesLByTeamId.GetValueOrDefault(match.Team2Id, 0) + p1Wins;
        }

        var sosByTeamId = ComputeStrengthOfSchedule(teams, winsByTeamId, lossesByTeamId, matchups);
        return new RoundRobinWinsAndH2H(
            winsByTeamId, lossesByTeamId, h2hByTeamId, matchups.ToList(),
            seriesWByTeamId, seriesLByTeamId, gamesWByTeamId, gamesLByTeamId,
            sosByTeamId);
    }

    private static IReadOnlyDictionary<int, double> ComputeStrengthOfSchedule(
        List<Team> teams,
        IReadOnlyDictionary<int, int> winsByTeamId,
        IReadOnlyDictionary<int, int> lossesByTeamId,
        IReadOnlyList<RoundRobinMatchup> matchups)
    {
        var result = new Dictionary<int, double>();
        var nonBye = matchups.Where(m => m.MatchupType != MatchupType.Bye).ToList();
        foreach (var team in teams)
        {
            var opponentIds = nonBye
                .Where(m => m.Team1Id == team.Id || m.Team2Id == team.Id)
                .Select(m => m.Team1Id == team.Id ? m.Team2Id : m.Team1Id)
                .Distinct()
                .ToList();
            if (opponentIds.Count == 0) { result[team.Id] = 0; continue; }
            double sum = 0;
            foreach (var oppId in opponentIds)
            {
                var w = winsByTeamId.GetValueOrDefault(oppId, 0);
                var l = lossesByTeamId.GetValueOrDefault(oppId, 0);
                sum += (w + l) > 0 ? (double)w / (w + l) : 0;
            }
            result[team.Id] = sum / opponentIds.Count;
        }
        return result;
    }

    private static IReadOnlyDictionary<int, TiebreakerTeamStats> BuildStatsByTeamId(
        List<Team> teams,
        RoundRobinWinsAndH2H data)
    {
        var d = new Dictionary<int, TiebreakerTeamStats>();
        foreach (var t in teams)
        {
            var sw = data.SeriesWByTeamId.GetValueOrDefault(t.Id, 0);
            var sl = data.SeriesLByTeamId.GetValueOrDefault(t.Id, 0);
            var gw = data.GamesWByTeamId.GetValueOrDefault(t.Id, 0);
            var gl = data.GamesLByTeamId.GetValueOrDefault(t.Id, 0);
            var seriesTotal = sw + sl;
            var gamesTotal = gw + gl;
            d[t.Id] = new TiebreakerTeamStats(
                Wins: data.WinsByTeamId.GetValueOrDefault(t.Id, 0),
                Losses: data.LossesByTeamId.GetValueOrDefault(t.Id, 0),
                SeriesDiff: sw - sl,
                GameDiff: gw - gl,
                SeriesPct: seriesTotal > 0 ? (double)sw / seriesTotal : 0,
                GamePct: gamesTotal > 0 ? (double)gw / gamesTotal : 0,
                SOS: data.SOSByTeamId?.GetValueOrDefault(t.Id));
        }
        return d;
    }

    private void AppendResolved(List<Team> ordered, List<Team> subgroup, RoundRobinWinsAndH2H data, int criterionIndex = 0)
    {
        if (subgroup.Count == 1) { ordered.Add(subgroup[0]); return; }

        var seriesW = data.SeriesWByTeamId;
        var seriesL = data.SeriesLByTeamId;
        var gamesW = data.GamesWByTeamId;
        var gamesL = data.GamesLByTeamId;
        var sos = data.SOSByTeamId;

        if (criterionIndex >= 7)
        {
            foreach (var t in subgroup.OrderBy(t => t.Id))
                ordered.Add(t);
            return;
        }

        List<List<Team>> partitions = criterionIndex switch
        {
            0 => PartitionBy(subgroup, t => (seriesW.GetValueOrDefault(t.Id, 0) - seriesL.GetValueOrDefault(t.Id, 0))),
            1 => PartitionBy(subgroup, t => (gamesW.GetValueOrDefault(t.Id, 0) - gamesL.GetValueOrDefault(t.Id, 0))),
            2 => PartitionBy(subgroup, t =>
            {
                var sw = seriesW.GetValueOrDefault(t.Id, 0);
                var sl = seriesL.GetValueOrDefault(t.Id, 0);
                return (sw + sl) > 0 ? (double)sw / (sw + sl) : 0d;
            }),
            3 => PartitionBy(subgroup, t =>
            {
                var gw = gamesW.GetValueOrDefault(t.Id, 0);
                var gl = gamesL.GetValueOrDefault(t.Id, 0);
                return (gw + gl) > 0 ? (double)gw / (gw + gl) : 0d;
            }),
            4 => sos != null ? PartitionBy(subgroup, t => sos.GetValueOrDefault(t.Id, 0d)) : new List<List<Team>> { subgroup },
            _ => new List<List<Team>> { subgroup }
        };

        if (criterionIndex == 4 && sos == null)
        {
            AppendResolved(ordered, subgroup, data, 7);
            return;
        }

        foreach (var p in partitions)
        {
            if (p.Count == 1) ordered.Add(p[0]);
            else AppendResolved(ordered, p, data, criterionIndex + 1);
        }
    }

    private static List<List<Team>> PartitionBy<TKey>(List<Team> teams, Func<Team, TKey> keySelector) where TKey : IComparable
    {
        return teams.GroupBy(keySelector).OrderByDescending(g => g.Key).Select(g => g.ToList()).ToList();
    }

    private static IReadOnlyDictionary<int, int> ComputeH2HWithinSet(
        IReadOnlySet<int> tiedSet,
        IReadOnlyList<RoundRobinMatchup> matchups)
    {
        var h2h = new Dictionary<int, int>();
        foreach (var m in matchups.Where(m =>
            m.TeamWinnerId.HasValue &&
            m.MatchupType != MatchupType.Bye &&
            tiedSet.Contains(m.Team1Id) &&
            tiedSet.Contains(m.Team2Id)))
        {
            var wid = m.TeamWinnerId!.Value;
            h2h[wid] = h2h.GetValueOrDefault(wid, 0) + 1;
        }
        return h2h;
    }
}

public record TiebreakerRankingResult(
    IReadOnlyList<Team> OrderedTeams,
    IReadOnlyDictionary<int, int> TiebreakerByTeamId,
    IReadOnlyDictionary<int, TiebreakerTeamStats> StatsByTeamId);
