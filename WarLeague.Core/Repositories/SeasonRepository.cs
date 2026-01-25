using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;

namespace WarLeague.Core.Repositories
{
    public class SeasonRepository
    {
        private readonly WarLeagueDbContext _context;

        public SeasonRepository(WarLeagueDbContext context)
        {
            _context = context;
        }

        public async Task<List<Season>> GetAllAsync()
        {
            return await _context.Seasons
                .ToListAsync();
        }

        public async Task<Season> AddAsync(Season season)
        {
            await _context.Seasons.AddAsync(season);
            await _context.SaveChangesAsync();
            return season;
        }

        public async Task<Season> UpdateAsync(Season season)
        {
            _context.Seasons.Update(season);
            await _context.SaveChangesAsync();
            return season;
        }

        public async Task DeleteAsync(Season season)
        {
            _context.Seasons.Remove(season);
            await _context.SaveChangesAsync();
        }

        public async Task<Season?> GetBySeasonNumberAsync(int seasonNumber)
        {
            return await _context.Seasons.SingleOrDefaultAsync(s => s.SeasonNumber == seasonNumber);
        }

        public async Task UpdateRangeAsync(List<Season> seasons)
        {
            _context.Seasons.UpdateRange(seasons);
            await _context.SaveChangesAsync();
        }
    }
}
