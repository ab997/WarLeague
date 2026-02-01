using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Repositories;

namespace WarLeague.Discord.Services
{
    public class DiscordPlayerService
    {
        private readonly PlayerRepository _playerRepository;
        public DiscordPlayerService(PlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }
        public async Task<Player> EnsurePlayerExistsAsync(IUser user)
        {
            Player? player = _playerRepository.GetByDiscordUserId(user.Id);

            if (player is null)
            {
                player = new Player
                {
                    DiscordUserId = user.Id,
                    UserName = user.Username
                };
                await _playerRepository.AddAsync(player);
            }

            return player;
        }
    }
}
