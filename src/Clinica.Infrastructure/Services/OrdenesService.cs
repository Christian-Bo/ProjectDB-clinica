using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Ordenes;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// -----------------------------------------------------------------------------
// Servicio de Órdenes de laboratorio/imagen.
// sp_ActualizarEstadoOrden registra cada cambio en OrdenesHistorial para
// trazabilidad completa. El service no decide los estados; solo los pasa.
// -----------------------------------------------------------------------------
public sealed class OrdenesService : IOrdenesService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<OrdenesService> _logger;

    public OrdenesService(DatabaseConnection db, ILogger<OrdenesService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<OrdenResponseDto>> CrearAsync(
        CrearOrdenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateSpCommand(connection, "dbo.sp_CrearOrdenDesdeConsulta");
        AddParam(command, "@ConsultaId", request.ConsultaId);
        AddParam(command, "@TipoOrden", request.TipoOrden);
        AddParam(command, "@Descripcion", request.Descripcion);
        AddParam(command, "@UsuarioId", request.UsuarioId);
        AddParam(command, "@Urgente", request.Urgencia == "URGENTE" ? 1 : 0);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);

        OrdenResponseDto? data = null;
        if (envelope.HttpStatus is >= 200 and < 300 && envelope.OrdenId.HasValue)
        {
            data = await LoadOrdenAsync(connection, envelope.OrdenId.Value, cancellationToken);
        }

        return BuildResult(envelope, data);
    }

    public async Task<ServiceOperationResult<OrdenResponseDto>> ObtenerAsync(
        long ordenId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var data = await LoadOrdenAsync(connection, ordenId, cancellationToken);

        if (data is null)
        {
            return new ServiceOperationResult<OrdenResponseDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "ORDEN_NO_ENCONTRADA",
                Message = $"No se encontró la orden con id {ordenId}."
            };
        }

        return new ServiceOperationResult<OrdenResponseDto>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "ORDEN_OK",
            Message = "Orden obtenida correctamente.",
            Data = data
        };
    }

    public async Task<ServiceOperationResult<ListaOrdenesResponseDto>> ListarAsync(
        OrdenListFiltersDto filters,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateSpCommand(connection, "dbo.sp_Orden_Listar");
        AddParam(command, "@PacienteId", filters.PacienteId);
        AddParam(command, "@MedicoId", filters.MedicoId);
        AddParam(command, "@Estado", filters.Estado);
        AddParam(command, "@TipoOrden", filters.TipoOrden);
        AddParam(command, "@PageSize", filters.PageSize);
        AddParam(command, "@PageNumber", filters.PageNumber);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<OrdenResponseDto>();
            int total = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                if (total == 0 && !reader.IsDBNull("TotalRegistros"))
                    total = reader.GetInt32OrDefault("TotalRegistros");

                items.Add(MapOrdenRow(reader));
            }

            return new ServiceOperationResult<ListaOrdenesResponseDto>
            {
                HttpStatus = StatusCodes.Status200OK,
                Code = "ORDENES_OK",
                Message = "Órdenes obtenidas correctamente.",
                Data = new ListaOrdenesResponseDto { Total = total, Items = items }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listando órdenes");
            return new ServiceOperationResult<ListaOrdenesResponseDto>
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code = "ERROR_ORDENES",
                Message = ex.Message
            };
        }
    }

    public async Task<ServiceOperationResult<OrdenResponseDto>> ActualizarEstadoAsync(
        long ordenId,
        ActualizarEstadoOrdenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateSpCommand(connection, "dbo.sp_ActualizarEstadoOrden");
        AddParam(command, "@OrdenId", ordenId);
        AddParam(command, "@NuevoEstado", request.NuevoEstado);
        AddParam(command, "@UsuarioId", request.UsuarioId);
        AddParam(command, "@Observacion", request.Observacion);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);

        OrdenResponseDto? data = null;
        if (envelope.HttpStatus is >= 200 and < 300)
        {
            data = await LoadOrdenAsync(connection, ordenId, cancellationToken);
        }

        return BuildResult(envelope, data);
    }

    // =========================================================================
    // HELPERS PRIVADOS
    // =========================================================================

    private async Task<OrdenResponseDto?> LoadOrdenAsync(
        SqlConnection connection,
        long ordenId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateSpCommand(connection, "dbo.sp_Orden_Obtener");
        AddParam(command, "@OrdenId", ordenId);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var orden = MapOrdenRow(reader);

            // Segundo resultset: historial de estados
            var historial = new List<OrdenHistorialDto>();
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    historial.Add(new OrdenHistorialDto
                    {
                        EstadoAnterior = reader.GetNullableString("EstadoAnterior") ?? string.Empty,
                        EstadoNuevo = reader.GetNullableString("EstadoNuevo") ?? string.Empty,
                        Observacion = reader.GetNullableString("Observacion"),
                        UsuarioNombre = reader.GetNullableString("UsuarioNombre") ?? string.Empty,
                        FechaCambio = reader.GetDateTime(reader.GetOrdinal("FechaCambio"))
                    });
                }
            }
            orden.Historial = historial;

            return orden;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando orden {OrdenId}", ordenId);
            return null;
        }
    }

    private static OrdenResponseDto MapOrdenRow(SqlDataReader reader) => new()
    {
        OrdenId = reader.GetInt64OrDefault("OrdenId"),
        ConsultaId = reader.GetInt64OrDefault("ConsultaId"),
        PacienteId = reader.GetInt32OrDefault("PacienteId"),
        PacienteNombre = reader.GetNullableString("PacienteNombre") ?? string.Empty,
        MedicoNombre = reader.GetNullableString("MedicoNombre") ?? string.Empty,
        TipoOrden = reader.GetNullableString("TipoOrden") ?? string.Empty,
        Descripcion = reader.GetNullableString("Descripcion") ?? string.Empty,
        Estado = reader.GetNullableString("Estado") ?? string.Empty,
        Urgencia = reader.GetNullableString("Urgencia"),
        FechaEmision = reader.GetDateTime(reader.GetOrdinal("FechaEmision"))
    };

    private static ServiceOperationResult<T> BuildResult<T>(SpEnvelope envelope, T? data)
        => new()
        {
            HttpStatus = envelope.HttpStatus,
            Code = envelope.Code,
            Message = envelope.Message,
            Data = data
        };

    private static long? TryGetLongColumn(SqlDataReader reader, string columnName)
    {
        try
        {
            var i = reader.GetOrdinal(columnName);
            return reader.IsDBNull(i) ? null : reader.GetInt64(i);
        }
        catch { return null; }
    }

        private async Task<SpEnvelope> ExecuteEnvelopeAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return new SpEnvelope { HttpStatus = 500, Code = "SP_SIN_RESPUESTA", Message = "El stored procedure no devolvió resultado." };

            return new SpEnvelope
            {
                HttpStatus = reader.GetInt32OrDefault("HttpStatus", 500),
                Code = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO",
                Message = reader.GetNullableString("Mensaje") ?? "Operación ejecutada.",
                OrdenId = TryGetLongColumn(reader, "OrdenId")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando {Sp}", command.CommandText);
            return new SpEnvelope { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = ex.Message };
        }
    }

    private static SqlCommand CreateSpCommand(SqlConnection connection, string spName)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = spName;
        cmd.CommandTimeout = 60;
        return cmd;
    }

    private static void AddParam(SqlCommand cmd, string name, object? value)
        => cmd.Parameters.Add(new SqlParameter(name, value ?? DBNull.Value));

    private sealed class SpEnvelope
    {
        public int HttpStatus { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public long? OrdenId { get; init; }
    }
}