using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Recetas;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// -----------------------------------------------------------------------------
// Servicio de Recetas.
// sp_CrearRecetaDesdeConsulta valida alergias y principio activo duplicado.
// Si la BD detecta conflicto, devuelve un HttpStatus 409 o 422 con el código
// de error clínico y el service lo pasa directo al controlador.
// -----------------------------------------------------------------------------
public sealed class RecetasService : IRecetasService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<RecetasService> _logger;

    public RecetasService(DatabaseConnection db, ILogger<RecetasService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<RecetaResponseDto>> CrearAsync(
        CrearRecetaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var medicamentosJson = System.Text.Json.JsonSerializer.Serialize(
    request.Items,
    new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    });
_logger.LogInformation(">> JSON medicamentos enviado al SP: {Json}", medicamentosJson);

await using var command = CreateSpCommand(connection, "dbo.sp_CrearRecetaDesdeConsulta");
AddParam(command, "@ConsultaId", request.ConsultaId);
AddParam(command, "@UsuarioId", request.UsuarioId);
AddParam(command, "@MedicamentosJson", medicamentosJson);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);

        RecetaResponseDto? data = null;
        if (envelope.HttpStatus is >= 200 and < 300 && envelope.RecetaId.HasValue)
        {
            data = await LoadRecetaAsync(connection, envelope.RecetaId.Value, cancellationToken);
        }

        return new ServiceOperationResult<RecetaResponseDto>
        {
            HttpStatus = envelope.HttpStatus,
            Code = envelope.Code,
            Message = envelope.Message,
            Data = data
        };
    }

    public async Task<ServiceOperationResult<RecetaResponseDto>> ObtenerAsync(
        long recetaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var data = await LoadRecetaAsync(connection, recetaId, cancellationToken);

        if (data is null)
        {
            return new ServiceOperationResult<RecetaResponseDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "RECETA_NO_ENCONTRADA",
                Message = $"No se encontró la receta con id {recetaId}."
            };
        }

        return new ServiceOperationResult<RecetaResponseDto>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "RECETA_OK",
            Message = "Receta obtenida correctamente.",
            Data = data
        };
    }

    // =========================================================================
    // HELPERS PRIVADOS
    // =========================================================================

    private async Task<RecetaResponseDto?> LoadRecetaAsync(
        SqlConnection connection,
        long recetaId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateSpCommand(connection, "dbo.sp_Receta_Obtener");
        AddParam(command, "@RecetaId", recetaId);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Primer resultset: cabecera de la receta
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var receta = new RecetaResponseDto
            {
                RecetaId = reader.GetInt64OrDefault("RecetaId"),
                ConsultaId = reader.GetInt64OrDefault("ConsultaId"),
                PacienteId = reader.GetInt32OrDefault("PacienteId"),
                PacienteNombre = reader.GetNullableString("PacienteNombre") ?? string.Empty,
                MedicoNombre = reader.GetNullableString("MedicoNombre") ?? string.Empty,
                Estado = reader.GetNullableString("Estado") ?? string.Empty,
                FechaEmision = reader.GetDateTime(reader.GetOrdinal("FechaEmision")),
                FechaDespacho = reader.IsDBNull("FechaDespacho") ? null : reader.GetDateTime(reader.GetOrdinal("FechaDespacho"))
            };

            // Segundo resultset: items de la receta
            var items = new List<RecetaItemResponseDto>();
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(new RecetaItemResponseDto
                    {
                        MedicamentoId = reader.GetInt32OrDefault("MedicamentoId"),
                        NombreComercial = reader.GetNullableString("NombreComercial") ?? string.Empty,
                        PrincipioActivo = reader.GetNullableString("PrincipioActivo") ?? string.Empty,
                        Dosis = reader.GetNullableString("Dosis") ?? string.Empty,
                        Frecuencia = reader.GetNullableString("Frecuencia") ?? string.Empty,
                        DuracionDias = reader.GetInt32OrDefault("DuracionDias"),
                        Indicaciones = reader.GetNullableString("Indicaciones")
                    });
                }
            }
            receta.Items = items;

            return receta;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando receta {RecetaId}", recetaId);
            return null;
        }
    }

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
                RecetaId = TryGetLongColumn(reader, "RecetaId")
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
        public long? RecetaId { get; init; }
    }
}