using Microsoft.Extensions.Configuration;
using System.Data;
// Descomenta el driver que uses:
using MySqlConnector;
// using Microsoft.Data.SqlClient;

namespace Clinica.Infrastructure.Database;

// Conexion ADO.NET pura — sin ORM, sin migraciones
public sealed class DatabaseConnection
{
    private readonly string _connectionString;

    public DatabaseConnection(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' no configurada.");
    }

    public IDbConnection CreateConnection()
    {
        // MySQL:
        return new MySqlConnection(_connectionString);
        // SQL Server (comenta la linea de arriba y descomenta esta):
        // return new SqlConnection(_connectionString);
    }
}
