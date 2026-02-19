
using Microsoft.SqlServer.Server;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Data;

namespace WarLeague.Core.Services
{
    public class SeasonService
    {
        private readonly SeasonRepository _seasonRepository;
        private readonly FormatRepository _formatRepository;
        private readonly ConferenceRepository _conferenceRepository;
        private readonly WeekRepository _weekRepository;
        private readonly PlayoffService _playoffService;
        private readonly WarLeagueDbContext _context;

        public SeasonService(SeasonRepository seasonRepository, FormatRepository formatRepository, ConferenceRepository conferenceRepository, WeekRepository weekRepository, PlayoffService playoffService, WarLeagueDbContext context)
        {
            _seasonRepository = seasonRepository;
            _formatRepository = formatRepository;
            _conferenceRepository = conferenceRepository;
            _weekRepository = weekRepository;
            _playoffService = playoffService;
            _context = context;
        }

        public async Task<BaseResult> CreateAsync(int formatId, int seasonNumber, int minTeamMembers)
        {
            var format = await _formatRepository.GetByIdAsync(formatId);
            var existing = format.Seasons.SingleOrDefault(s => s.SeasonNumber == seasonNumber);

            if (existing is not null)
            {
                return new BaseResult(false, $"Season with number {seasonNumber} already exists.");
            }

            var season = new Season
            {
                SeasonNumber = seasonNumber,
                Format = format,
                MinimumTeamMembers = minTeamMembers,
                Active = false
            };

            await _seasonRepository.AddAsync(season);

            return new BaseResult(true, $"Season '{seasonNumber}' created (inactive).");
        }

        public async Task<BaseResult> DeleteAsync(int formatId, int seasonNumber)
        {
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(seasonNumber, formatId);

            if (season == null)
            {
                return new BaseResult(false, $"Season with number {seasonNumber} not found.");
            }

            await _seasonRepository.DeleteAsync(season);

            return new BaseResult(true, $"Season '{seasonNumber}' deleted.");
        }

        public async Task<SeasonResult> SetActiveAsync(int formatId, int seasonNumber)
        {
            var season = await _seasonRepository.GetBySeasonNumberAndFormatAsync(seasonNumber, formatId);

            if (season == null)
            {
                return new SeasonResult { Success = false,  Message = $"Season with number {seasonNumber} not found." };
            }
            var allSeasons = await _seasonRepository.GetAllByFormatAsync(formatId);

            foreach (var s in allSeasons)
                s.Active = false;

            await _seasonRepository.UpdateRangeAsync(allSeasons);

            season.Active = true;
            await _seasonRepository.UpdateAsync(season);

            return new SeasonResult { Success = true, Message = $"Season '{seasonNumber}' is now active.", Season = season };
        }

        public async Task<SeasonResult> SetTeamModificationsAsync(int seasonId, bool enabled) 
        {
            var season = await _seasonRepository.GetById(seasonId);
            season.DisableTeamModification = !enabled;

            await _seasonRepository.UpdateAsync(season);
            return new SeasonResult{ Success = true, Message = $"Captain team modifications have been {(enabled ? "enabled" : "disabled")} for season {season.SeasonNumber}.", Season = season };
        }

        public async Task<BaseResult> SetPhaseToPlayoffsAsync(int seasonId)
        {
            //TODO: transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            var season = await _seasonRepository.GetById(seasonId);

            if (season.Phase == SeasonPhase.Playoffs)
            {
                return new BaseResult(false, "Season is already in Playoffs phase. Phase cannot be reversed.");
            }

            var weeks = await _weekRepository.GetBySeasonAsync(seasonId);
            var unfinishedWeeks = weeks.Where(w => w.Status != WeekStatus.Completed).ToList();
            if (unfinishedWeeks.Count > 0)
            {
                var weekNumbers = string.Join(", ", unfinishedWeeks.Select(w => $"Week {w.WeekNumber} ({w.Status})"));
                return new BaseResult(false, $"Cannot switch to Playoffs: all round-robin weeks must be completed. Unfinished: {weekNumbers}.");
            }

            if (weeks.Count == 0)
            {
                return new BaseResult(false, "Cannot switch to Playoffs: season has no weeks. Create and complete at least one round-robin week first.");
            }

            // Validate that at least one conference has playoff team count configured
            var conferences = await _conferenceRepository.GetBySeasonAsync(seasonId);
            bool hasNoPlayoffConfig = conferences.Any(c => c.PlayoffTeamsCount == 0);

            if (hasNoPlayoffConfig)
            {
                return new BaseResult(false, "Cannot switch to Playoffs: there are conferences who have not set the number of teams to advance to playoffs");
            }

            season.Phase = SeasonPhase.Playoffs;
            await _seasonRepository.UpdateAsync(season);

            var (_, playoffTeams, nonPlayoffTeams) = await _playoffService.GetFirstPlayoffWeekMatchupsAndQualifiersAsync(seasonId);
            var playoffNames = playoffTeams.Count > 0 ? string.Join(", ", playoffTeams.Select(t => t.Name)) : "(none)";
            var nonPlayoffNames = nonPlayoffTeams.Count > 0 ? string.Join(", ", nonPlayoffTeams.Select(t => t.Name)) : "(none)";
            string message = $"Season {season.SeasonNumber} switched to Playoffs phase.\n\n**Playoff teams:** {playoffNames}\n\n**Did not qualify:** {nonPlayoffNames}";

            await transaction.CommitAsync();

            return new BaseResult(true, message);
        }
    }
}
