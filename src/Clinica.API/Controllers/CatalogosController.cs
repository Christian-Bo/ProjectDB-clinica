using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Administrador,Supervisor")]
public sealed class CatalogosController : ControllerBase
{
    private readonly SqlExecutor _sql;
    public CatalogosController(SqlExecutor sql) => _sql = sql;

    [HttpGet("sedes")]
    public async Task<IActionResult> ListarSedes()
    {
        var rows = await _sql.QueryAsync("dbo.sp_Sede_Listar", Array.Empty<SqlParameter>(),
            r => new {
                sedeId       = r.IsDBNull(0) ? 0    : r.GetInt32(0),
                nombre       = r.IsDBNull(1) ? ""   : r.GetString(1),
                direccion    = r.IsDBNull(2) ? null : (string?)r.GetString(2),
                telefono     = r.IsDBNull(3) ? null : (string?)r.GetString(3),
                municipio    = r.IsDBNull(4) ? null : (string?)r.GetString(4),
                departamento = r.IsDBNull(5) ? null : (string?)r.GetString(5),
                activo       = r.IsDBNull(6) ? true : r.GetBoolean(6),
            });
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("sedes")]
    public async Task<IActionResult> UpsertSede([FromBody] SedeRequest req)
    {
        var rows = await _sql.QueryAsync("dbo.sp_Sede_Upsert",
            new[] {
                new SqlParameter("@SedeId",      (object?)req.SedeId      ?? DBNull.Value),
                new SqlParameter("@Nombre",      req.Nombre),
                new SqlParameter("@Direccion",   (object?)req.Direccion   ?? DBNull.Value),
                new SqlParameter("@Telefono",    (object?)req.Telefono    ?? DBNull.Value),
                new SqlParameter("@MunicipioId", (object?)req.MunicipioId ?? DBNull.Value),
                new SqlParameter("@Activo",      req.Activo ?? true),
            },
            r => new {
                sedeId = r.IsDBNull(0) ? 0  : r.GetInt32(0),
                nombre = r.IsDBNull(1) ? "" : r.GetString(1),
            });
        return Ok(new { success = true, data = rows.FirstOrDefault() });
    }

    [HttpGet("especialidades")]
    public async Task<IActionResult> ListarEspecialidades()
    {
        var rows = await _sql.QueryAsync("dbo.sp_Especialidad_Listar", Array.Empty<SqlParameter>(),
            r => new {
                especialidadId = r.IsDBNull(0) ? 0    : r.GetInt32(0),
                nombre         = r.IsDBNull(1) ? ""   : r.GetString(1),
                descripcion    = r.IsDBNull(2) ? null : (string?)r.GetString(2),
                activo         = r.IsDBNull(3) ? true : r.GetBoolean(3),
            });
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("especialidades")]
    public async Task<IActionResult> UpsertEspecialidad([FromBody] EspecialidadRequest req)
    {
        var rows = await _sql.QueryAsync("dbo.sp_Especialidad_Upsert",
            new[] {
                new SqlParameter("@EspecialidadId", (object?)req.EspecialidadId ?? DBNull.Value),
                new SqlParameter("@Nombre",         req.Nombre),
                new SqlParameter("@Descripcion",    (object?)req.Descripcion    ?? DBNull.Value),
                new SqlParameter("@Activo",         req.Activo ?? true),
            },
            r => new {
                especialidadId = r.IsDBNull(0) ? 0  : r.GetInt32(0),
                nombre         = r.IsDBNull(1) ? "" : r.GetString(1),
            });
        return Ok(new { success = true, data = rows.FirstOrDefault() });
    }

    [HttpGet("servicios")]
    public async Task<IActionResult> ListarServicios()
    {
        var rows = await _sql.QueryAsync("dbo.sp_Servicio_Listar", Array.Empty<SqlParameter>(),
            r => new {
                servicioId         = r.IsDBNull(0) ? 0    : r.GetInt32(0),
                nombre             = r.IsDBNull(1) ? ""   : r.GetString(1),
                sedeNombre         = r.IsDBNull(2) ? null : (string?)r.GetString(2),
                especialidadNombre = r.IsDBNull(3) ? null : (string?)r.GetString(3),
                activo             = r.IsDBNull(4) ? true : r.GetBoolean(4),
            });
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("servicios")]
    public async Task<IActionResult> UpsertServicio([FromBody] ServicioRequest req)
    {
        var rows = await _sql.QueryAsync("dbo.sp_Servicio_Upsert",
            new[] {
                new SqlParameter("@ServicioId",     (object?)req.ServicioId     ?? DBNull.Value),
                new SqlParameter("@Nombre",         req.Nombre),
                new SqlParameter("@SedeId",         (object?)req.SedeId         ?? DBNull.Value),
                new SqlParameter("@EspecialidadId", (object?)req.EspecialidadId ?? DBNull.Value),
                new SqlParameter("@Activo",         req.Activo ?? true),
            },
            r => new {
                servicioId = r.IsDBNull(0) ? 0  : r.GetInt32(0),
                nombre     = r.IsDBNull(1) ? "" : r.GetString(1),
            });
        return Ok(new { success = true, data = rows.FirstOrDefault() });
    }

