using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services
{
    public class DeckSubmissionService
    {
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly TeamRepository _teamRepository;
        private readonly WeekRepository _weekRepository;
        private readonly DeckSubmissionRepository _deckSubmissionRepository;
        public DeckSubmissionService(PlayerSeasonTeamRepository playerSeasonTeamRepository, TeamRepository teamRepository, WeekRepository weekRepository, DeckSubmissionRepository deckSubmissionRepository)
        {
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _teamRepository = teamRepository;
            _weekRepository = weekRepository;
            _deckSubmissionRepository = deckSubmissionRepository;
        }
        public async Task<Result> SubmitAsync(int seasonId, int playerId, string deckContent)
        {
            Week? openWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.Open);
            if (openWeek is null)
            {
                return new Result { Success = false, Message = "No open week found for the season." };
            }
            if (openWeek.Status != WeekStatus.Open)
            {
                return new Result { Success = false, Message = "Deck submissions are not open for the current week." };
            }

            var now = DateTime.UtcNow;
            if (openWeek.SubmissionsClosedDate.HasValue && openWeek.SubmissionsClosedDate.Value <= now)
            {
                return new Result { Success = false, Message = "Deck submissions are closed for the current week." };
            }

            var pst = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(playerId, seasonId);
            if (pst is null)
            {
                return new Result { Success = false, Message = "Player is not on any team for the active season." };
            }

            // Upsert submission for (player, week).
            var existing = await _deckSubmissionRepository.GetByPlayerAndWeekAsync(playerId, openWeek.Id);
            if (existing != null)
            {
                existing.DeckFile = deckContent;
                existing.SubmittedDate = DateTime.UtcNow;
                existing.IsValidated = false;
                await _deckSubmissionRepository.UpdateAsync(existing);
            }
            else
            {
                await _deckSubmissionRepository.AddAsync(new DeckSubmission
                {
                    PlayerId = playerId,
                    WeekId = openWeek.Id,
                    DeckFile = deckContent,
                    SubmittedDate = DateTime.UtcNow,
                    IsValidated = false
                });
            }

            return new Result { Success = true, Message = $"Deck submitted for {pst.Player.UserName} for week {openWeek.WeekNumber} (season {pst.Season.SeasonNumber})." };
        }
    }
}
