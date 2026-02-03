using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WarLeague.Discord.Helpers;

namespace WarLeague.Discord.HostedService;

public class DiscordBotService : IHostedService
{
    private readonly DiscordSocketClient _discord;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordSocketClient> _logger;

    public DiscordBotService(DiscordSocketClient discord, IConfiguration config, ILogger<DiscordSocketClient> logger)
    {
        _discord = discord;
        _config = config;
        _logger = logger;

        _discord.Log += msg => LogHelper.OnLogAsync(_logger, msg);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _discord.LoginAsync(TokenType.Bot, _config["Discord:Token"]);
        await _discord.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discord.LogoutAsync();
        await _discord.StopAsync();
    }
}