    [HttpGet("tipos-consulta")]
    public async Task<IActionResult> ListarTiposConsulta()
    {
        var rows = await _sql.QueryAsync("dbo.sp_TipoConsulta_Listar", Array.Empty<SqlParameter>(),
            r => new {
                tipoConsultaId = r.IsDBNull(0) ? 0    : r.GetInt32(0),
                nombre         = r.IsDBNull(1) ? ""   : r.GetString(1),
                descripcion    = r.IsDBNull(2) ? null : (string?)r.GetString(2),
                activo         = r.IsDBNull(3) ? true : r.GetBoolean(3),
            });
        return Ok(new { success = true, data = rows });
    }

    [HttpGet("feriados")]
    public async Task<IActionResult> ListarFeriados([FromQuery] int? anio = null)
    {
        var rows = await _sql.QueryAsync("dbo.sp_Feriado_Listar",
            new[] { new SqlParameter("@Anio", (object?)anio ?? DBNull.Value) },
            r => new {
                feriadoId = r.IsDBNull(0) ? 0    : r.GetInt32(0),
                nombre    = r.IsDBNull(1) ? ""   : r.GetString(1),
                fecha     = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2),
                diaSemana = r.IsDBNull(3) ? null : (string?)r.GetString(3),
                activo    = r.IsDBNull(4) ? true : r.GetBoolean(4),
            });
        return Ok(new { success = true, data = rows });
    }

    [HttpGet("consultorios")]
    public async Task<IActionResult> ListarConsultorios([FromQuery] int? sedeId = null)
    {
        var rows = await _sql.QueryAsync("dbo.sp_Consultorio_Listar",
            new[] { new SqlParameter("@SedeId", (object?)sedeId ?? DBNull.Value) },
            r => new {
                consultorioId = r.IsDBNull(0) ? 0    : r.GetInt32(0),
                nombre        = r.IsDBNull(1) ? ""   : r.GetString(1),
                piso          = r.IsDBNull(2) ? null : (string?)r.GetString(2),
                descripcion   = r.IsDBNull(3) ? null : (string?)r.GetString(3),
                sedeId        = r.IsDBNull(4) ? 0    : r.GetInt32(4),
                sedeNombre    = r.IsDBNull(5) ? null : (string?)r.GetString(5),
                activo        = r.IsDBNull(6) ? true : r.GetBoolean(6),
            });
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("consultorios")]
    public async Task<IActionResult> UpsertConsultorio([FromBody] ConsultorioRequest req)
    {
        var rows = await _sql.QueryAsync("dbo.sp_Consultorio_Upsert",
            new[] {
                new SqlParameter("@ConsultorioId", (object?)req.ConsultorioId ?? DBNull.Value),
                new SqlParameter("@Nombre",        req.Nombre),
                new SqlParameter("@Piso",          (object?)req.Piso          ?? DBNull.Value),
                new SqlParameter("@Descripcion",   (object?)req.Descripcion   ?? DBNull.Value),
                new SqlParameter("@SedeId",        (object?)req.SedeId        ?? DBNull.Value),
                new SqlParameter("@Activo",        req.Activo ?? true),
            },
            r => new {
                consultorioId = r.IsDBNull(0) ? 0  : r.GetInt32(0),
                nombre        = r.IsDBNull(1) ? "" : r.GetString(1),
            });
        return Ok(new { success = true, data = rows.FirstOrDefault() });
    }

    [HttpGet("estaciones")]
    public async Task<IActionResult> ListarEstaciones([FromQuery] int? sedeId = null)
    {
        var rows = await _sql.QueryAsync("dbo.sp_EstacionAtencion_Listar",
            new[] { new SqlParameter("@SedeId", (object?)sedeId ?? DBNull.Value) },
            r => new {
                estacionId = r.IsDBNull(0) ? 0    : r.GetInt32(0),
                nombre     = r.IsDBNull(1) ? ""   : r.GetString(1),
                codigo     = r.IsDBNull(2) ? null : (string?)r.GetString(2),
                sedeId     = r.IsDBNull(3) ? 0    : r.GetInt32(3),
                sedeNombre = r.IsDBNull(4) ? null : (string?)r.GetString(4),
                activo     = r.IsDBNull(5) ? true : r.GetBoolean(5),
            });
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("estaciones")]
    public async Task<IActionResult> UpsertEstacion([FromBody] EstacionRequest req)
    {
        var rows = await _sql.QueryAsync("dbo.sp_EstacionAtencion_Upsert",
            new[] {
                new SqlParameter("@EstacionAtencionId", (object?)req.EstacionId ?? DBNull.Value),
                new SqlParameter("@Nombre",             req.Nombre),
                new SqlParameter("@Codigo",             (object?)req.Codigo     ?? DBNull.Value),
                new SqlParameter("@SedeId",             (object?)req.SedeId     ?? DBNull.Value),
                new SqlParameter("@Activo",             req.Activo ?? true),
            },
            r => new {
                estacionId = r.IsDBNull(0) ? 0  : r.GetInt32(0),
                nombre     = r.IsDBNull(1) ? "" : r.GetString(1),
            });
        return Ok(new { success = true, data = rows.FirstOrDefault() });
    }

    [HttpGet("horarios")]
    public async Task<IActionResult> ListarHorarios([FromQuery] int? sedeId = null)
    {
        var rows = await _sql.QueryAsync("dbo.sp_HorarioAtencion_Listar",
            new[] { new SqlParameter("@SedeId", (object?)sedeId ?? DBNull.Value) },
            r => new {
                horarioId  = r.IsDBNull(0) ? 0    : r.GetInt32(0),
                diaSemana  = r.IsDBNull(1) ? 0    : r.GetInt32(1),
                diaNombre  = r.IsDBNull(2) ? null : (string?)r.GetString(2),
                horaInicio = r.IsDBNull(3) ? null : (string?)r.GetString(3),
                horaFin    = r.IsDBNull(4) ? null : (string?)r.GetString(4),
                sedeId     = r.IsDBNull(5) ? 0    : r.GetInt32(5),
                sedeNombre = r.IsDBNull(6) ? null : (string?)r.GetString(6),
                activo     = r.IsDBNull(7) ? true : r.GetBoolean(7),
            });
        return Ok(new { success = true, data = rows });
    }

    [HttpPost("horarios")]
    public async Task<IActionResult> UpsertHorario([FromBody] HorarioRequest req)
    {
        var rows = await _sql.QueryAsync("dbo.sp_HorarioAtencion_Upsert",
            new[] {
                new SqlParameter("@HorarioAtencionId", (object?)req.HorarioId  ?? DBNull.Value),
                new SqlParameter("@DiaSemana",         req.DiaSemana),
                new SqlParameter("@HoraInicio",        req.HoraInicio),
                new SqlParameter("@HoraFin",           req.HoraFin),
                new SqlParameter("@SedeId",            (object?)req.SedeId     ?? DBNull.Value),
                new SqlParameter("@Activo",            req.Activo ?? true),
            },
            r => new {
                horarioId = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                diaSemana = r.IsDBNull(1) ? 0 : r.GetInt32(1),
            });
        return Ok(new { success = true, data = rows.FirstOrDefault() });
    }
}

public sealed record SedeRequest(int? SedeId, string Nombre, string? Direccion, string? Telefono, int? MunicipioId, bool? Activo);
public sealed record EspecialidadRequest(int? EspecialidadId, string Nombre, string? Descripcion, bool? Activo);
public sealed record ServicioRequest(int? ServicioId, string Nombre, int? SedeId, int? EspecialidadId, bool? Activo);
public sealed record ConsultorioRequest(int? ConsultorioId, string Nombre, string? Piso, string? Descripcion, int? SedeId, bool? Activo);
public sealed record EstacionRequest(int? EstacionId, string Nombre, string? Codigo, int? SedeId, bool? Activo);
public sealed record HorarioRequest(int? HorarioId, int DiaSemana, string HoraInicio, string HoraFin, int? SedeId, bool? Activo);