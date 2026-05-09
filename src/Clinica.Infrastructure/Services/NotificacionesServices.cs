using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Notificaciones;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// =============================================================================
// PlantillasNotificacionService
// SPs:
//   sp_PlantillaNotificacion_Upsert → @PlantillaId, @TipoEvento, @Canal,
//                                      @Asunto, @Cuerpo, @VariablesJSON,
//                                      @Activo, @FechaModificacion
//                                      devuelve: IdGenerado | IdActualizado
//   sp_PlantillaNotificacion_Obtener → @PlantillaId
//   sp_PlantillaNotificacion_Listar  → @TipoEvento, @Canal, @Activo
// =============================================================================
public sealed class PlantillasNotificacionService : IPlantillasNotificacionService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<PlantillasNotificacionService> _logger;

    public PlantillasNotificacionService(DatabaseConnection db, ILogger<PlantillasNotificacionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<PlantillaNotificacionDto>> UpsertPlantillaAsync(
        PlantillaNotificacionUpsertDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_PlantillaNotificacion_Upsert");
        P(cmd, "@PlantillaId",       req.PlantillaId);
        P(cmd, "@TipoEvento",        req.TipoEvento);
        P(cmd, "@Canal",             req.Canal);
        P(cmd, "@Asunto",            req.Asunto);
        P(cmd, "@Cuerpo",            req.Cuerpo);
        P(cmd, "@VariablesJSON",     req.VariablesJSON);
        P(cmd, "@Activo",            req.Activo);
        P(cmd, "@FechaModificacion", req.FechaModificacion ?? (object)DBNull.Value);

        var env = await ExecAsync(cmd, ct);
        if (!env.IsOk) return Fail<PlantillaNotificacionDto>(env);

        var id = env.IntId ?? req.PlantillaId;
        if (!id.HasValue) return Ok<PlantillaNotificacionDto>(env, null);

        var dto = await CargarAsync(conn, id.Value, ct);
        return Ok(env, dto);
    }

    public async Task<ServiceOperationResult<PlantillaNotificacionDto>> ObtenerPlantillaAsync(
        int plantillaId, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        var dto = await CargarAsync(conn, plantillaId, ct);
        if (dto is null)
            return new ServiceOperationResult<PlantillaNotificacionDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "PLANTILLA_NO_ENCONTRADA",
                Message = "No se encontró la plantilla."
            };

        return new ServiceOperationResult<PlantillaNotificacionDto>
            { HttpStatus = 200, Code = "PLANTILLA_OBTENIDA", Message = "OK.", Data = dto };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<PlantillaNotificacionDto>>> ListarPlantillasAsync(
        PlantillaListarFiltrosDto filtros, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_PlantillaNotificacion_Listar");
        P(cmd, "@TipoEvento", filtros.TipoEvento);
        P(cmd, "@Canal",      filtros.Canal);
        P(cmd, "@Activo",     filtros.Activo);

        var list = new List<PlantillaNotificacionDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return new ServiceOperationResult<IReadOnlyList<PlantillaNotificacionDto>>
        {
            HttpStatus = 200,
            Code = "PLANTILLAS_LISTADAS",
            Message = list.Count == 0 ? "No se encontraron plantillas." : $"{list.Count} plantilla(s).",
            Data = list
        };
    }

    private async Task<PlantillaNotificacionDto?> CargarAsync(SqlConnection conn, int id, CancellationToken ct)
    {
        await using var cmd = Sp(conn, "dbo.sp_PlantillaNotificacion_Obtener");
        P(cmd, "@PlantillaId", id);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    private static PlantillaNotificacionDto Map(SqlDataReader r) => new()
    {
        PlantillaId       = r.GetInt32OrDefault("PlantillaId"),
        TipoEvento        = r.GetNullableString("TipoEvento") ?? string.Empty,
        Canal             = r.GetNullableString("Canal") ?? string.Empty,
        Asunto            = r.GetNullableString("Asunto"),
        Cuerpo            = r.GetNullableString("Cuerpo") ?? string.Empty,
        VariablesJSON     = r.GetNullableString("VariablesJSON"),
        Activo            = r.GetBooleanOrDefault("Activo"),
        FechaCreacion     = r.GetDateTimeOrDefault("FechaCreacion"),
        FechaModificacion = r.GetNullableDateTime("FechaModificacion")
    };

    private async Task<SpEnv> ExecAsync(SqlCommand cmd, CancellationToken ct)
    {
        try
        {
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return SpEnv.Sin();
            return new SpEnv
            {
                HttpStatus = r.GetInt32OrDefault("HttpStatus", 500),
                Code       = r.GetNullableString("Codigo") ?? "SP_SIN_CODIGO",
                Message    = r.GetNullableString("Mensaje") ?? string.Empty,
                IntId      = r.GetNullableInt32("IdGenerado") ?? r.GetNullableInt32("IdActualizado")
            };
        }
        catch (Exception ex) { _logger.LogError(ex, "{SP}", cmd.CommandText); return SpEnv.Error(ex.Message); }
    }

    private static SqlCommand Sp(SqlConnection c, string n) =>
        new() { Connection = c, CommandType = CommandType.StoredProcedure, CommandText = n, CommandTimeout = 60 };
    private static void P(SqlCommand cmd, string n, object? v) =>
        cmd.Parameters.Add(new SqlParameter(n, v ?? DBNull.Value));
    private static ServiceOperationResult<T> Fail<T>(SpEnv e) =>
        new() { HttpStatus = e.HttpStatus, Code = e.Code, Message = e.Message };
    private static ServiceOperationResult<T> Ok<T>(SpEnv e, T? d) =>
        new() { HttpStatus = e.HttpStatus, Code = e.Code, Message = e.Message, Data = d };

    private sealed class SpEnv
    {
        public int HttpStatus { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public int? IntId { get; init; }
        public bool IsOk => HttpStatus is >= 200 and < 300;
        public static SpEnv Sin() => new() { HttpStatus = 500, Code = "SP_SIN_RESPUESTA", Message = "Sin resultado." };
        public static SpEnv Error(string m) => new() { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = m };
    }
}

// =============================================================================
// ColaNotificacionesService
// SPs:
//   sp_ColaNotificacion_Encolar → @PacienteId, @UsuarioId, @TipoEvento, @Canal,
//                                  @Destinatario, @Asunto, @Cuerpo,
//                                  @FechaProgramada, @MaxIntentos, @MetadatosJSON
//                                  devuelve: NotificacionId
//   sp_ColaNotificacion_ListarPendientes → @Canal, @MaxRegistros
//   sp_ProcesarColaNotificaciones → sin parámetros, devuelve Registros
// =============================================================================
public sealed class ColaNotificacionesService : IColaNotificacionesService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<ColaNotificacionesService> _logger;

    public ColaNotificacionesService(DatabaseConnection db, ILogger<ColaNotificacionesService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<object>> EncolarAsync(
        EncolarNotificacionRequestDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_ColaNotificacion_Encolar");
        P(cmd, "@PacienteId",      req.PacienteId);
        P(cmd, "@UsuarioId",       req.UsuarioId);
        P(cmd, "@TipoEvento",      req.TipoEvento);
        P(cmd, "@Canal",           req.Canal);
        P(cmd, "@Destinatario",    req.Destinatario);
        P(cmd, "@Asunto",          req.Asunto);
        P(cmd, "@Cuerpo",          req.Cuerpo);
        P(cmd, "@FechaProgramada", req.FechaProgramada);
        P(cmd, "@MaxIntentos",     req.MaxIntentos);
        P(cmd, "@MetadatosJSON",   req.MetadatosJSON);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return Err("SP_SIN_RESPUESTA", "El SP no devolvió resultado.");

            var status = reader.GetInt32OrDefault("HttpStatus", 500);
            var code   = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO";
            var msg    = reader.GetNullableString("Mensaje") ?? string.Empty;

            return new ServiceOperationResult<object>
                { HttpStatus = status, Code = code, Message = msg };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_ColaNotificacion_Encolar");
            return Err("ERROR_INFRAESTRUCTURA", ex.Message);
        }
    }

    public async Task<ServiceOperationResult<IReadOnlyList<NotificacionPendienteDto>>> ListarPendientesAsync(
        ColaListarFiltrosDto filtros, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_ColaNotificacion_ListarPendientes");
        P(cmd, "@Canal",        filtros.Canal);
        P(cmd, "@MaxRegistros", filtros.MaxRegistros);

        var list = new List<NotificacionPendienteDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new NotificacionPendienteDto
            {
                NotificacionId  = reader.GetInt64OrDefault("NotificacionId"),
                PacienteId      = reader.GetNullableInt32("PacienteId"),
                TipoEvento      = reader.GetNullableString("TipoEvento") ?? string.Empty,
                Canal           = reader.GetNullableString("Canal") ?? string.Empty,
                Destinatario    = reader.GetNullableString("Destinatario") ?? string.Empty,
                Asunto          = reader.GetNullableString("Asunto"),
                Estado          = reader.GetNullableString("Estado") ?? string.Empty,
                Intentos        = (byte)reader.GetInt32OrDefault("Intentos"),
                MaxIntentos     = (byte)reader.GetInt32OrDefault("MaxIntentos"),
                FechaProgramada = reader.GetDateTimeOrDefault("FechaProgramada"),
                FechaCreacion   = reader.GetDateTimeOrDefault("FechaCreacion")
            });
        }

        return new ServiceOperationResult<IReadOnlyList<NotificacionPendienteDto>>
        {
            HttpStatus = 200,
            Code = "PENDIENTES_LISTADOS",
            Message = list.Count == 0 ? "Sin pendientes." : $"{list.Count} notificación(es) pendiente(s).",
            Data = list
        };
    }

    public async Task<ServiceOperationResult<ProcesarColaResultDto>> ProcesarColaAsync(CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_ProcesarColaNotificaciones");

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return new ServiceOperationResult<ProcesarColaResultDto>
                    { HttpStatus = 500, Code = "SP_SIN_RESPUESTA", Message = "Sin resultado." };

            return new ServiceOperationResult<ProcesarColaResultDto>
            {
                HttpStatus = reader.GetInt32OrDefault("HttpStatus", 500),
                Code       = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO",
                Message    = reader.GetNullableString("Mensaje") ?? string.Empty,
                Data       = new ProcesarColaResultDto
                {
                    RegistrosProcesados = reader.GetNullableInt32("Registros") ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_ProcesarColaNotificaciones");
            return new ServiceOperationResult<ProcesarColaResultDto>
                { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = ex.Message };
        }
    }

    private static SqlCommand Sp(SqlConnection c, string n) =>
        new() { Connection = c, CommandType = CommandType.StoredProcedure, CommandText = n, CommandTimeout = 60 };
    private static void P(SqlCommand cmd, string n, object? v) =>
        cmd.Parameters.Add(new SqlParameter(n, v ?? DBNull.Value));
    private static ServiceOperationResult<object> Err(string code, string msg) =>
        new() { HttpStatus = 500, Code = code, Message = msg };
}
