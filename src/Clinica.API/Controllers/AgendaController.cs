using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/agenda")]
[AllowAnonymous]
public sealed class AgendaController : ControllerBase
{
    private readonly SqlExecutor _sql;

    public AgendaController(SqlExecutor sql)
    {
        _sql = sql;
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(
        [FromQuery] string tipo,
        [FromQuery] int? sedeId = null,
        [FromQuery] int? especialidadId = null,
        [FromQuery] int? servicioId = null,
        [FromQuery] string? busqueda = null,
        CancellationToken ct = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_Lookup", new[]
        {
            P("@Tipo", tipo),
            P("@SedeId", sedeId),
            P("@EspecialidadId", especialidadId),
            P("@ServicioId", servicioId),
            P("@Busqueda", busqueda)
        }, r => new
        {
            id = I(r, "Id"),
            label = S(r, "Label") ?? S(r, "Nombre") ?? string.Empty,
            nombre = S(r, "Nombre"),
            sedeId = NI(r, "SedeId"),
            sedeNombre = S(r, "SedeNombre"),
            especialidadId = NI(r, "EspecialidadId"),
            especialidadNombre = S(r, "EspecialidadNombre"),
            servicioId = NI(r, "ServicioId"),
            servicioNombre = S(r, "ServicioNombre"),
            consultorioId = NI(r, "ConsultorioId"),
            medicoId = NI(r, "MedicoId"),
            usuarioId = NI(r, "UsuarioId"),
            correoElectronico = S(r, "CorreoElectronico"),
            tipoEstacion = S(r, "TipoEstacion"),
            estado = S(r, "Estado"),
            activo = B(r, "Activo", true)
        }, ct);

        return Ok(new { ok = true, code = "OK", message = "Catálogo cargado.", data = rows });
    }

    [HttpGet("disponibilidad")]
    public async Task<IActionResult> Disponibilidad(
        [FromQuery] int sedeId,
        [FromQuery] DateTime fecha,
        [FromQuery] int? servicioId = null,
        [FromQuery] int? especialidadId = null,
        [FromQuery] int? medicoId = null,
        [FromQuery] bool soloDisponibles = true,
        CancellationToken ct = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_Agenda_Disponibilidad_Listar", new[]
        {
            P("@SedeId", sedeId),
            P("@Fecha", fecha.Date),
            P("@ServicioId", servicioId),
            P("@EspecialidadId", especialidadId),
            P("@MedicoId", medicoId),
            P("@SoloDisponibles", soloDisponibles)
        }, r => new
        {
            sedeId = I(r, "SedeId"),
            sedeNombre = S(r, "SedeNombre"),
            servicioId = I(r, "ServicioId"),
            servicioNombre = S(r, "ServicioNombre"),
            especialidadId = I(r, "EspecialidadId"),
            especialidadNombre = S(r, "EspecialidadNombre"),
            medicoId = I(r, "MedicoId"),
            medicoNombre = S(r, "MedicoNombre"),
            consultorioId = I(r, "ConsultorioId"),
            consultorioNombre = S(r, "ConsultorioNombre"),
            fechaInicio = D(r, "FechaInicio"),
            fechaFin = D(r, "FechaFin"),
            horaInicio = ST(r, "HoraInicio"),
            horaFin = ST(r, "HoraFin"),
            duracionSlotMinutos = I(r, "DuracionSlotMinutos"),
            capacidadPorSlot = I(r, "CapacidadPorSlot"),
            citasTomadas = I(r, "CitasTomadas"),
            disponible = B(r, "Disponible"),
            estadoSlot = S(r, "EstadoSlot"),
            motivo = S(r, "Motivo")
        }, ct);

        return Ok(new { ok = true, code = "OK", message = "Disponibilidad cargada.", data = rows });
    }

    private static SqlParameter P(string name, object? value) => new(name, value is null ? DBNull.Value : value);
    private static bool Has(Microsoft.Data.SqlClient.SqlDataReader r, string column) { for (var i = 0; i < r.FieldCount; i++) if (string.Equals(r.GetName(i), column, StringComparison.OrdinalIgnoreCase)) return true; return false; }
    private static object? V(Microsoft.Data.SqlClient.SqlDataReader r, string column) { if (!Has(r, column)) return null; var o = r.GetOrdinal(column); return r.IsDBNull(o) ? null : r.GetValue(o); }
    private static string? S(Microsoft.Data.SqlClient.SqlDataReader r, string column) => V(r, column)?.ToString();
    private static int I(Microsoft.Data.SqlClient.SqlDataReader r, string column, int def = 0) => V(r, column) is { } v ? Convert.ToInt32(v) : def;
    private static int? NI(Microsoft.Data.SqlClient.SqlDataReader r, string column) => V(r, column) is { } v ? Convert.ToInt32(v) : null;
    private static bool B(Microsoft.Data.SqlClient.SqlDataReader r, string column, bool def = false) => V(r, column) is { } v ? Convert.ToBoolean(v) : def;
    private static DateTime? D(Microsoft.Data.SqlClient.SqlDataReader r, string column) => V(r, column) is { } v ? Convert.ToDateTime(v) : null;
    private static string? ST(Microsoft.Data.SqlClient.SqlDataReader r, string column) { var v = V(r, column); return v is TimeSpan ts ? ts.ToString(@"hh\:mm") : v?.ToString(); }
}
