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

        public async Task<Season?> GetByIdAsync(int id)
        {
            return await _context.Seasons
                .Include(s => s.Format)
                .Include(s => s.Weeks)
                .SingleOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Season?> GetByNumberAndFormatAsync(int seasonNumber, string formatName)
        {
            return await _context.Seasons
                .Include(s => s.Format)
                .Include(s => s.Weeks)
                .SingleOrDefaultAsync(s => s.SeasonNumber == seasonNumber && s.Format.Name == formatName);
        }

        public async Task<List<Season>> GetAllAsync()
        {
            return await _context.Seasons
                .Include(s => s.Format)
                .Include(s => s.Weeks)
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
    }
}
