using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data.Data.Enums;

namespace WarLeague.Data.Data.Entities
{
    public class RolePermissionMapping
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
        public PermissionType PermissionType { get; set; }
    }
}
