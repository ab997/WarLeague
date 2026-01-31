using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services
{
    public class TeamService
    {
        private readonly WarLeagueDbContext _context;
        private readonly TeamRepository _teamRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly SeasonRepository _seasonRepository;
        private readonly PlayerRepository _playerRepository;
        public TeamService(WarLeagueDbContext context, TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository, SeasonRepository seasonRepository, PlayerRepository playerRepository)
        {
            _context = context;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _seasonRepository = seasonRepository;
            _playerRepository = playerRepository;
        }

        public async Task<Result> CreateAsync(int seasonId, string teamName, int captainId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            Season? season = await _seasonRepository.GetByIdOrDefault(seasonId);

            if (season is null)
            {
                return new Result { Success = false, Message = $"Season with ID '{seasonId}' does not exist." };
            }

            Team? check = await _teamRepository.GetByNameAsync(teamName);

            if (check is not null)
            {
                return new Result { Success = false, Message = $"A team with the name '{teamName}' already exists." };
            }

            Player player = await _playerRepository.GetByIdAsync(captainId);

            bool success = await _playerSeasonTeamRepository.EnsurePlayerIsNotMemberOfTeamInSeasonAsync(player.Id, season.Id);

            if (!success)
            {
                return new Result { Success = false, Message = $"Player {player.UserName} already a member of another team." };
            }

            Team team = new Team
            {
                Name = teamName,
                Captain = player,
                CreatedDate = DateTime.UtcNow,
                Season = season,
            };

            await _teamRepository.AddAsync(team);

            PlayerSeasonTeam pst = new PlayerSeasonTeam
            {
                Player = player,
                Season = season,
                Team = team
            };

            await _playerSeasonTeamRepository.AddAsync(pst);

            await transaction.CommitAsync();

            return new Result { Success = true, Message = "Team created successfully with you as captain." };
        }

        public async Task<Team?> DeleteAsync(int seasonId, string teamName)
        {
            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team == null)
            {
                return null;
            }

            await _teamRepository.DeleteAsync(team);

            return team;
        }
        /// <summary>
        /// add a member to captain's team
        /// </summary>
        /// <param name="seasonId"></param>
        /// <param name="teamName"></param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public async Task<Result> CaptainAddMemberAsync(int seasonId, int captainId, int playerId)
        {
            Team? team = await _teamRepository.GetByCaptainAndSeasonAsync(captainId, seasonId);

            if (team is null)
            {
                return new Result { Success = false, Message = $"Player with id {captainId} is not captain of any team in this season." };
            }

            return await AddMemberAsync(seasonId, playerId, team.Id);
        }

        public async Task<Result> AddMemberAsync(int seasonId, int playerId, string teamName)
        {
            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team is null)
            {
                return new Result { Success = false, Message = $"Team with name '{teamName}' does not exist in this season." };
            }
            return await AddMemberAsync(seasonId, playerId, team.Id);
        }

        public async Task<Result> AddMemberAsync(int seasonId, int playerId, int teamId)
        {
            bool notMember = await _playerSeasonTeamRepository.EnsurePlayerIsNotMemberOfTeamInSeasonAsync(playerId, seasonId);

            if (!notMember)
            {
                return new Result { Success = false, Message = $"Player with id {playerId} is already a member of another team in this season." };
            }

            PlayerSeasonTeam pst = new PlayerSeasonTeam
            {
                PlayerId = playerId,
                SeasonId = seasonId,
                TeamId = teamId
            };

            await _playerSeasonTeamRepository.AddAsync(pst);

            return new Result { Success = true, Message = $"Added player to team." };
        }

        public async Task<Result> CaptainRemoveMemberAsync(int seasonId, int captainId, int playerId)
        {
            Team? team = await _teamRepository.GetByCaptainAndSeasonAsync(captainId, seasonId);

            if (team is null)
            {
                return new Result { Success = false, Message = $"Player with id {captainId} is not captain of any team in this season." };
            }

            return await RemoveMemberFromTeamAsync(seasonId, playerId, team);
        }

        public async Task<Result> RemoveMemberFromTeamAsync(int seasonId, int playerId, Team team)
        {
            // Prevent dropping the captain
            if (team.CaptainId == playerId)
            {
                return new Result { Success = false, Message = "The team captain cannot be removed. Transfer captainship first if needed." };
            }

            // Ensure the user is a member of this team in the current season
            PlayerSeasonTeam? pst =
                await _playerSeasonTeamRepository.GetByPlayerSeasonAndTeamAsync(
                    playerId,
                    seasonId,
                    team.Id);

            if (pst == null)
            {
                return new Result { Success = false, Message = $"Player with id {playerId} is not a member of team '{team.Name}'." };
            }

            await _playerSeasonTeamRepository.DeleteAsync(pst);

            return new Result { Success = true, Message = $"Removed player from team '{team.Name}'." };
        }

        public async Task<Result> RemoveMemberAsync(int seasonId, int playerId)
        {
            // Ensure the user is a member of this team in the current season
            PlayerSeasonTeam? pst =
                await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(
                    playerId,
                    seasonId);

            if (pst == null)
            {
                return new Result { Success = false, Message = $"Player with id {playerId} is not a member of any team." };
            }

            Team team = pst.Team;

            // Prevent dropping the captain
            if (team.CaptainId == playerId)
            {
                return new Result { Success = false, Message = "The team captain cannot be removed. Transfer captainship first if needed." };
            }

            await _playerSeasonTeamRepository.DeleteAsync(pst);

            return new Result { Success = true, Message = $"Removed player from team '{team.Name}'." };
        }
        public async Task<Result> TransferMemberAsync(int seasonId, int playerId, string teamName)
        {
            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team == null)
            {
                return new Result { Success = false, Message = $"Team with name '{teamName}' does not exist in this season." };
            }
            return await TransferMemberAsync(seasonId, playerId, team.Id);
        }
        public async Task<Result> TransferMemberAsync(int seasonId, int playerId, int teamId)
        {
            // Captains cannot be transferred
            bool canTransfer = await _playerSeasonTeamRepository.EnsurePlayerIsNotCaptainOfTeamInSeasonAsync(playerId, seasonId);
            if (!canTransfer)
            {
                return new Result { Success = false, Message = "Captains cannot be transferred." };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            Result result1 = await RemoveMemberAsync(seasonId, playerId);

            if (!result1.Success)
            {
                return result1;
            }

            Result result2 = await AddMemberAsync(seasonId, playerId, teamId);

            if (!result2.Success)
            {
                return result2;
            }

            await transaction.CommitAsync();

            return new Result { Success = true, Message = "Player transferred successfully." };
        }

        public async Task<Result> TransferCaptainshipAsync(int seasonId, int newCaptainId, string teamName)
        {
            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team == null)
            {
                return new Result { Success = false, Message = $"Team with name '{teamName}' does not exist in this season." };
            }

            // Ensure the user is already a member of the team
            PlayerSeasonTeam? pst =
                await _playerSeasonTeamRepository.GetByPlayerSeasonAndTeamAsync(
                    newCaptainId,
                    seasonId,
                    team.Id);

            if (pst == null)
            {
                return new Result { Success = false, Message = $"Player with id {newCaptainId} is not a member of team '{team.Name}'. Captainship transfer is supported only among existing members." };
            }

            team.CaptainId = newCaptainId;

            await _teamRepository.UpdateAsync(team);

            return new Result { Success = true, Message = "Captainship transferred successfully." };
        }
    }
}
