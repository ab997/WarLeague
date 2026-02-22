using Microsoft.EntityFrameworkCore;
using WarLeague.Data;
using WarLeague.Data.Data;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Repositories;

public class FormatRepository
{
    private readonly WarLeagueDbContext _context;
    private readonly GuildContextService _guildContextService;

    public FormatRepository(WarLeagueDbContext context, GuildContextService guildContextService)
    {
        _context = context;
        _guildContextService = guildContextService;
    }

    public async Task<Format> GetByIdAsync(int id)
    {
        return await _context.Formats.Include(x => x.Seasons).SingleAsync(f => f.Id == id && f.GuildId == _guildContextService.GuildId);
    }

    public async Task<Format?> GetByNameAsync(string name)
    {
        return await _context.Formats.Include(x => x.Seasons).SingleOrDefaultAsync(f => f.Name == name && f.GuildId == _guildContextService.GuildId);
    }

    public async Task<List<Format>> GetAllAsync()
    {
        return await _context.Formats
            .Where(f => f.GuildId == _guildContextService.GuildId)
            .ToListAsync();
    }

    /// <summary>
    /// Format names for the guild, optionally filtered by name prefix (case-insensitive), for autocomplete.
    /// </summary>
    public async Task<List<Format>> GetByNamePrefixAsync(string? prefix, int limit)
    {
        var query = _context.Formats.Where(f => f.GuildId == _guildContextService.GuildId);
        if (!string.IsNullOrWhiteSpace(prefix))
            query = query.Where(f => f.Name.StartsWith(prefix));
        return await query.OrderBy(f => f.Name).Take(limit).ToListAsync();
    }

    public async Task<Format> AddAsync(Format format)
    {
        format.GuildId = _guildContextService.GuildId;
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

    public async Task<(bool isSingleFormatMode, Format? format)> GetSingleFormatModeFormatAsync()
    {
        var formats = await GetAllAsync();

        Format? format = formats.SingleOrDefault(x => x.SingleFormatMode && x.GuildId == _guildContextService.GuildId);

        if (format is null) return (false, null);

        return (true, format);
    }
}
