using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Consultas;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// -----------------------------------------------------------------------------
// Servicio del módulo Dev4 — Consulta Médica e Historia Clínica.
//
// Patrón: igual al TicketQueueService ya existente en el proyecto.
// - Cada método abre su propia conexión y ejecuta el SP correspondiente.
// - El SP devuelve siempre una fila con HttpStatus, Codigo, Mensaje.
// - Si el resultado es exitoso, se llama sp_Consulta_ObtenerCompleta
//   para devolver el objeto completo al frontend.
// - Nunca se hace lógica de negocio aquí; eso lo decide SQL Server.
// -----------------------------------------------------------------------------
public sealed class ConsultasService : IConsultasService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<ConsultasService> _logger;

    public ConsultasService(DatabaseConnection db, ILogger<ConsultasService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Abre una consulta desde un ticket válido.
    // sp_AbrirConsultaDesdeTicket valida que el ticket esté en estado correcto.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<ConsultaResponseDto>> AbrirDesdeTicketAsync(
        AbrirConsultaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateSpCommand(connection, "dbo.sp_AbrirConsultaDesdeTicket");
        AddParam(command, "@TicketId", request.TicketId);
        AddParam(command, "@UsuarioId", request.UsuarioId);
        AddParam(command, "@Modalidad", request.Modalidad);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);

        ConsultaResponseDto? data = null;
        if (envelope.HttpStatus is >= 200 and < 300 && envelope.ConsultaId.HasValue)
        {
            data = await LoadConsultaCompletaAsync(connection, envelope.ConsultaId.Value, cancellationToken);
        }

        return BuildResult(envelope, data);
    }

    // -------------------------------------------------------------------------
    // Cierra la consulta y la deja inmutable.
    // Después del cierre no se puede hacer PUT sobre el registro base.
    // Los diagnósticos se envían como JSON al SP.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<ConsultaResponseDto>> CerrarAsync(
        long consultaId,
        CerrarConsultaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateSpCommand(connection, "dbo.sp_CerrarConsulta");
        AddParam(command, "@ConsultaId", consultaId);
        AddParam(command, "@UsuarioId", request.UsuarioId);
        AddParam(command, "@DiagnosticosJson", System.Text.Json.JsonSerializer.Serialize(request.Diagnosticos));
        AddParam(command, "@Hallazgos", request.Hallazgos);
        AddParam(command, "@Plan", request.Plan);
        AddParam(command, "@Observaciones", request.Observaciones);
        // Signos vitales opcionales
        AddParam(command, "@Peso", request.PesoKg);
        AddParam(command, "@Talla", request.TallaCm);
        AddParam(command, "@TemperaturaC", request.Temperatura);
        AddParam(command, "@PresionSistolica", request.PresionSistolica);
        AddParam(command, "@PresionDiastolica", request.PresionDiastolica);
        AddParam(command, "@FrecuenciaCardiaca", request.FrecuenciaCardiaca);
        AddParam(command, "@FrecuenciaRespiratoria", request.FrecuenciaRespiratoria);
        AddParam(command, "@SaturacionOxigeno", request.SaturacionOxigeno);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);

        ConsultaResponseDto? data = null;
        if (envelope.HttpStatus is >= 200 and < 300)
        {
            data = await LoadConsultaCompletaAsync(connection, consultaId, cancellationToken);
        }

        return BuildResult(envelope, data);
    }

    // -------------------------------------------------------------------------
    // Agrega una nota de corrección append-only sobre una consulta cerrada.
    // No modifica el registro original; agrega una fila en NotasCorreccion.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<ConsultaResponseDto>> AgregarNotaCorreccionAsync(
        long consultaId,
        NotaCorreccionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateSpCommand(connection, "dbo.sp_AgregarNotaCorreccionConsulta");
        AddParam(command, "@ConsultaId", consultaId);
        AddParam(command, "@Nota", request.Nota);
        AddParam(command, "@UsuarioId", request.UsuarioId);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);

        ConsultaResponseDto? data = null;
        if (envelope.HttpStatus is >= 200 and < 300)
        {
            data = await LoadConsultaCompletaAsync(connection, consultaId, cancellationToken);
        }

        return BuildResult(envelope, data);
    }

    // -------------------------------------------------------------------------
    // Obtiene una consulta completa con diagnósticos, signos vitales y notas.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<ConsultaResponseDto>> ObtenerAsync(
        long consultaId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var data = await LoadConsultaCompletaAsync(connection, consultaId, cancellationToken);

        if (data is null)
        {
            return new ServiceOperationResult<ConsultaResponseDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "CONSULTA_NO_ENCONTRADA",
                Message = $"No se encontró la consulta con id {consultaId}."
            };
        }

        return new ServiceOperationResult<ConsultaResponseDto>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "CONSULTA_OK",
            Message = "Consulta obtenida correctamente.",
            Data = data
        };
    }

    // -------------------------------------------------------------------------
    // Historial clínico completo del paciente.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<HistorialClinicoResponseDto>> ObtenerHistorialAsync(
        int pacienteId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateSpCommand(connection, "dbo.sp_HistorialClinico_Paciente");
        AddParam(command, "@PacienteId", pacienteId);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var consultas = new List<ConsultaResumenDto>();
            string pacienteNombre = string.Empty;

            while (await reader.ReadAsync(cancellationToken))
            {
                // La primera fila también trae el nombre del paciente
                if (string.IsNullOrEmpty(pacienteNombre))
                    pacienteNombre = reader.GetNullableString("PacienteNombre") ?? $"Paciente #{pacienteId}";

                consultas.Add(new ConsultaResumenDto
                {
                    ConsultaId = reader.GetInt64OrDefault("ConsultaId"),
                    MedicoNombre = reader.GetNullableString("MedicoNombre") ?? string.Empty,
                    Estado = reader.GetNullableString("Estado") ?? string.Empty,
                    MotivoConsulta = reader.GetNullableString("MotivoConsulta"),
                    FechaHoraInicio = reader.GetDateTime(reader.GetOrdinal("FechaHoraInicio")),
                    FechaHoraCierre = reader.IsDBNull("FechaHoraCierre") ? null : reader.GetDateTime(reader.GetOrdinal("FechaHoraCierre")),
                    TotalDiagnosticos = reader.GetInt32OrDefault("TotalDiagnosticos"),
                    TotalRecetas = reader.GetInt32OrDefault("TotalRecetas"),
                    TotalOrdenes = reader.GetInt32OrDefault("TotalOrdenes")
                });
            }

            return new ServiceOperationResult<HistorialClinicoResponseDto>
            {
                HttpStatus = StatusCodes.Status200OK,
                Code = "HISTORIAL_OK",
                Message = "Historial clínico obtenido correctamente.",
                Data = new HistorialClinicoResponseDto
                {
                    PacienteId = pacienteId,
                    PacienteNombre = pacienteNombre,
                    Consultas = consultas
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo historial clínico del paciente {PacienteId}", pacienteId);
            return new ServiceOperationResult<HistorialClinicoResponseDto>
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code = "ERROR_HISTORIAL",
                Message = ex.Message
            };
        }
    }

    // =========================================================================
    // HELPERS PRIVADOS
    // =========================================================================

    private async Task<ConsultaResponseDto?> LoadConsultaCompletaAsync(
        SqlConnection connection,
        long consultaId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateSpCommand(connection, "dbo.sp_Consulta_ObtenerCompleta");
        AddParam(command, "@ConsultaId", consultaId);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Primer resultset: datos principales de la consulta
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var consulta = new ConsultaResponseDto
            {
                ConsultaId = reader.GetInt64OrDefault("ConsultaId"),
                TicketId = reader.GetInt64OrDefault("TicketId"),
                PacienteId = reader.GetInt32OrDefault("PacienteId"),
                PacienteNombre = reader.GetNullableString("PacienteNombre") ?? string.Empty,
                MedicoId = reader.GetInt32OrDefault("MedicoId"),
                MedicoNombre = reader.GetNullableString("MedicoNombre") ?? string.Empty,
                Estado = reader.GetNullableString("Estado") ?? string.Empty,
                Modalidad = reader.GetNullableString("Modalidad") ?? string.Empty,
                MotivoConsulta = reader.GetNullableString("MotivoConsulta"),
                Hallazgos = reader.GetNullableString("Hallazgos"),
                Plan = reader.GetNullableString("Plan"),
                FechaHoraInicio = reader.GetDateTime(reader.GetOrdinal("FechaHoraInicio")),
                FechaHoraCierre = reader.IsDBNull("FechaHoraCierre") ? null : reader.GetDateTime(reader.GetOrdinal("FechaHoraCierre"))
            };

            // Signos vitales si el SP los incluye en el mismo resultset
            if (!reader.IsDBNull("PresionSistolica"))
            {
                consulta.SignosVitales = new SignosVitalesDto
                {
                    PresionSistolica = reader.IsDBNull("PresionSistolica") ? null : reader.GetDecimal(reader.GetOrdinal("PresionSistolica")),
                    PresionDiastolica = reader.IsDBNull("PresionDiastolica") ? null : reader.GetDecimal(reader.GetOrdinal("PresionDiastolica")),
                    FrecuenciaCardiaca = reader.IsDBNull("FrecuenciaCardiaca") ? null : reader.GetDecimal(reader.GetOrdinal("FrecuenciaCardiaca")),
                    FrecuenciaRespiratoria = reader.IsDBNull("FrecuenciaRespiratoria") ? null : reader.GetDecimal(reader.GetOrdinal("FrecuenciaRespiratoria")),
                    Temperatura = reader.IsDBNull("Temperatura") ? null : reader.GetDecimal(reader.GetOrdinal("Temperatura")),
                    SaturacionOxigeno = reader.IsDBNull("SaturacionOxigeno") ? null : reader.GetDecimal(reader.GetOrdinal("SaturacionOxigeno")),
                    PesoKg = reader.IsDBNull("PesoKg") ? null : reader.GetDecimal(reader.GetOrdinal("PesoKg")),
                    TallaCm = reader.IsDBNull("TallaCm") ? null : reader.GetDecimal(reader.GetOrdinal("TallaCm")),
                    Imc = reader.IsDBNull("Imc") ? null : reader.GetDecimal(reader.GetOrdinal("Imc"))
                };
            }

            // Segundo resultset: diagnósticos
            var diagnosticos = new List<DiagnosticoDto>();
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    diagnosticos.Add(new DiagnosticoDto
                    {
                        DiagnosticoId = reader.GetInt64OrDefault("DiagnosticoId"),
                        CodigoCie = reader.GetNullableString("CodigoCie") ?? string.Empty,
                        Descripcion = reader.GetNullableString("Descripcion") ?? string.Empty,
                        TipoDiagnostico = reader.GetNullableString("TipoDiagnostico") ?? string.Empty
                    });
                }
            }
            consulta.Diagnosticos = diagnosticos;

            // Tercer resultset: notas de corrección
            var notas = new List<NotaCorreccionDto>();
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    notas.Add(new NotaCorreccionDto
                    {
                        NotaId = reader.GetInt64OrDefault("NotaId"),
                        Nota = reader.GetNullableString("Nota") ?? string.Empty,
                        UsuarioNombre = reader.GetNullableString("UsuarioNombre") ?? string.Empty,
                        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion"))
                    });
                }
            }
            consulta.NotasCorreccion = notas;

            return consulta;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando consulta completa {ConsultaId}", consultaId);
            return null;
        }
    }

    private static ServiceOperationResult<T> BuildResult<T>(SpEnvelope envelope, T? data)
        => new()
        {
            HttpStatus = envelope.HttpStatus,
            Code = envelope.Code,
            Message = envelope.Message,
            Data = data
        };

    private async Task<SpEnvelope> ExecuteEnvelopeAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new SpEnvelope
                {
                    HttpStatus = StatusCodes.Status500InternalServerError,
                    Code = "SP_SIN_RESPUESTA",
                    Message = "El stored procedure no devolvió una fila de resultado."
                };
            }

            return new SpEnvelope
            {
                HttpStatus = reader.GetInt32OrDefault("HttpStatus", StatusCodes.Status500InternalServerError),
                Code = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO",
                Message = reader.GetNullableString("Mensaje") ?? "Operación ejecutada.",
                ConsultaId = TryGetConsultaId(reader)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando stored procedure {Sp}", command.CommandText);
            return new SpEnvelope
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code = "ERROR_INFRAESTRUCTURA",
                Message = ex.Message
            };
        }
    }

    private static long? TryGetConsultaId(SqlDataReader reader)
    {
        try
        {
            var i = reader.GetOrdinal("ConsultaId");
            return reader.IsDBNull(i) ? null : reader.GetInt64(i);
        }
        catch { return null; }
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
        public long? ConsultaId { get; init; }
    }
}