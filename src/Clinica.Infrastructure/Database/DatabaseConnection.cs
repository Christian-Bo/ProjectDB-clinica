using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Clinica.Infrastructure.Database;

// Conexion ADO.NET pura a SQL Server — sin ORM, sin migraciones.
public sealed class DatabaseConnection
{
    private readonly string _connectionString;

    public DatabaseConnection(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' no configurada.");

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("La ConnectionString 'DefaultConnection' esta vacia.");
        }
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
