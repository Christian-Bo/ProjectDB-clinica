using System.Data;
using System.Text.Json;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Consultas;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace Clinica.Infrastructure.Services;

// =============================================================================
// ConsultasService — Dev4
// Implementa todas las operaciones del módulo de consulta médica.
// Sigue el patrón establecido: stored procedures, sin ORM, sin migraciones.
// =============================================================================
public sealed class ConsultasService : IConsultasService
{
    private readonly DatabaseConnection _db;

    public ConsultasService(DatabaseConnection db)
    {
        _db = db;
    }

    // -------------------------------------------------------------------------
    // Abre una consulta desde un ticket válido.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<ConsultaResponseDto>> AbrirDesdeTicketAsync(
        AbrirConsultaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = _db.CreateConnection();
            await using var cmd = CreateSpCommand(conn, "sp_AbrirConsultaDesdeTicket");

            cmd.Parameters.AddWithValue("@TicketId",      request.TicketId);
            cmd.Parameters.AddWithValue("@UsuarioId",     request.UsuarioId ?? 1);
            cmd.Parameters.AddWithValue("@ConsultorioId", 1);
            cmd.Parameters.AddWithValue("@Modalidad",     request.Modalidad ?? "PRESENCIAL");

            await conn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var consultaId = reader.GetInt64OrDefault("ConsultaId");
                return new ServiceOperationResult<ConsultaResponseDto>
                {
                    HttpStatus = StatusCodes.Status200OK,
                    Code       = "CONSULTA_ABIERTA",
                    Message    = "Consulta abierta correctamente.",
                    Data       = new ConsultaResponseDto { ConsultaId = consultaId }
                };
            }

            return new ServiceOperationResult<ConsultaResponseDto>
            {
                HttpStatus = StatusCodes.Status400BadRequest,
                Code       = "CONSULTA_ERROR",
                Message    = "No se pudo abrir la consulta."
            };
        }
        catch (SqlException ex)
        {
            return new ServiceOperationResult<ConsultaResponseDto>
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code       = "SQL_ERROR",
                Message    = ex.Message
            };
        }
    }

    // -------------------------------------------------------------------------
    // Cierra la consulta. A partir de aquí el registro es inmutable (trigger).
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<ConsultaResponseDto>> CerrarAsync(
        long consultaId,
        CerrarConsultaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var diagnosticosJson = JsonSerializer.Serialize(
                request.Diagnosticos,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await using var conn = _db.CreateConnection();
            await using var cmd  = CreateSpCommand(conn, "sp_CerrarConsulta");

            cmd.Parameters.AddWithValue("@ConsultaId",            consultaId);
            cmd.Parameters.AddWithValue("@UsuarioId",             request.UsuarioId ?? 1);
            cmd.Parameters.AddWithValue("@DiagnosticosJson",      diagnosticosJson);
            cmd.Parameters.AddWithValue("@Hallazgos",             (object?)request.Hallazgos            ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Plan",                  (object?)request.Plan                 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Observaciones",         (object?)request.Observaciones        ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PresionSistolica",      (object?)request.PresionSistolica      ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PresionDiastolica",     (object?)request.PresionDiastolica     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FrecuenciaCardiaca",    (object?)request.FrecuenciaCardiaca    ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FrecuenciaRespiratoria",(object?)request.FrecuenciaRespiratoria?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Temperatura",           (object?)request.Temperatura           ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SaturacionOxigeno",     (object?)request.SaturacionOxigeno     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PesoKg",                (object?)request.PesoKg                ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TallaCm",               (object?)request.TallaCm               ?? DBNull.Value);

            await conn.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            return new ServiceOperationResult<ConsultaResponseDto>
            {
                HttpStatus = StatusCodes.Status200OK,
                Code       = "CONSULTA_CERRADA",
                Message    = "Consulta cerrada correctamente.",
                Data       = new ConsultaResponseDto { ConsultaId = consultaId }
            };
        }
        catch (SqlException ex)
        {
            return new ServiceOperationResult<ConsultaResponseDto>
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code       = "SQL_ERROR",
                Message    = ex.Message
            };
        }
    }

    // -------------------------------------------------------------------------
    // Agrega una nota de corrección. No toca el registro original.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<ConsultaResponseDto>> AgregarNotaCorreccionAsync(
        long consultaId,
        NotaCorreccionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = _db.CreateConnection();
            await using var cmd  = CreateSpCommand(conn, "sp_AgregarNotaCorreccionConsulta");

            cmd.Parameters.AddWithValue("@ConsultaId", consultaId);
            cmd.Parameters.AddWithValue("@UsuarioId",  request.UsuarioId ?? 1);
            cmd.Parameters.AddWithValue("@Nota",       request.Nota);

            await conn.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            return new ServiceOperationResult<ConsultaResponseDto>
            {
                HttpStatus = StatusCodes.Status200OK,
                Code       = "NOTA_AGREGADA",
                Message    = "Nota de corrección agregada correctamente.",
                Data       = new ConsultaResponseDto { ConsultaId = consultaId }
            };
        }
        catch (SqlException ex)
        {
            return new ServiceOperationResult<ConsultaResponseDto>
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code       = "SQL_ERROR",
                Message    = ex.Message
            };
        }
    }

    // -------------------------------------------------------------------------
    // Obtiene la consulta completa: datos principales + diagnósticos + notas.
    // El SP devuelve 3 resultsets.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<ConsultaResponseDto>> ObtenerAsync(
        long consultaId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = _db.CreateConnection();
            await using var cmd  = CreateSpCommand(conn, "sp_Consulta_ObtenerCompleta");

            cmd.Parameters.AddWithValue("@ConsultaId", consultaId);

            await conn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            // --- Resultset 1: datos principales ---
            if (!await reader.ReadAsync(cancellationToken))
                return new ServiceOperationResult<ConsultaResponseDto>
                {
                    HttpStatus = StatusCodes.Status404NotFound,
                    Code       = "NOT_FOUND",
                    Message    = "Consulta no encontrada."
                };

            var dto = new ConsultaResponseDto
            {
                ConsultaId      = reader.GetInt64OrDefault("ConsultaId"),
                TicketId        = reader.GetInt64OrDefault("TicketId"),
                PacienteId      = reader.GetInt32OrDefault("PacienteId"),
                PacienteNombre  = reader.GetNullableString("PacienteNombre") ?? string.Empty,
                MedicoId        = reader.GetInt32OrDefault("MedicoId"),
                MedicoNombre    = reader.GetNullableString("MedicoNombre")   ?? string.Empty,
                Estado          = reader.GetNullableString("Estado")         ?? string.Empty,
                Modalidad       = reader.GetNullableString("Modalidad")      ?? string.Empty,
                MotivoConsulta  = reader.GetNullableString("MotivoConsulta"),
                Hallazgos       = reader.GetNullableString("Hallazgos"),
                Plan            = reader.GetNullableString("Plan"),
                FechaHoraInicio = reader.GetDateTimeOrDefault("FechaHoraInicio"),
                FechaHoraCierre = reader.GetNullableDateTime("FechaHoraCierre"),
            };

            // Signos vitales opcionales en el mismo resultset
            if (reader.HasColumn("PresionSistolica"))
            {
                dto.SignosVitales = new SignosVitalesDto
                {
                    PresionSistolica       = reader.IsDBNull(reader.GetOrdinal("PresionSistolica"))       ? null : reader.GetDecimal(reader.GetOrdinal("PresionSistolica")),
                    PresionDiastolica      = reader.IsDBNull(reader.GetOrdinal("PresionDiastolica"))      ? null : reader.GetDecimal(reader.GetOrdinal("PresionDiastolica")),
                    FrecuenciaCardiaca     = reader.IsDBNull(reader.GetOrdinal("FrecuenciaCardiaca"))     ? null : reader.GetDecimal(reader.GetOrdinal("FrecuenciaCardiaca")),
                    FrecuenciaRespiratoria = reader.IsDBNull(reader.GetOrdinal("FrecuenciaRespiratoria")) ? null : reader.GetDecimal(reader.GetOrdinal("FrecuenciaRespiratoria")),
                    Temperatura            = reader.IsDBNull(reader.GetOrdinal("Temperatura"))            ? null : reader.GetDecimal(reader.GetOrdinal("Temperatura")),
                    SaturacionOxigeno      = reader.IsDBNull(reader.GetOrdinal("SaturacionOxigeno"))      ? null : reader.GetDecimal(reader.GetOrdinal("SaturacionOxigeno")),
                    PesoKg                 = reader.IsDBNull(reader.GetOrdinal("PesoKg"))                 ? null : reader.GetDecimal(reader.GetOrdinal("PesoKg")),
                    TallaCm                = reader.IsDBNull(reader.GetOrdinal("TallaCm"))                ? null : reader.GetDecimal(reader.GetOrdinal("TallaCm")),
                    Imc                    = reader.HasColumn("IMC") && !reader.IsDBNull(reader.GetOrdinal("IMC")) ? reader.GetDecimal(reader.GetOrdinal("IMC")) : null,
                };
            }

            // --- Resultset 2: diagnósticos ---
            var diagnosticos = new List<DiagnosticoDto>();
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    diagnosticos.Add(new DiagnosticoDto
                    {
                        DiagnosticoId   = reader.GetInt64OrDefault("DiagnosticoId"),
                        CodigoCie       = reader.GetNullableString("CodigoCIE10")      ?? string.Empty,
                        Descripcion     = reader.GetNullableString("DescripcionCIE10") ?? string.Empty,
                        TipoDiagnostico = reader.GetNullableString("TipoDiagnostico")  ?? string.Empty,
                    });
                }
            }
            dto.Diagnosticos = diagnosticos.AsReadOnly();

            // --- Resultset 3: notas de corrección ---
            var notas = new List<NotaCorreccionDto>();
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    notas.Add(new NotaCorreccionDto
                    {
                        NotaId        = reader.GetInt64OrDefault("NotaId"),
                        Nota          = reader.GetNullableString("Nota")          ?? string.Empty,
                        UsuarioNombre = reader.GetNullableString("UsuarioNombre") ?? string.Empty,
                        FechaCreacion = reader.GetDateTimeOrDefault("FechaCreacion"),
                    });
                }
            }
            dto.NotasCorreccion = notas.AsReadOnly();

            return new ServiceOperationResult<ConsultaResponseDto>
            {
                HttpStatus = StatusCodes.Status200OK,
                Code       = "CONSULTA_OK",
                Message    = "Consulta obtenida correctamente.",
                Data       = dto
            };
        }
        catch (SqlException ex)
        {
            return new ServiceOperationResult<ConsultaResponseDto>
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code       = "SQL_ERROR",
                Message    = ex.Message
            };
        }
    }

    // -------------------------------------------------------------------------
    // Historial clínico del paciente.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<HistorialClinicoResponseDto>> ObtenerHistorialAsync(
        int pacienteId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = _db.CreateConnection();
            await using var cmd  = CreateSpCommand(conn, "sp_HistorialClinico_Paciente");

            cmd.Parameters.AddWithValue("@PacienteId", pacienteId);

            await conn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var historial = new HistorialClinicoResponseDto { PacienteId = pacienteId };
            var consultas = new List<ConsultaResumenDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.IsNullOrEmpty(historial.PacienteNombre))
                    historial.PacienteNombre = reader.GetNullableString("PacienteNombre") ?? string.Empty;

                consultas.Add(new ConsultaResumenDto
                {
                    ConsultaId      = reader.GetInt64OrDefault("ConsultaId"),
                    MedicoNombre    = reader.GetNullableString("MedicoNombre") ?? string.Empty,
                    Estado          = reader.GetNullableString("Estado")       ?? string.Empty,
                    MotivoConsulta  = reader.GetNullableString("MotivoConsulta"),
                    FechaHoraInicio = reader.GetDateTimeOrDefault("FechaHoraInicio"),
                    FechaHoraCierre = reader.GetNullableDateTime("FechaHoraCierre"),
                    TotalDiagnosticos = reader.GetInt32OrDefault("TotalDiagnosticos"),
                    TotalRecetas      = reader.GetInt32OrDefault("TotalRecetas"),
                    TotalOrdenes      = reader.GetInt32OrDefault("TotalOrdenes"),
                });
            }

            historial.Consultas = consultas.AsReadOnly();

            return new ServiceOperationResult<HistorialClinicoResponseDto>
            {
                HttpStatus = StatusCodes.Status200OK,
                Code       = "HISTORIAL_OK",
                Message    = "Historial obtenido correctamente.",
                Data       = historial
            };
        }
        catch (SqlException ex)
        {
            return new ServiceOperationResult<HistorialClinicoResponseDto>
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code       = "SQL_ERROR",
                Message    = ex.Message
            };
        }
    }

    // -------------------------------------------------------------------------
    // Lista consultas con filtros opcionales — usa sp_Consultas_Listar.
    // Usado por la página "Mis consultas" del módulo médico.
    // -------------------------------------------------------------------------
    public async Task<ServiceOperationResult<List<ConsultaListadoDto>>> ListarAsync(
        ListaConsultasRequestDto filtros,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = _db.CreateConnection();
            await using var cmd  = CreateSpCommand(conn, "sp_Consultas_Listar");

            cmd.Parameters.AddWithValue("@MedicoId",   (object?)filtros.MedicoId   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado",     (object?)filtros.Estado     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PacienteId", (object?)filtros.PacienteId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PageNumber", filtros.PageNumber);
            cmd.Parameters.AddWithValue("@PageSize",   filtros.PageSize);

            await conn.OpenAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var lista = new List<ConsultaListadoDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                lista.Add(new ConsultaListadoDto
                {
                    ConsultaId        = reader.GetInt64OrDefault("ConsultaId"),
                    TicketId          = reader.GetInt64OrDefault("TicketId"),
                    PacienteId        = reader.GetInt32OrDefault("PacienteId"),
                    PacienteNombre    = reader.GetNullableString("PacienteNombre") ?? string.Empty,
                    MedicoId          = reader.GetInt32OrDefault("MedicoId"),
                    MedicoNombre      = reader.GetNullableString("MedicoNombre")   ?? string.Empty,
                    Estado            = reader.GetNullableString("Estado")         ?? string.Empty,
                    Modalidad         = reader.GetNullableString("Modalidad")      ?? string.Empty,
                    MotivoConsulta    = reader.GetNullableString("MotivoConsulta"),
                    Hallazgos         = reader.GetNullableString("Hallazgos"),
                    Plan              = reader.GetNullableString("Plan"),
                    FechaHoraInicio   = reader.GetDateTimeOrDefault("FechaHoraInicio"),
                    FechaHoraCierre   = reader.GetNullableDateTime("FechaHoraCierre"),
                    TotalDiagnosticos = reader.GetInt32OrDefault("TotalDiagnosticos"),
                    TotalNotas        = reader.GetInt32OrDefault("TotalNotas"),
                });
            }

            return new ServiceOperationResult<List<ConsultaListadoDto>>
            {
                HttpStatus = StatusCodes.Status200OK,
                Code       = "CONSULTAS_OK",
                Message    = "Consultas obtenidas correctamente.",
                Data       = lista
            };
        }
        catch (SqlException ex)
        {
            return new ServiceOperationResult<List<ConsultaListadoDto>>
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code       = "SQL_ERROR",
                Message    = ex.Message
            };
        }
    }

    // -------------------------------------------------------------------------
    // Helper: crea un SqlCommand de tipo StoredProcedure.
    // -------------------------------------------------------------------------
    private static SqlCommand CreateSpCommand(SqlConnection connection, string spName)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandType    = CommandType.StoredProcedure;
        cmd.CommandText    = spName;
        cmd.CommandTimeout = 60;
        return cmd;
    }
}
