using Microsoft.Extensions.DependencyInjection;
using WarLeague.Core.Data;
using WarLeague.Core.Domain.Services;
using WarLeague.Core.Repositories;

namespace WarLeague.Test;

/// <summary>
/// Provides a configured DI container for tests with all services and repositories.
/// </summary>
public static class TestServiceProvider
{
    public static IServiceProvider CreateServiceProvider(WarLeagueDbContext context)
    {
        var services = new ServiceCollection();

        // Register DbContext
        services.AddSingleton(context);

        // Register Repositories
        services.AddScoped<FormatRepository>();
        services.AddScoped<SeasonRepository>();
        services.AddScoped<WeekRepository>();
        services.AddScoped<TeamRepository>();
        services.AddScoped<PlayerSeasonTeamRepository>();
        services.AddScoped<MatchRepository>();
        services.AddScoped<PlayerRepository>();
        services.AddScoped<DeckSubmissionRepository>();

        // Register Services
        services.AddScoped<FormatService>();
        services.AddScoped<SeasonService>();
        services.AddScoped<WeekService>();
        services.AddScoped<TeamService>();
        services.AddScoped<TeamValidationService>();
        services.AddScoped<MatchService>();
        services.AddScoped<DeckSubmissionService>();

        return services.BuildServiceProvider();
    }
}
