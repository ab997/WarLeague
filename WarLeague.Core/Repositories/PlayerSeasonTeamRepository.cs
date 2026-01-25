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
    }
}
