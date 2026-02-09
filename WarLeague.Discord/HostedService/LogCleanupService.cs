using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WarLeague.Discord.HostedService;

public class LogCleanupService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _logRetentionPeriod;

    public LogCleanupService(IConfiguration configuration, ILogger<LogCleanupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Read from configuration or use defaults
        var retentionDays = _configuration.GetValue<int>("LogCleanup:RetentionDays", 180);
        var cleanupIntervalHours = _configuration.GetValue<int>("LogCleanup:CleanupIntervalHours", 24);
        
        _logRetentionPeriod = TimeSpan.FromDays(retentionDays);
        _cleanupInterval = TimeSpan.FromHours(cleanupIntervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Log Cleanup Service started. Will run every {Interval} to delete logs older than {RetentionDays} days.",
            _cleanupInterval, _logRetentionPeriod.Days);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldLogsAsync(stoppingToken);
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

    private async Task CleanupOldLogsAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Connection string not configured. Skipping log cleanup.");
            return;
        }

        var cutoffDate = DateTime.UtcNow.Subtract(_logRetentionPeriod);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM Logs 
            WHERE TimeStamp < @CutoffDate";
        
        command.Parameters.AddWithValue("@CutoffDate", cutoffDate);

        var rowsDeleted = await command.ExecuteNonQueryAsync(cancellationToken);

        if (rowsDeleted > 0)
        {
            _logger.LogInformation("Deleted {RowsDeleted} log entries older than {CutoffDate:yyyy-MM-dd}", 
                rowsDeleted, cutoffDate);
        }
    }
}
