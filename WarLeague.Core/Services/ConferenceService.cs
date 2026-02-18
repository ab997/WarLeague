using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Services;

public class ConferenceService
{
    private readonly ConferenceRepository _conferenceRepository;
    private readonly SeasonRepository _seasonRepository;

    public ConferenceService(ConferenceRepository conferenceRepository, SeasonRepository seasonRepository)
    {
        _conferenceRepository = conferenceRepository;
        _seasonRepository = seasonRepository;
    }

    public async Task<BaseResult> CreateAsync(int seasonId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new BaseResult(false, "Conference name is required.");
        }

        Season? season = await _seasonRepository.GetByIdOrDefault(seasonId);
        if (season is null)
        {
            return new BaseResult(false, $"Season with ID '{seasonId}' does not exist.");
        }

        Conference? existingConference = await _conferenceRepository.GetByNameAndSeasonAsync(name.Trim(), seasonId);
        if (existingConference is not null)
        {
            return new BaseResult(false, $"Conference '{name}' already exists.");
        }

        Conference conference = new Conference
        {
            Name = name.Trim(),
            SeasonId = seasonId
        };

        await _conferenceRepository.AddAsync(conference);

        return new BaseResult(true, $"Conference '{conference.Name}' created.");
    }

    public async Task<BaseResult> UpdateAsync(int seasonId, string currentName, string newName)
    {
        if (string.IsNullOrWhiteSpace(currentName))
        {
            return new BaseResult(false, "Current conference name is required.");
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            return new BaseResult(false, "New conference name is required.");
        }

        Conference? conference = await _conferenceRepository.GetByNameAndSeasonAsync(currentName.Trim(), seasonId);
        if (conference is null)
        {
            return new BaseResult(false, $"Conference '{currentName}' was not found.");
        }

        Conference? conflict = await _conferenceRepository.GetByNameAndSeasonAsync(newName.Trim(), seasonId);
        if (conflict is not null)
        {
            return new BaseResult(false, $"Conference '{newName}' already exists.");
        }

        conference.Name = newName.Trim();
        await _conferenceRepository.UpdateAsync(conference);

        return new BaseResult(true, $"Conference '{currentName}' renamed to '{conference.Name}'.");
    }

    public async Task<BaseResult> DeleteAsync(int seasonId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new BaseResult(false, "Conference name is required.");
        }

        Conference? conference = await _conferenceRepository.GetByNameAndSeasonAsync(name.Trim(), seasonId);
        if (conference is null)
        {
            return new BaseResult(false, $"Conference '{name}' was not found.");
        }

        bool hasTeams = await _conferenceRepository.HasTeamsAsync(conference.Id);
        if (hasTeams)
        {
            return new BaseResult(false, $"Conference '{name}' cannot be deleted because teams are assigned to it.");
        }

        await _conferenceRepository.DeleteAsync(conference);

        return new BaseResult(true, $"Conference '{name}' deleted.");
    }

    public async Task<BaseResult> ListAsync(int seasonId)
    {
        Season? season = await _seasonRepository.GetByIdOrDefault(seasonId);
        if (season is null)
        {
            return new BaseResult(false, $"Season with ID '{seasonId}' does not exist.");
        }

        List<Conference> conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
        if (conferences.Count == 0)
        {
            return new BaseResult(true, "No conferences found for the active season.");
        }

        string conferenceLines = string.Join(Environment.NewLine, conferences.Select(c => $"- {c.Name}"));

        return new BaseResult(true, $"Conferences:{Environment.NewLine}{conferenceLines}");
    }
}
