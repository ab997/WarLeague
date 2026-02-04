using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Domain.Model;

namespace WarLeague.Discord.Model
{
    public class SocketRoleResult : BaseResult
    {
        public SocketRole? Role { get; set; }
    }
}
