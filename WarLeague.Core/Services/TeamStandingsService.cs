using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Services;

public class TeamStandingsService
{
    private readonly TeamStandingsRepository _teamStandingsRepository;
    private readonly RoundRobinMatchupRepository _roundRobinMatchupRepository;
    private readonly WeekRepository _weekRepository;
    private readonly ConferenceRepository _conferenceRepository;
    private readonly TeamRepository _teamRepository;
    private readonly PlayoffMatchupRepository _playoffMatchupRepository;
    private readonly SeasonRepository _seasonRepository;

    public TeamStandingsService(
        TeamStandingsRepository teamStandingsRepository,
        RoundRobinMatchupRepository roundRobinMatchupRepository,
        WeekRepository weekRepository,
        ConferenceRepository conferenceRepository,
        TeamRepository teamRepository,
        PlayoffMatchupRepository playoffMatchupRepository,
        SeasonRepository seasonRepository)
    {
        _teamStandingsRepository = teamStandingsRepository;
        _roundRobinMatchupRepository = roundRobinMatchupRepository;
        _weekRepository = weekRepository;
        _conferenceRepository = conferenceRepository;
        _teamRepository = teamRepository;
        _playoffMatchupRepository = playoffMatchupRepository;
        _seasonRepository = seasonRepository;
    }

    /// <summary>
    /// Default tiebreaker: higher wins first, then lower Team.Id.
    /// Used to order teams when generating seeds.
    /// </summary>
    private static int GetTiebreakerValue(int wins, int teamId)
    {
        // Encode so that OrderByDescending(tiebreaker) gives: wins desc, then teamId asc
        // Use a large multiplier so wins dominate (e.g. wins * 1_000_000 - teamId)
        return wins * 1_000_000 - teamId;
    }

    /// <summary>
    /// Generates TeamStandings from round-robin results: computes wins per team,
    /// takes top N per conference by PlayoffTeamsCount, then assigns Seed 1..N and Tiebreaker.
    /// Overwrites any existing standings for the season.
    /// </summary>
    public async Task<BaseResult> GenerateStandingsFromRoundRobinAsync(int seasonId)
    {
        var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);
        if (season.Phase != SeasonPhase.Playoffs)
        {
            return new BaseResult(false, "Standings can only be generated when season is in Playoffs phase.");
        }
        var existingPlayoffMatchups = await _playoffMatchupRepository.GetBySeasonIdAsync(seasonId);
        if (existingPlayoffMatchups.Count > 0)
        {
            return new BaseResult(false, "Cannot generate or regenerate standings: playoff matchups already exist for this season.");
        }

        var teams = await _teamRepository.GetBySeasonAsync(seasonId);
        var completedWeeks = (await _weekRepository.GetBySeasonAsync(seasonId))
            .Where(w => w.Status == WeekStatus.Completed)
            .OrderBy(w => w.WeekNumber)
            .ToList();

        var winsByTeamId = new Dictionary<int, int>();
        foreach (var t in teams)
            winsByTeamId[t.Id] = 0;

        foreach (var week in completedWeeks)
        {
            var matchups = await _roundRobinMatchupRepository.GetByWeekIdAsync(week.Id);
            foreach (var m in matchups.Where(m => m.TeamWinnerId.HasValue))
            {
                winsByTeamId[m.TeamWinnerId!.Value] = winsByTeamId.GetValueOrDefault(m.TeamWinnerId.Value, 0) + 1;
            }
        }

        var conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
        var playoffTeams = new List<Team>();

        foreach (var conference in conferences.Where(c => c.PlayoffTeamsCount > 0))
        {
            var conferenceTeams = teams.Where(t => t.ConferenceId == conference.Id).ToList();
            var seeded = conferenceTeams
                .OrderByDescending(t => winsByTeamId.GetValueOrDefault(t.Id, 0))
                .ThenBy(t => t.Id)
                .Take(conference.PlayoffTeamsCount)
                .ToList();
            playoffTeams.AddRange(seeded);
        }

