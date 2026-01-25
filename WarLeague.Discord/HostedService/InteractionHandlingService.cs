using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.HostedService
{
    public class InteractionHandlingService : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _config;
        private readonly ILogger<InteractionService> _logger;

        public InteractionHandlingService(
            DiscordSocketClient discord,
            InteractionService interactions,
            IServiceProvider services,
            IConfiguration config,
            ILogger<InteractionService> logger)
        {
            _discord = discord;
            _interactions = interactions;
            _services = services;
            _config = config;
            _logger = logger;

            _interactions.Log += msg => LogHelper.OnLogAsync(_logger, msg);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.Ready += () => _interactions.RegisterCommandsGloballyAsync(true);
            _discord.InteractionCreated += OnInteractionAsync;
            _interactions.InteractionExecuted += HandleInteractionExecuted;

            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _interactions.Dispose();
            return Task.CompletedTask;
        }

        private async Task OnInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_discord, interaction);
                var result = await _interactions.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
            catch
            {
                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    await interaction.GetOriginalResponseAsync()
                        .ContinueWith(msg => msg.Result.DeleteAsync());
                }
            }
        }

        private Task HandleInteractionExecuted(ICommandInfo command, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
                _ = Task.Run(() => HandleInteractionExecutionResult(context.Interaction, result));
            return Task.CompletedTask;
        }

        private async Task HandleInteractionExecutionResult(IDiscordInteraction interaction, IResult result)
        {
            string message = "An error has occurred. We are already investigating it!";
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    message = result.ErrorReason;
                    _logger.LogInformation($"Unmet precondition - {result.Error}");
                    break;

                case InteractionCommandError.BadArgs:
                    _logger.LogInformation($"Bad args - {result.Error}");
                    break;

                case InteractionCommandError.ConvertFailed:
                    _logger.LogInformation($"Convert Failed - {result.Error}");
                    break;

                case InteractionCommandError.Exception:
                    _logger.LogInformation($"Exception - {result.Error}");
                    break;

                case InteractionCommandError.ParseFailed:
                    _logger.LogInformation($"Parse Failed - {result.Error}");
                    break;

                case InteractionCommandError.UnknownCommand:
                    _logger.LogInformation($"Unknown Command - {result.Error}");
                    break;

                case InteractionCommandError.Unsuccessful:
                    _logger.LogInformation($"Unsuccessful - {result.Error}");
                    break;
            }

            if (!interaction.HasResponded)
            {
                await interaction.RespondAsync(message, ephemeral: true);
            }
            else
            {
                await interaction.FollowupAsync(message, ephemeral: true);
            }
        }
    }
}
