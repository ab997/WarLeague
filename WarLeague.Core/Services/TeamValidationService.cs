using WarLeague.Data.Entities;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Services;

public class TeamValidationService
{
    private readonly TeamRepository _teamRepository;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    private readonly SeasonRepository _seasonRepository;

    public TeamValidationService(TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository, SeasonRepository seasonRepository)
    {
        _teamRepository = teamRepository;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _seasonRepository = seasonRepository;
    }

    public async Task<BaseResult> ValidateAllTeamsInSeasonAsync(int seasonId)
    {
        Season? season = await _seasonRepository.GetByIdOrDefault(seasonId);

        if (season is null)
        {
            return new BaseResult { Success = false, Message = $"Season with ID '{seasonId}' does not exist." };
        }

        var teams = await _teamRepository.GetBySeasonAsync(seasonId);

        if (teams.Count == 0)
        {
            return new BaseResult { Success = false, Message = "No teams found in this season." };
        }

        var psts = await _playerSeasonTeamRepository.GetBySeasonAsync(seasonId);

        var invalidTeams = new List<string>();

        foreach (var team in teams.OrderBy(t => t.Name))
        {
            var teamMemberCount = psts.Count(p => p.TeamId == team.Id);

            if (teamMemberCount < season.MinimumTeamMembers)
            {
                invalidTeams.Add($"{team.Name} ({teamMemberCount}/{season.MinimumTeamMembers})");
            }
        }

        if (invalidTeams.Count > 0)
        {
            return new BaseResult
            {
                Success = false,
                Message = $"Cannot proceed because not all teams meet minimum member requirement ({season.MinimumTeamMembers}):\n" +
                    string.Join("\n", invalidTeams)
            };
        }

        return new BaseResult { Success = true, Message = "All teams are valid." };
    }
}
