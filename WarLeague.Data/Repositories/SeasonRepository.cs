using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Repositories
{
    public class SeasonRepository
    {
        private readonly WarLeagueDbContext _context;

        public SeasonRepository(WarLeagueDbContext context)
        {
            _context = context;
        }

        public async Task<List<Season>> GetAllByFormatAsync(int formatId)
        {
            return await _context.Seasons
                .Where(s => s.Format.Id == formatId)
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

        public async Task<Season?> GetBySeasonNumberAndFormatAsync(int seasonNumber, int formatId)
        {
            return await _context.Seasons
                .Where(s => s.Format.Id == formatId)
                .SingleOrDefaultAsync(s => s.SeasonNumber == seasonNumber);
        }

        public async Task UpdateRangeAsync(List<Season> seasons)
        {
            _context.Seasons.UpdateRange(seasons);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Season>> GetActiveSeasonsByFormatAsync(int formatId)
        {
            return await _context.Seasons
                .Where(s => s.Format.Id == formatId && s.Active)
                .ToListAsync();
        }

        public async Task<Season> GetSingleActiveSeasonByFormatNameAsync(string formatName)
        {
            return await _context.Seasons
               .Where(s => s.Format.Name == formatName)
              .SingleAsync(s => s.Active);
        }

        public async Task<Season?> GetByIdOrDefault(int seasonId)
        {
            return await _context.Seasons
                .SingleOrDefaultAsync(s => s.Id == seasonId);
        }
    }
}
