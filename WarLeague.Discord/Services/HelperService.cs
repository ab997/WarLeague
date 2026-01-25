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
        private readonly SeasonRepository _seasonRepository;
        public HelperService(FormatRepository formatRepository, SeasonRepository seasonRepository)
        {
            _formatRepository = formatRepository;
            _seasonRepository = seasonRepository;
        }
        public async Task<Format> GetFormatByCategoryNameAsync(SocketInteractionContext context)
        {
            SocketTextChannel channel = (SocketTextChannel)context.Channel;

            string categoryName = channel.Category.Name;

            return (await _formatRepository.GetByNameAsync(categoryName))!;
        }
        /// <summary>
        /// assumes that category name is format name is ensured
        /// assumes that single active season per format is ensured
        /// </summary>
        public async Task<Season> GetSeasonByCategoryNameAsync(SocketInteractionContext context)
        {
            SocketTextChannel channel = (SocketTextChannel)context.Channel;

            string categoryName = channel.Category.Name;

            return (await _seasonRepository.GetSingleActiveSeasonByFormatNameAsync(categoryName))!;
        }
    }
}
