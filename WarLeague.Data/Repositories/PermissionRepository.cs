using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;

namespace WarLeague.Data.Repositories
{
    public class PermissionRepository
    {
        private readonly WarLeagueDbContext _dbContext;
        public PermissionRepository(WarLeagueDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<RolePermissionMapping> BindAsync(ulong guildId, PermissionType permission, ulong roleId)
        {
            // check for existing mapping
            var existingMapping = await _dbContext.RolePermissionMappings
                .FirstOrDefaultAsync(x => x.GuildId == guildId && x.PermissionType == permission);

            if (existingMapping is not null)
            {
                existingMapping.RoleId = roleId;
                await _dbContext.SaveChangesAsync();
                return existingMapping;
            }

            var mapping = new RolePermissionMapping
            {
                GuildId = guildId,
                PermissionType = permission,
                RoleId = roleId
            };

            _dbContext.RolePermissionMappings.Add(mapping);
            await _dbContext.SaveChangesAsync();

            return mapping;
        }

        public async Task<IReadOnlyCollection<ulong>> GetRoleIdsAsync(ulong guildId, PermissionType permission)
        {
            return await _dbContext.RolePermissionMappings
                .Where(x => x.GuildId == guildId && x.PermissionType == permission)
                .AsNoTracking()
                .Select(x => x.RoleId)
                .ToListAsync();
        }
        public IReadOnlyCollection<ulong> GetRoleIds(ulong guildId, PermissionType permission)
        {
            return _dbContext.RolePermissionMappings
                .Where(x => x.GuildId == guildId && x.PermissionType == permission)
                .AsNoTracking()
                .Select(x => x.RoleId)
                .ToList();
        }

        public async Task<bool> UnbindAsync(ulong guildId, PermissionType permission)
        {
            var existingMapping = await _dbContext.RolePermissionMappings
                .FirstOrDefaultAsync(x => x.GuildId == guildId && x.PermissionType == permission);

            if (existingMapping is null)
            {
                return false;
            }

            _dbContext.RolePermissionMappings.Remove(existingMapping);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}
