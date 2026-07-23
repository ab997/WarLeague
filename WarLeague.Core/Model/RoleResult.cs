using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Core.Model
{
    public class RoleResult : BaseResult
    {
        public RoleResult()
        {
            
        }
        public ulong DiscordRoleId { get; set; }
    }
}
