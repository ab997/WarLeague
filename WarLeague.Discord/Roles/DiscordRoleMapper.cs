using Discord;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarLeague.Core.Data.Enums;

namespace WarLeague.Discord.Roles
{
    public class DiscordRoleMapper
    {
        private readonly DiscordRoleMappings _mappings;

        public DiscordRoleMapper(IOptions<DiscordRoleMappings> options)
        {
            _mappings = options.Value;
        }

        // Map Discord guild user's role IDs to your application Role enum
        public Role MapToApplicationRole(IGuildUser user)
        {
            if (user == null) return Role.Player;

            // Prefer exact role id checks (safer than name matching)
            var roleIds = user.RoleIds;

            if (_mappings.AdminRoleId != 0 && roleIds.Contains(_mappings.AdminRoleId))
                return Role.Admin;

            if (_mappings.CaptainRoleId != 0 && roleIds.Contains(_mappings.CaptainRoleId))
                return Role.TeamCaptain;

            return Role.Player;
        }
    }
}
