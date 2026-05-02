using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Clinica.Infrastructure.Database;

/// <summary>
/// Abstracción central para ejecutar Stored Procedures en SQL Server.
/// Todo acceso a datos del Módulo 3 pasa por aquí — sin SQL inline.
/// </summary>
public sealed class SqlExecutor
{
    private readonly string _connectionString;

    public SqlExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
    }

    /// <summary>Ejecuta un SP y devuelve múltiples result sets como DataSet.</summary>
    public async Task<DataSet> ExecuteSpAsync(
        string storedProcedure,
        SqlParameter[]? parameters = null,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(storedProcedure, connection)
        {
            CommandType    = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        if (parameters is { Length: > 0 })
            command.Parameters.AddRange(parameters);

        var adapter = new SqlDataAdapter(command);
        var dataSet = new DataSet();

        // SqlDataAdapter.Fill es sincrónico; lo corremos en el pool de hilos.
        await Task.Run(() => adapter.Fill(dataSet), ct);

        return dataSet;
    }

    /// <summary>Ejecuta un SP y devuelve el primer result set como DataTable.</summary>
    public async Task<DataTable> ExecuteSpFirstTableAsync(
        string storedProcedure,
        SqlParameter[]? parameters = null,
        CancellationToken ct = default)
    {
        var ds = await ExecuteSpAsync(storedProcedure, parameters, ct);
        return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
    }

    /// <summary>Ejecuta un SP sin retorno de datos (comandos de escritura sin result set).</summary>
    public async Task ExecuteSpNonQueryAsync(
        string storedProcedure,
        SqlParameter[]? parameters = null,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = CreateCommand(connection, storedProcedure, parameters);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Ejecuta un SP de escritura que devuelve un sobre estándar:
    /// HttpStatus, Codigo, Mensaje y opcionalmente EntityId/CitaId/PacienteId/Id.
    /// </summary>
    public async Task<SpResult> ExecuteAsync(
        string storedProcedure,
        SqlParameter[]? parameters = null,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = CreateCommand(connection, storedProcedure, parameters);
        await using var reader = await command.ExecuteReaderAsync(ct);

        do
        {
            if (!await reader.ReadAsync(ct)) continue;

            // Verificar si este resultset tiene columna HttpStatus
            bool tieneHttpStatus = false;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals("HttpStatus", StringComparison.OrdinalIgnoreCase))
                {
                    tieneHttpStatus = true;
                    break;
                }
            }

            if (tieneHttpStatus)
            {
                return new SpResult
                {
                    HttpStatus = reader.GetInt32OrDefault("HttpStatus", 200),
                    Codigo     = reader.GetNullableString("Codigo") ?? reader.GetNullableString("Code") ?? "OK",
                    Mensaje    = reader.GetNullableString("Mensaje") ?? reader.GetNullableString("Message") ?? "Operacion ejecutada correctamente.",
                    EntityId   = ReadEntityId(reader)
                };
            }
        }
        while (await reader.NextResultAsync(ct));

        return new SpResult
        {
            HttpStatus = 200,
            Codigo     = "OK",
            Mensaje    = "Operacion ejecutada correctamente."
        };
    }

    /// <summary>Ejecuta un SP de consulta y devuelve una lista mapeada con SqlDataReader.</summary>
    public async Task<List<T>> QueryAsync<T>(
        string storedProcedure,
        SqlParameter[]? parameters,
        Func<SqlDataReader, T> mapper,
        CancellationToken ct = default)
    {
        var items = new List<T>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = CreateCommand(connection, storedProcedure, parameters);
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            items.Add(mapper(reader));
        }

        return items;
    }

    /// <summary>Ejecuta un SP de consulta y devuelve una sola fila mapeada, o null si no hay filas.</summary>
    public async Task<T?> QuerySingleAsync<T>(
        string storedProcedure,
        SqlParameter[]? parameters,
        Func<SqlDataReader, T> mapper,
        CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = CreateCommand(connection, storedProcedure, parameters);
        await using var reader = await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
        {
            return default;
        }

        return mapper(reader);
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string storedProcedure, SqlParameter[]? parameters)
    {
        var command = new SqlCommand(storedProcedure, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        if (parameters is { Length: > 0 })
        {
            command.Parameters.AddRange(parameters);
        }

        return command;
    }

    private static long? ReadEntityId(SqlDataReader reader)
    {
        string[] candidates =
        [
            "EntityId",
            "Id",
            "CitaId",
            "PacienteId",
            "TicketId",
            "ConsultaId",
            "OrdenId",
            "RecetaId"
        ];

        foreach (var column in candidates)
        {
            var value = reader.GetNullableInt64(column);
            if (value.HasValue)
            {
                return value.Value;
            }
        }

        var codigo = reader.GetNullableString("Codigo") ?? reader.GetNullableString("Code");
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            var lastPart = codigo.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
            if (long.TryParse(lastPart, out var parsedId))
            {
                return parsedId;
            }
        }

        return null;
    }
}
