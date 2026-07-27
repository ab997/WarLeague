using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WarLeague.Discord.Helpers;

public class DiscordBotService : IHostedService
{
    private readonly DiscordSocketClient _discord;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordSocketClient> _logger;
    //private readonly Random _random = new();
    //private const double ReactionChance = 0.05;
    //private static readonly Emoji PoopEmoji = new("💩");

    public DiscordBotService(DiscordSocketClient discord, IConfiguration config, ILogger<DiscordSocketClient> logger)
    {
        _discord = discord;
        _config = config;
        _logger = logger;

        _discord.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        //_discord.MessageReceived += OnMessageReceivedAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _discord.LoginAsync(TokenType.Bot, _config["Discord:Token"]);
        await _discord.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        //_discord.MessageReceived -= OnMessageReceivedAsync;
        await _discord.LogoutAsync();
        await _discord.StopAsync();
    }

    //private async Task OnMessageReceivedAsync(SocketMessage message)
    //{
    //    if (message.Author.IsBot)
    //        return;

    //    var targetUserId = _config.GetValue<ulong>("PoopReaction:TargetUserId");
    //    if (targetUserId == 0 || message.Author.Id != targetUserId)
    //        return;

    //    if (_random.NextDouble() > ReactionChance)
    //        return;

    //    try
    //    {
    //        await message.AddReactionAsync(PoopEmoji);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogWarning(ex, "Failed to add poop reaction");
    //    }
    //}
}