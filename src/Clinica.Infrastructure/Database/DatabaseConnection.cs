using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Clinica.Infrastructure.Database;

// -----------------------------------------------------------------------------
// Conexion ADO.NET pura a SQL Server.
//
// Esta clase resuelve la cadena de conexion desde multiples fuentes para evitar
// problemas tipicos en desarrollo y despliegue:
// - appsettings.json / appsettings.Development.json
// - ConnectionStrings:DefaultConnection
// - ConnectionStrings__DefaultConnection
// - variables de entorno reales del sistema
// -----------------------------------------------------------------------------
public sealed class DatabaseConnection
{
    private readonly string _connectionString;

    public DatabaseConnection(IConfiguration configuration)
    {
        if (!TryResolveConnectionString(configuration, out var connectionString))
        {
            throw new InvalidOperationException(
                "La ConnectionString 'DefaultConnection' esta vacia. " +
                "Configura ConnectionStrings:DefaultConnection en appsettings, .env o variables de entorno.");
        }

        _connectionString = connectionString!;
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public static bool TryResolveConnectionString(IConfiguration configuration, out string? connectionString)
    {
        var candidates = new[]
        {
            configuration.GetConnectionString("DefaultConnection"),
            configuration["ConnectionStrings:DefaultConnection"],
            configuration["ConnectionStrings__DefaultConnection"],
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
            Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection"),
            Environment.GetEnvironmentVariable("DefaultConnection")
        };

        connectionString = candidates
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim();

        return !string.IsNullOrWhiteSpace(connectionString);
    }
}