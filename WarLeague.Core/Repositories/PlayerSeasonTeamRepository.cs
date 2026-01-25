using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;

namespace WarLeague.Core.Repositories
{
    public class PlayerSeasonTeamRepository
    {
        private readonly WarLeagueDbContext _context;
        public PlayerSeasonTeamRepository(WarLeagueDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PlayerSeasonTeam playerSeasonTeam)
        {
            _context.PlayerSeasonTeams.Add(playerSeasonTeam);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EnsurePlayerIsNotMemberOfTeamInSeasonAsync(int playerId, int seasonId)
        {
            PlayerSeasonTeam? pst = await _context.PlayerSeasonTeams.SingleOrDefaultAsync(x => x.PlayerId == playerId && x.SeasonId == seasonId);

            if (pst is null) return true;

            return false;
        }

        public async Task<bool> EnsurePlayerIsNotCaptainOfTeamInSeasonAsync(int playerId, int seasonId)
        {
            PlayerSeasonTeam? pst = await _context.PlayerSeasonTeams.SingleOrDefaultAsync(x => x.PlayerId == playerId && x.SeasonId == seasonId);

            if (pst is null) return true;

            int teamId = pst.TeamId;

            Team team = await _context.Teams.SingleAsync(x => x.Id == teamId);

            return team.CaptainId != playerId;
        }

        public async Task<PlayerSeasonTeam?> GetByPlayerAndSeasonAsync(int playerId, int seasonId)
        {
            return await _context.PlayerSeasonTeams.SingleOrDefaultAsync(x => x.PlayerId == playerId && x.SeasonId == seasonId);
        }

        public async Task DeleteAsync(PlayerSeasonTeam existingPst)
        {
            _context.PlayerSeasonTeams.Remove(existingPst);
            await _context.SaveChangesAsync();
        }

        public async Task<PlayerSeasonTeam?> GetByPlayerSeasonAndTeamAsync(int playerId, int seasonId, int teamId)
        {
            return await _context.PlayerSeasonTeams.SingleOrDefaultAsync(x => x.PlayerId == playerId && x.SeasonId == seasonId && x.TeamId == teamId);
        }
    }
}
