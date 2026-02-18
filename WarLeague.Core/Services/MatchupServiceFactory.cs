using Microsoft.Extensions.DependencyInjection;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Services
{
    public class MatchupServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MatchupServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMatchupService GetMatchupService(Season season)
        {
            return season.Phase switch
            {
                SeasonPhase.RoundRobin => _serviceProvider.GetRequiredService<RoundRobinService>(),
                SeasonPhase.Playoffs => _serviceProvider.GetRequiredService<PlayoffService>(),
                _ => throw new ArgumentException($"Unknown season phase: {season.Phase}")
            };
        }
    }
}
