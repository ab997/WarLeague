using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Services
{
    public class DeckSubmissionService
    {
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly TeamRepository _teamRepository;
        private readonly WeekRepository _weekRepository;
        private readonly DeckSubmissionRepository _deckSubmissionRepository;
        private readonly MatchupServiceFactory _matchupServiceFactory;
        private readonly SeasonRepository _seasonRepository;

        public DeckSubmissionService(
            PlayerSeasonTeamRepository playerSeasonTeamRepository,
            TeamRepository teamRepository,
            WeekRepository weekRepository,
            DeckSubmissionRepository deckSubmissionRepository,
            MatchupServiceFactory matchupServiceFactory,
            SeasonRepository seasonRepository)
        {
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _teamRepository = teamRepository;
            _weekRepository = weekRepository;
            _deckSubmissionRepository = deckSubmissionRepository;
            _matchupServiceFactory = matchupServiceFactory;
            _seasonRepository = seasonRepository;
        }
        public async Task<BaseResult> SubmitAsync(int seasonId, int playerId, string deckContent, int seatNumber)
        {
            (BaseResult value, Week? openWeek) = await EnsureSingleValidOpenWeekAsync(seasonId);
            if (!value.Success || openWeek is null)
            {
                return value;
            }

            if (seatNumber < 1 || seatNumber > openWeek.SubmissionsRequired)
            {
                return new BaseResult 
                { 
                    Success = false, 
                    Message = $"Seat number must be between 1 and {openWeek.SubmissionsRequired} for this week." 
                };
            }

            var pst = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(playerId, seasonId);
            if (pst is null)
            {
                return new BaseResult { Success = false, Message = "Player is not on any team for the active season." };
            }

            var season = await _seasonRepository.GetByIdOrDefault(openWeek.SeasonId);
            if (season is null)
            {
                return new BaseResult { Success = false, Message = "Season not found for the open week." };
            }
            var matchupService = _matchupServiceFactory.GetMatchupService(season);
            var canSubmitResult = await matchupService.ValidateTeamCanSubmitForWeekAsync(season, openWeek, pst.TeamId);
            if (!canSubmitResult.Success)
            {
                return canSubmitResult;
            }

            // Check if seat is already taken by a different player on the team
            var seatTaken = await _deckSubmissionRepository.GetBySeatAndWeekAndTeamAsync(seatNumber, openWeek.Id, pst.TeamId);
            if (seatTaken != null && seatTaken.PlayerId != playerId)
            {
                return new BaseResult 
                { 
                    Success = false, 
                    Message = $"Seat {seatNumber} is already taken by {seatTaken.Player.UserName}. Please delete their submission first or choose a different seat." 
                };
            }

            // Upsert submission for (player, week)
            var existing = await _deckSubmissionRepository.GetByPlayerAndWeekAsync(playerId, openWeek.Id);
            if (existing != null)
            {
                existing.DeckFile = deckContent;
                existing.SubmittedDate = DateTime.UtcNow;
                existing.SeatNumber = seatNumber;
                await _deckSubmissionRepository.UpdateAsync(existing);
                return new BaseResult { Success = true, Message = $"Deck **updated** for {pst.Player.UserName} for week {openWeek.WeekNumber} (season {pst.Season.SeasonNumber}) at seat {seatNumber}." };
            }

            await _deckSubmissionRepository.AddAsync(new DeckSubmission
            {
                PlayerId = playerId,
                WeekId = openWeek.Id,
                DeckFile = deckContent,
                SubmittedDate = DateTime.UtcNow,
                SeatNumber = seatNumber
            });

            return new BaseResult { Success = true, Message = $"Deck submitted for {pst.Player.UserName} for week {openWeek.WeekNumber} (season {pst.Season.SeasonNumber}) at seat {seatNumber}." };
        }

        public async Task<BaseResult> DeleteSubmissionAsync(int seasonId, int playerId)
        {
            (BaseResult value, Week? openWeek) = await EnsureSingleValidOpenWeekAsync(seasonId);
            if (!value.Success || openWeek is null)
            {
                return value;
            }

            bool deleted = await _deckSubmissionRepository.DeleteByPlayerAndWeekAsync(playerId, openWeek.Id);
            if (!deleted)
            {
                return new BaseResult { Success = false, Message = $"No existing deck submission found to delete for player on week {openWeek.WeekNumber}." };
            }

            return new BaseResult { Success = true, Message = $"Deck submission deleted for player for week {openWeek.WeekNumber}." };
        }

        private async Task<(BaseResult value, Week? openWeek)> EnsureSingleValidOpenWeekAsync(int seasonId)
        {
            Week? openWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.Open);
            if (openWeek is null)
            {
                return (
                    value: new BaseResult { Success = false, Message = "No open week found for the season." },
                    openWeek: null);
            }
            if (openWeek.Status != WeekStatus.Open)
            {
                return (
                    value: new BaseResult { Success = false, Message = "Deck submissions are not open for the current week." },
                    openWeek: null);
            }

            return (
                value: new BaseResult { Success = true, Message = "Valid single open week."},
                openWeek: openWeek);
        }
    }
}
