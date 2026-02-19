using Microsoft.Extensions.DependencyInjection;
using WarLeague.Data;
using WarLeague.Core.Repositories;
using WarLeague.Core.Services;
using WarLeague.Data.Data;

namespace WarLeague.Test;

/// <summary>
/// Provides a configured DI container for tests with domain services only.
/// Repositories are internal dependencies of services.
/// </summary>
public static class TestServiceProvider
{
    public static IServiceProvider CreateServiceProvider(WarLeagueDbContext context)
    {
        var services = new ServiceCollection();

        // Register DbContext
        services.AddSingleton(context);

        // Register Repositories (internal dependencies, not exposed to tests)
        services.AddScoped<FormatRepository>();
        services.AddScoped<SeasonRepository>();
        services.AddScoped<WeekRepository>();
        services.AddScoped<TeamRepository>();
        services.AddScoped<ConferenceRepository>();
        services.AddScoped<PlayerSeasonTeamRepository>();
        services.AddScoped<MatchRepository>();
        services.AddScoped<RoundRobinMatchupRepository>();
        services.AddScoped<PlayoffMatchupRepository>();
        services.AddScoped<PlayerRepository>();
        services.AddScoped<DeckSubmissionRepository>();

        // Register Services (these are what tests should use)
        services.AddScoped<FormatService>();
        services.AddScoped<SeasonService>();
        services.AddScoped<WeekService>();
        services.AddScoped<TeamService>();
        services.AddScoped<TeamValidationService>();
        services.AddScoped<RoundRobinService>();
        services.AddScoped<PlayoffService>();
        services.AddScoped<MatchupServiceFactory>();
        services.AddScoped<MatchService>();
        services.AddScoped<DeckSubmissionService>();
        services.AddScoped<SubstitutionService>();
        services.AddScoped<ConferenceService>();

        services.AddScoped<GuildContextService>();

        return services.BuildServiceProvider();
    }
}
