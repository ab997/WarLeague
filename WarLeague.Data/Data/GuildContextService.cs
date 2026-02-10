using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Data.Data
{
    /// <summary>
    /// sole purpose of this service is to hold GuildId so that we can filter Formats by GuildId instead of passing parameters around.
    /// </summary>
    public class GuildContextService
    {
        private ulong _guildId;
        public ulong GuildId => _guildId;

        public void SetGuildId(ulong guildId)
        {
            _guildId = guildId;
        }
    }
}
