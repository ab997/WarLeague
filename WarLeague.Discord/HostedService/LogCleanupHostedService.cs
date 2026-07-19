using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WarLeague.Data.Services;

namespace WarLeague.Discord.HostedService;

public class LogCleanupHostedService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LogCleanupHostedService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _logRetentionPeriod;
    private readonly LogsCleanupService _logsCleanupService;

    public LogCleanupHostedService(IConfiguration configuration, ILogger<LogCleanupHostedService> logger, LogsCleanupService logsCleanupService)
    {
        _configuration = configuration;
        _logger = logger;

        // Read from configuration or use defaults
        var retentionDays = _configuration.GetValue<int>("LogCleanup:RetentionDays", 180);
        var cleanupIntervalHours = _configuration.GetValue<int>("LogCleanup:CleanupIntervalHours", 24);

        _logRetentionPeriod = TimeSpan.FromDays(retentionDays);
        _cleanupInterval = TimeSpan.FromHours(cleanupIntervalHours);
        _logsCleanupService = logsCleanupService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Log Cleanup Service started. Will run every {Interval} to delete logs older than {RetentionDays} days.",
            _cleanupInterval, _logRetentionPeriod.Days);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _logsCleanupService.CleanupOldLogsAsync(_logRetentionPeriod, _configuration.GetConnectionString("DefaultConnection"), stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during log cleanup");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Wait before retrying
            }
        }
    }

    
}
