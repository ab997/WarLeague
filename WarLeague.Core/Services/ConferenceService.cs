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

    public async Task<BaseResult> CreateAsync(int seasonId, string name, int playoffTeamsCount)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new BaseResult(false, "Conference name is required.");
        }

        var season = await _seasonRepository.GetById(seasonId);

        Conference? existingConference = await _conferenceRepository.GetByNameAndSeasonAsync(name.Trim(), seasonId);
        if (existingConference is not null)
        {
            return new BaseResult(false, $"Conference '{name}' already exists.");
        }

        // Validate playoff team count
        if (playoffTeamsCount <= 0)
        {
            return new BaseResult(false, "Playoff team count cannot be negative.");
        }

        Conference conference = new Conference
        {
            Name = name.Trim(),
            SeasonId = seasonId,
            PlayoffTeamsCount = playoffTeamsCount
        };

        await _conferenceRepository.AddAsync(conference);

        string message = playoffTeamsCount > 0
            ? $"Conference '{conference.Name}' created (playoff teams: {playoffTeamsCount})."
            : $"Conference '{conference.Name}' created (no playoffs).";

        return new BaseResult(true, message);
    }

    public async Task<BaseResult> UpdateAsync(int seasonId, string currentName, string? newName = null, int? playoffTeamsCount = null)
    {
        if (string.IsNullOrWhiteSpace(currentName))
        {
            return new BaseResult(false, "Current conference name is required.");
        }

        Conference? conference = await _conferenceRepository.GetByNameAndSeasonAsync(currentName.Trim(), seasonId);
        if (conference is null)
        {
            return new BaseResult(false, $"Conference '{currentName}' was not found.");
        }

        bool hasChanges = false;
        var changes = new List<string>();

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(newName))
        {
            Conference? conflict = await _conferenceRepository.GetByNameAndSeasonAsync(newName.Trim(), seasonId);
            if (conflict is not null && conflict.Id != conference.Id)
            {
                return new BaseResult(false, $"Conference '{newName}' already exists.");
            }

            conference.Name = newName.Trim();
            hasChanges = true;
            changes.Add($"renamed to '{conference.Name}'");
        }

        // Update playoff team count if provided
        if (playoffTeamsCount.HasValue)
        {
            const int maxPlayoffTeams = 32;
            if (playoffTeamsCount.Value < 0)
            {
                return new BaseResult(false, "Playoff team count cannot be negative.");
            }
            if (playoffTeamsCount.Value > maxPlayoffTeams)
            {
                return new BaseResult(false, $"Playoff team count cannot exceed {maxPlayoffTeams}.");
            }

            conference.PlayoffTeamsCount = playoffTeamsCount.Value;
            hasChanges = true;
            if (playoffTeamsCount.Value > 0)
            {
                changes.Add($"playoff teams set to {playoffTeamsCount.Value}");
            }
            else
            {
                changes.Add("playoff teams cleared");
            }
        }

        if (!hasChanges)
        {
            return new BaseResult(false, "No changes specified.");
        }

        await _conferenceRepository.UpdateAsync(conference);

        string message = $"Conference '{currentName}' {string.Join(", ", changes)}.";
        return new BaseResult(true, message);
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
        _ = await _seasonRepository.GetById(seasonId);

        List<Conference> conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
        if (conferences.Count == 0)
        {
            return new BaseResult(true, "No conferences found for the active season.");
        }

        string conferenceLines = string.Join(Environment.NewLine, conferences.Select(c =>
        {
            if (c.PlayoffTeamsCount > 0)
            {
                return $"- {c.Name} (playoff teams: {c.PlayoffTeamsCount})";
            }
            else
            {
                return $"- {c.Name} (no playoffs)";
            }
        }));

        return new BaseResult(true, $"Conferences:{Environment.NewLine}{conferenceLines}");
    }

}
