using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WarLeague.Data;
using WarLeague.Core.Repositories;
using WarLeague.Core.Services;
using WarLeague.Discord.HostedService;
using WarLeague.Discord.Services;
using Serilog;
using WarLeague.Data.Repositories;
using WarLeague.Data.Data;

var builder = Host.CreateApplicationBuilder(args);

// Configuration.
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    ;

// serilog
builder.Services.AddSerilog((services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<WarLeagueDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<TeamRepository>();
builder.Services.AddScoped<PlayerRepository>();
builder.Services.AddScoped<MatchRepository>();
builder.Services.AddScoped<RoundRobinMatchupRepository>();
builder.Services.AddScoped<WeekRepository>();
builder.Services.AddScoped<DeckSubmissionRepository>();
builder.Services.AddScoped<FormatRepository>();
builder.Services.AddScoped<SeasonRepository>();
builder.Services.AddScoped<PlayerRepository>();
builder.Services.AddScoped<PlayerSeasonTeamRepository>();
builder.Services.AddScoped<PermissionRepository>();

// multi server support
builder.Services.AddScoped<GuildContextService>();

// Services (core - domain)
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<FormatService>();
builder.Services.AddScoped<SeasonService>();
builder.Services.AddScoped<WeekService>();
builder.Services.AddScoped<DeckSubmissionService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<SubstitutionService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<TeamValidationService>();
builder.Services.AddScoped<IMatchupService, RoundRobinService>();


// Services (discord)
builder.Services.AddScoped<DiscordApiHelperService>();
builder.Services.AddScoped<DiscordPlayerService>();
builder.Services.AddScoped<DiscordRoleService>();
// Discord client
var discordClient = new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent,
    AlwaysDownloadUsers = true
});

// we disable auto service scope create so that we can create our own scope and set GuildId in GuildContextService before executing command
var interactionService = new InteractionService(discordClient, new InteractionServiceConfig { AutoServiceScopes = false });
builder.Services.AddSingleton(discordClient);
builder.Services.AddSingleton(interactionService);

builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddHostedService<InteractionHandlingService>();
builder.Services.AddHostedService<LogCleanupService>();

builder.Services.AddHttpClient();

var app = builder.Build();

await app.RunAsync();
