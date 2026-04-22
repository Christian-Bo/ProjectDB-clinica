using Clinica.Application.Contracts;
using Clinica.Application.Models;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

public sealed class DatabaseHealthService : IDatabaseHealthService
{
    private readonly DatabaseConnection _databaseConnection;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<DatabaseHealthService> _logger;

    public DatabaseHealthService(DatabaseConnection databaseConnection, IHostEnvironment hostEnvironment, ILogger<DatabaseHealthService> logger)
    {
        _databaseConnection = databaseConnection;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using SqlConnection connection = _databaseConnection.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    DB_NAME() AS DatabaseName,
                    CONVERT(datetime2, SYSUTCDATETIME()) AS ServerUtcNow,
                    CAST(SERVERPROPERTY('ServerName') AS nvarchar(256)) AS DataSource;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return new DatabaseHealthResult
                {
                    IsSuccess = false,
                    ErrorMessage = "La consulta de salud no devolvio filas."
                };
            }

            return new DatabaseHealthResult
            {
                IsSuccess = true,
                DatabaseName = reader["DatabaseName"]?.ToString(),
                ServerUtcNow = reader["ServerUtcNow"] as DateTime?,
                DataSource = reader["DataSource"]?.ToString(),
                EnvironmentName = _hostEnvironment.EnvironmentName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return new DatabaseHealthResult
            {
                IsSuccess = false,
                ErrorMessage = "Database connectivity error",
                EnvironmentName = _hostEnvironment.EnvironmentName
            };
        }
    }
}