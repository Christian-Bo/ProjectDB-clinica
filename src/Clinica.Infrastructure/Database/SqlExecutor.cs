using Microsoft.Data.SqlClient;
using System.Data;

namespace Clinica.Infrastructure.Database;

public sealed class SqlExecutor
{
    private readonly DatabaseConnection _db;

    public SqlExecutor(DatabaseConnection db)
    {
        _db = db;
    }

    public async Task<List<T>> QueryAsync<T>(
        string storedProcedure,
        SqlParameter[] parameters,
        Func<SqlDataReader, T> map)
    {
        var results = new List<T>();

        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(storedProcedure, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddRange(parameters);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }

        return results;
    }

    public async Task<T?> QuerySingleAsync<T>(
        string storedProcedure,
        SqlParameter[] parameters,
        Func<SqlDataReader, T> map) where T : class
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(storedProcedure, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddRange(parameters);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return map(reader);
        }

        return null;
    }

    public async Task<SpResult> ExecuteAsync(
        string storedProcedure,
        SqlParameter[] parameters)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(storedProcedure, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddRange(parameters);

        await using var reader = await command.ExecuteReaderAsync();

        do
        {
            while (await reader.ReadAsync())
            {
                if (reader.HasColumn("HttpStatus"))
                {

                    return new SpResult
                    {
                        HttpStatus = reader.GetInt32OrDefault("HttpStatus", 200),
                        Codigo     = reader.GetNullableString("Codigo") ?? string.Empty,
                        Mensaje    = reader.GetNullableString("Mensaje") ?? string.Empty,
                        EntityId   = reader.GetNullableInt64("CitaId")
                    };
                }
            }
        }
        while (await reader.NextResultAsync());

        return new SpResult { HttpStatus = 200, Codigo = "OK", Mensaje = "Operacion completada" };
    }
}

// Fuera de SqlExecutor
public sealed class SpResult
{
    public int HttpStatus { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
    public long? EntityId { get; init; }
    public bool Success => HttpStatus >= 200 && HttpStatus < 300;
}