        // Global seed order: same as current PlayoffService (wins desc, then Team.Id)
        var seededOrder = playoffTeams
            .OrderByDescending(t => winsByTeamId.GetValueOrDefault(t.Id, 0))
            .ThenBy(t => t.Id)
            .ToList();

        await _teamStandingsRepository.DeleteBySeasonIdAsync(seasonId);

        var standings = new List<TeamStandings>();
        for (var i = 0; i < seededOrder.Count; i++)
        {
            var team = seededOrder[i];
            var wins = winsByTeamId.GetValueOrDefault(team.Id, 0);
            standings.Add(new TeamStandings
            {
                SeasonId = seasonId,
                TeamId = team.Id,
                Seed = i + 1,
                Tiebreaker = GetTiebreakerValue(wins, team.Id),
                Wins = wins
            });
        }

        if (standings.Count > 0)
            await _teamStandingsRepository.AddRangeAsync(standings);

        return new BaseResult(true, $"Generated standings for {standings.Count} playoff team(s).");
    }

    /// <summary>
    /// Returns standings for the season (for display). Only valid when season is in Playoffs.
    /// </summary>
    public async Task<List<TeamStandings>> GetStandingsForSeasonAsync(int seasonId)
    {
        return await _teamStandingsRepository.GetBySeasonIdAsync(seasonId);
    }

    /// <summary>
    /// Returns true if standings are editable: season is Playoffs and no playoff matchups exist yet.
    /// </summary>
    public async Task<bool> CanEditStandingsAsync(int seasonId)
    {
        var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);
        if (season.Phase != SeasonPhase.Playoffs)
            return false;
        var playoffMatchups = await _playoffMatchupRepository.GetBySeasonIdAsync(seasonId);
        return playoffMatchups.Count == 0;
    }

    public async Task<BaseResult> UpdateSeedAsync(int seasonId, int teamId, int newSeed)
    {
        var canEdit = await CanEditStandingsAsync(seasonId);
        if (!canEdit)
            return new BaseResult(false, "Standings cannot be edited: season must be in Playoffs phase and no playoff matchups may exist yet.");

        var standings = await _teamStandingsRepository.GetBySeasonIdWithoutTeamAsync(seasonId);
        var count = standings.Count;
        if (count == 0)
            return new BaseResult(false, "No standings exist for this season.");
        if (newSeed < 1 || newSeed > count)
            return new BaseResult(false, $"Seed must be between 1 and {count}.");

        var entry = standings.FirstOrDefault(s => s.TeamId == teamId);
        if (entry == null)
            return new BaseResult(false, "Team is not in playoff standings.");

        var oldSeed = entry.Seed;
        if (oldSeed == newSeed)
            return new BaseResult(true, "Seed unchanged.");

        var other = standings.FirstOrDefault(s => s.Seed == newSeed);
        if (other != null)
        {
            // Use a temporary unique seed to avoid violating (SeasonId, Seed) unique index during swap
            var tempSeed = -entry.TeamId;
            entry.Seed = tempSeed;
            await _teamStandingsRepository.UpdateAsync(entry);
            other.Seed = oldSeed;
            entry.Seed = newSeed;
            await _teamStandingsRepository.UpdateRangeAsync(new[] { other, entry });
        }
        else
        {
            entry.Seed = newSeed;
            await _teamStandingsRepository.UpdateAsync(entry);
        }
        return new BaseResult(true, $"Seed updated to {newSeed}.");
    }

    public async Task<BaseResult> UpdateTiebreakerAsync(int seasonId, int teamId, int tiebreaker)
    {
        var canEdit = await CanEditStandingsAsync(seasonId);
        if (!canEdit)
            return new BaseResult(false, "Standings cannot be edited: season must be in Playoffs phase and no playoff matchups may exist yet.");

        var entry = await _teamStandingsRepository.GetBySeasonIdAndTeamIdAsync(seasonId, teamId);
        if (entry == null)
            return new BaseResult(false, "Team is not in playoff standings.");
        entry.Tiebreaker = tiebreaker;
        await _teamStandingsRepository.UpdateAsync(entry);
        return new BaseResult(true, "Tiebreaker updated.");
    }
}
