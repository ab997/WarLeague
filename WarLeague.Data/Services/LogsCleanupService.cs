using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace WarLeague.Data.Services
{
    public class LogsCleanupService
    {
        private readonly ILogger<LogsCleanupService> _logger;

        public LogsCleanupService(ILogger<LogsCleanupService> logger)
        {
            _logger = logger;
        }

        public async Task CleanupOldLogsAsync(TimeSpan logRetentionPeriod, string? connectionString, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Connection string not configured. Skipping log cleanup.");
                return;
            }

            var cutoffDate = DateTime.UtcNow.Subtract(logRetentionPeriod);

            await using var connection =
                new NpgsqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = """
            DELETE FROM logs
            WHERE raise_date < @cutoff_date;
            """;

            command.Parameters.AddWithValue(
                "cutoff_date",
                NpgsqlDbType.TimestampTz,
                cutoffDate);

            var rowsDeleted =
                await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsDeleted > 0)
            {
                _logger.LogInformation("Deleted {RowsDeleted} log entries older than {CutoffDate:yyyy-MM-dd}",
                    rowsDeleted, cutoffDate);
            }
        }
    }
}
