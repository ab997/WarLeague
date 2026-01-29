using Microsoft.EntityFrameworkCore;
using WarLeague.Core.Data;
using WarLeague.Core.Data.Entities;

namespace WarLeague.Core.Repositories;

public class FormatRepository
{
    private readonly WarLeagueDbContext _context;

    public FormatRepository(WarLeagueDbContext context)
    {
        _context = context;
    }

    public async Task<Format?> GetByIdOrDefaultAsync(int id)
    {
        return await _context.Formats.Include(x => x.Seasons).SingleOrDefaultAsync(f => f.Id == id);
    }
    public async Task<Format> GetByIdAsync(int id)
    {
        return await _context.Formats.Include(x => x.Seasons).SingleAsync(f => f.Id == id);
    }

    public async Task<Format?> GetByNameAsync(string name)
    {
        return await _context.Formats.Include(x => x.Seasons).SingleOrDefaultAsync(f => f.Name == name);
    }

    public async Task<List<Format>> GetAllAsync()
    {
        return await _context.Formats.ToListAsync();
    }

    public async Task<Format> AddAsync(Format format)
    {
        await _context.Formats.AddAsync(format);
        await _context.SaveChangesAsync();
        return format;
    }

    public async Task<Format> UpdateAsync(Format format)
    {
        _context.Formats.Update(format);
        await _context.SaveChangesAsync();
        return format;
    }

    public async Task DeleteAsync(Format format)
    {
        _context.Formats.Remove(format);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(List<Format> formats)
    {
        _context.Formats.UpdateRange(formats);
        await _context.SaveChangesAsync();
    }
}
