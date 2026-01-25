using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WarLeague.Core.Data;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;
using WarLeague.Discord.HostedService;
using WarLeague.Discord.Roles;
using WarLeague.Discord.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configuration.
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    ;

// Discord client
var discordConfig = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent,
    AlwaysDownloadUsers = true
};

var discordClient = new DiscordSocketClient(discordConfig);
var interactionService = new InteractionService(discordClient);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<WarLeagueDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<TeamRepository>();
builder.Services.AddScoped<PlayerRepository>();
builder.Services.AddScoped<MatchRepository>();
builder.Services.AddScoped<WeekRepository>();
builder.Services.AddScoped<DeckSubmissionRepository>();
builder.Services.AddScoped<FormatRepository>();
builder.Services.AddScoped<SeasonRepository>();

// Services
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<WeekService>();
builder.Services.AddScoped<DeckSubmissionService>();
builder.Services.AddScoped<StandingsService>();
builder.Services.AddScoped<HelperService>();

// Discord services
builder.Services.AddSingleton(discordClient);
builder.Services.AddSingleton(interactionService);
builder.Services.AddScoped<PermissionService>();
builder.Services.AddSingleton<MessageService>();
builder.Services.AddSingleton<FileValidationService>();
builder.Services.Configure<DiscordRoleMappings>(builder.Configuration.GetSection("DiscordRoleMappings"));
builder.Services.AddSingleton<DiscordRoleMapper>();

// Hosted services
builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddHostedService<InteractionHandlingService>();
//builder.Services.AddHostedService<DailyStandingsService>();

var app = builder.Build();

await app.RunAsync();
