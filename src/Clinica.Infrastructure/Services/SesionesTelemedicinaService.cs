using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Telemedicina;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// =============================================================================
// SesionesTelemedicinaService
// SPs:
//   sp_SesionTelemedica_Upsert → @SesionTeleId, @CitaId, @ConsultaId,
//                                  @PlataformaVideoId, @UrlSala, @CodigoSala,
//                                  @PasswordSala, @Estado, @FechaInicioReal,
//                                  @FechaFinReal, @GrabacionUrl, @NotasSesion,
//                                  @TokenMedico, @TokenPaciente, @TokenExpiracion
//                                  devuelve: SesionTeleId
//   sp_SesionTelemedica_Obtener → @SesionTeleId (BIGINT), @CitaId (BIGINT)
//   sp_SesionTelemedica_Listar  → @Estado, @FechaDesde, @FechaHasta
//
// ATENCIÓN: El PK es SesionTeleId (no SesionId).
//           CodigoSala es UNIQUE en dbo.SesionesTelemedicas.
// =============================================================================
public sealed class SesionesTelemedicinaService : ISesionesTelemedicinaService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<SesionesTelemedicinaService> _logger;

    public SesionesTelemedicinaService(DatabaseConnection db, ILogger<SesionesTelemedicinaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<SesionTelemedicaDto>> UpsertSesionAsync(
        SesionTelemedicaUpsertDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_SesionTelemedica_Upsert");
        P(cmd, "@SesionTeleId",      req.SesionTeleId);
        P(cmd, "@CitaId",            req.CitaId);
        P(cmd, "@ConsultaId",        req.ConsultaId);
        P(cmd, "@PlataformaVideoId", req.PlataformaVideoId);
        P(cmd, "@UrlSala",           req.UrlSala);
        P(cmd, "@CodigoSala",        req.CodigoSala);
        P(cmd, "@PasswordSala",      req.PasswordSala);
        P(cmd, "@Estado",            req.Estado);
        P(cmd, "@FechaInicioReal",   req.FechaInicioReal);
        P(cmd, "@FechaFinReal",      req.FechaFinReal);
        P(cmd, "@GrabacionUrl",      req.GrabacionUrl);
        P(cmd, "@NotasSesion",       req.NotasSesion);
        P(cmd, "@TokenMedico",       req.TokenMedico);
        P(cmd, "@TokenPaciente",     req.TokenPaciente);
        P(cmd, "@TokenExpiracion",   req.TokenExpiracion);

        var env = await ExecAsync(cmd, ct);
        if (!env.IsOk) return Fail<SesionTelemedicaDto>(env);

        var id = env.LongId ?? req.SesionTeleId;
        if (!id.HasValue) return Ok<SesionTelemedicaDto>(env, null);

        var dto = await CargarAsync(conn, id.Value, null, ct);
        return Ok(env, dto);
    }

    public async Task<ServiceOperationResult<SesionTelemedicaDto>> ObtenerSesionAsync(
        long? sesionTeleId, long? citaId, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        var dto = await CargarAsync(conn, sesionTeleId, citaId, ct);
        if (dto is null)
            return new ServiceOperationResult<SesionTelemedicaDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "SESION_NO_ENCONTRADA",
                Message = "No se encontró la sesión de telemedicina."
            };

        return new ServiceOperationResult<SesionTelemedicaDto>
            { HttpStatus = 200, Code = "SESION_OBTENIDA", Message = "OK.", Data = dto };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<SesionTelemedicaDto>>> ListarSesionesAsync(
        SesionListarFiltrosDto filtros, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_SesionTelemedica_Listar");
        P(cmd, "@Estado",     filtros.Estado);
        P(cmd, "@FechaDesde", filtros.FechaDesde);
        P(cmd, "@FechaHasta", filtros.FechaHasta);

        var list = new List<SesionTelemedicaDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return new ServiceOperationResult<IReadOnlyList<SesionTelemedicaDto>>
        {
            HttpStatus = 200,
            Code = "SESIONES_LISTADAS",
            Message = list.Count == 0 ? "No se encontraron sesiones." : $"{list.Count} sesión(es).",
            Data = list
        };
    }

    private async Task<SesionTelemedicaDto?> CargarAsync(
        SqlConnection conn, long? sesionTeleId, long? citaId, CancellationToken ct)
    {
        await using var cmd = Sp(conn, "dbo.sp_SesionTelemedica_Obtener");
        P(cmd, "@SesionTeleId", sesionTeleId);
        P(cmd, "@CitaId",       citaId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    private static SesionTelemedicaDto Map(SqlDataReader r) => new()
    {
        SesionTeleId      = r.GetInt64OrDefault("SesionTeleId"),
        CitaId            = r.GetInt64OrDefault("CitaId"),
        ConsultaId        = r.GetNullableInt64("ConsultaId"),
        PlataformaVideoId = r.GetNullableInt32("PlataformaVideoId"),
        PlataformaVideoNombre = r.GetNullableString("PlataformaVideoNombre"),
        PacienteId        = r.GetNullableInt32("PacienteId"),
        PacienteNombre    = r.GetNullableString("PacienteNombre"),
        MedicoId          = r.GetNullableInt32("MedicoId"),
        MedicoNombre      = r.GetNullableString("MedicoNombre"),
        SedeId            = r.GetNullableInt32("SedeId"),
        SedeNombre        = r.GetNullableString("SedeNombre"),
        ServicioId        = r.GetNullableInt32("ServicioId"),
        ServicioNombre    = r.GetNullableString("ServicioNombre"),
        UrlSala           = r.GetNullableString("UrlSala") ?? string.Empty,
        CodigoSala        = r.GetNullableString("CodigoSala") ?? string.Empty,
        PasswordSala      = r.GetNullableString("PasswordSala"),
        Estado            = r.GetNullableString("Estado") ?? string.Empty,
        FechaCreacion     = r.GetDateTimeOrDefault("FechaCreacion"),
        FechaInicioReal   = r.GetNullableDateTime("FechaInicioReal"),
        FechaFinReal      = r.GetNullableDateTime("FechaFinReal"),
        DuracionMinutos   = r.GetNullableInt32("DuracionMinutos"),
        GrabacionUrl      = r.GetNullableString("GrabacionUrl"),
        NotasSesion       = r.GetNullableString("NotasSesion"),
        TokenMedico       = r.GetNullableString("TokenMedico"),
        TokenPaciente     = r.GetNullableString("TokenPaciente"),
        TokenExpiracion   = r.GetNullableDateTime("TokenExpiracion")
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
                LongId     = r.GetNullableInt64("SesionTeleId")
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
        public long? LongId { get; init; }
        public bool IsOk => HttpStatus is >= 200 and < 300;
        public static SpEnv Sin() => new() { HttpStatus = 500, Code = "SP_SIN_RESPUESTA", Message = "Sin resultado." };
        public static SpEnv Error(string m) => new() { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = m };
    }
}
