using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;

namespace WarLeague.Discord.Services
{
    public class HelperService
    {
        private readonly FormatRepository _formatRepository;
        public HelperService(FormatRepository formatRepository)
        {
            _formatRepository = formatRepository;
        }
        public async Task<Format> GetFormatByCategoryNameAsync(SocketInteractionContext context)
        {
            SocketTextChannel channel = (SocketTextChannel)context.Channel;

            string categoryName = channel.Category.Name;

            return (await _formatRepository.GetByNameAsync(categoryName))!;
        }
    }
}
