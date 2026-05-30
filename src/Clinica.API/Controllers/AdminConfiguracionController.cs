using System.Data;
using System.Security.Claims;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/admin/configuracion")]
[Authorize(Roles = "Administrador,Supervisor")]
public sealed class AdminConfiguracionController : ControllerBase
{
    private readonly SqlExecutor _sql;

    public AdminConfiguracionController(SqlExecutor sql)
    {
        _sql = sql;
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumen(CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_Resumen", Array.Empty<SqlParameter>(), r => new
        {
            totalSedes = I(r, "TotalSedes"),
            totalEspecialidades = I(r, "TotalEspecialidades"),
            totalServicios = I(r, "TotalServicios"),
            totalClinicas = I(r, "TotalClinicas"),
            totalVentanillas = I(r, "TotalVentanillas"),
            totalMedicos = I(r, "TotalMedicos"),
            totalSecretarias = I(r, "TotalSecretarias"),
            totalHorariosMedicos = I(r, "TotalHorariosMedicos"),
            totalClinicasAsignadas = I(r, "TotalClinicasAsignadas"),
            medicosSinHorario = I(r, "MedicosSinHorario"),
            ventanillasSinSecretaria = I(r, "VentanillasSinSecretaria"),
            ventanillasSinClinicas = I(r, "VentanillasSinClinicas"),
            clinicasSinMedico = I(r, "ClinicasSinMedico")
        }, ct);

        return OkEnvelope(rows.FirstOrDefault());
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> ObtenerLookup(
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
        }, MapLookup, ct);

        return OkEnvelope(rows);
    }

    [HttpGet("horarios-medicos")]
    public async Task<IActionResult> ListarHorariosMedicos(
        [FromQuery] int? sedeId = null,
        [FromQuery] int? medicoId = null,
        [FromQuery] int? consultorioId = null,
        [FromQuery] string? busqueda = null,
        [FromQuery] bool soloActivos = false,
        CancellationToken ct = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_HorariosMedicos_Listar", new[]
        {
            P("@SedeId", sedeId),
            P("@MedicoId", medicoId),
            P("@ConsultorioId", consultorioId),
            P("@Busqueda", busqueda),
            P("@SoloActivos", soloActivos)
        }, MapHorarioMedico, ct);

        return OkEnvelope(rows);
    }

    [HttpPost("horarios-medicos")]
    public async Task<IActionResult> GuardarHorarioMedico([FromBody] HorarioMedicoRequest req, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_HorarioMedico_Guardar", new[]
        {
            P("@MedicoHorarioId", req.MedicoHorarioId),
            P("@MedicoId", req.MedicoId),
            P("@SedeId", req.SedeId),
            P("@ConsultorioId", req.ConsultorioId),
            P("@DiaSemana", req.DiaSemana),
            P("@HoraInicio", req.HoraInicio),
            P("@HoraFin", req.HoraFin),
            P("@AlmuerzoInicio", req.AlmuerzoInicio),
            P("@AlmuerzoFin", req.AlmuerzoFin),
            P("@DuracionSlotMinutos", req.DuracionSlotMinutos),
            P("@CapacidadPorSlot", req.CapacidadPorSlot),
            P("@Activo", req.Activo),
            P("@UsuarioId", ResolveUserId())
        }, r => new { medicoHorarioId = I(r, "MedicoHorarioId"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Configuración de horario guardada.");
    }

    [HttpPatch("horarios-medicos/{id:int}/estado")]
    public async Task<IActionResult> CambiarEstadoHorario(int id, [FromBody] EstadoRequest req, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_HorarioMedico_CambiarEstado", new[]
        {
            P("@MedicoHorarioId", id),
            P("@Activo", req.Activo),
            P("@UsuarioId", ResolveUserId())
        }, r => new { medicoHorarioId = I(r, "MedicoHorarioId"), activo = B(r, "Activo"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault());
    }

    [HttpDelete("horarios-medicos/{id:int}")]
    public async Task<IActionResult> EliminarHorarioMedico(int id, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_HorarioMedico_Eliminar", new[]
        {
            P("@MedicoHorarioId", id),
            P("@UsuarioId", ResolveUserId())
        }, r => new { medicoHorarioId = I(r, "MedicoHorarioId"), activo = B(r, "Activo"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Horario desactivado.");
    }

    [HttpGet("secretarias-ventanillas")]
    public async Task<IActionResult> ListarSecretariasVentanillas(
        [FromQuery] int? sedeId = null,
        [FromQuery] int? usuarioId = null,
        [FromQuery] int? estacionId = null,
        [FromQuery] string? busqueda = null,
        [FromQuery] bool soloActivas = false,
        CancellationToken ct = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_SecretariaAsignaciones_Listar", new[]
        {
            P("@SedeId", sedeId),
            P("@UsuarioId", usuarioId),
            P("@EstacionId", estacionId),
            P("@Busqueda", busqueda),
            P("@SoloActivas", soloActivas)
        }, r => new
        {
            secretariaAsignacionId = I(r, "SecretariaAsignacionId"),
            secretariaUsuarioId = I(r, "SecretariaUsuarioId"),
            secretariaNombre = S(r, "SecretariaNombre"),
            secretariaCorreo = S(r, "SecretariaCorreo"),
            sedeId = I(r, "SedeId"),
            sedeNombre = S(r, "SedeNombre"),
            estacionId = I(r, "EstacionId"),
            ventanillaNombre = S(r, "VentanillaNombre"),
            servicioId = NI(r, "ServicioId"),
            servicioNombre = S(r, "ServicioNombre"),
            rolOperativo = S(r, "RolOperativo"),
            activo = B(r, "Activo"),
            esPrincipal = B(r, "EsPrincipal"),
            fechaInicio = ND(r, "FechaInicio"),
            fechaFin = ND(r, "FechaFin")
        }, ct);

        return OkEnvelope(rows);
    }

    [HttpPost("secretarias-ventanillas")]
    public async Task<IActionResult> GuardarSecretariaVentanilla([FromBody] SecretariaVentanillaRequest req, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_SecretariaAsignacion_Guardar", new[]
        {
            P("@SecretariaAsignacionId", req.SecretariaAsignacionId),
            P("@UsuarioId", req.UsuarioId),
            P("@SedeId", req.SedeId),
            P("@EstacionId", req.EstacionId),
            P("@ServicioId", req.ServicioId),
            P("@RolOperativo", req.RolOperativo),
            P("@Activo", req.Activo),
            P("@EsPrincipal", req.EsPrincipal),
            P("@AdminUsuarioId", ResolveUserId())
        }, r => new { secretariaAsignacionId = I(r, "SecretariaAsignacionId"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Asignación de secretaria guardada.");
    }

    [HttpPatch("secretarias-ventanillas/{id:int}/estado")]
    public async Task<IActionResult> CambiarEstadoSecretaria(int id, [FromBody] EstadoRequest req, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_SecretariaAsignacion_CambiarEstado", new[]
        {
            P("@SecretariaAsignacionId", id),
            P("@Activo", req.Activo),
            P("@UsuarioId", ResolveUserId())
        }, r => new { secretariaAsignacionId = I(r, "SecretariaAsignacionId"), activo = B(r, "Activo"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault());
    }

    [HttpDelete("secretarias-ventanillas/{id:int}")]
    public async Task<IActionResult> EliminarSecretariaVentanilla(int id, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_SecretariaAsignacion_Eliminar", new[]
        {
            P("@SecretariaAsignacionId", id),
            P("@UsuarioId", ResolveUserId())
        }, r => new { secretariaAsignacionId = I(r, "SecretariaAsignacionId"), activo = B(r, "Activo"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Asignación desactivada.");
    }

    [HttpGet("ventanilla-clinicas")]
    public async Task<IActionResult> ListarVentanillaClinicas(
        [FromQuery] int? sedeId = null,
        [FromQuery] int? estacionId = null,
        [FromQuery] int? servicioId = null,
        [FromQuery] int? especialidadId = null,
        [FromQuery] string? busqueda = null,
        [FromQuery] bool soloActivas = false,
        CancellationToken ct = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_VentanillaClinicas_Listar", new[]
        {
            P("@SedeId", sedeId),
            P("@EstacionId", estacionId),
            P("@ServicioId", servicioId),
            P("@EspecialidadId", especialidadId),
            P("@Busqueda", busqueda),
            P("@SoloActivas", soloActivas)
        }, r => new
        {
            ventanillaClinicaId = I(r, "VentanillaClinicaId"),
            estacionId = I(r, "EstacionId"),
            ventanillaNombre = S(r, "VentanillaNombre"),
            sedeId = I(r, "SedeId"),
            sedeNombre = S(r, "SedeNombre"),
            consultorioId = I(r, "ConsultorioId"),
            consultorioNombre = S(r, "ConsultorioNombre"),
            servicioId = I(r, "ServicioId"),
            servicioNombre = S(r, "ServicioNombre"),
            especialidadId = I(r, "EspecialidadId"),
            especialidadNombre = S(r, "EspecialidadNombre"),
            medicoId = NI(r, "MedicoId"),
            medicoNombre = S(r, "MedicoNombre"),
            secretariaUsuarioId = NI(r, "SecretariaUsuarioId"),
            secretariaNombre = S(r, "SecretariaNombre"),
            activo = B(r, "Activo")
        }, ct);

        return OkEnvelope(rows);
    }

    [HttpPost("ventanilla-clinicas")]
    public async Task<IActionResult> GuardarVentanillaClinica([FromBody] VentanillaClinicaRequest req, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_VentanillaClinica_Guardar", new[]
        {
            P("@VentanillaClinicaId", req.VentanillaClinicaId),
            P("@EstacionId", req.EstacionId),
            P("@SedeId", req.SedeId),
            P("@ConsultorioId", req.ConsultorioId),
            P("@ServicioId", req.ServicioId),
            P("@EspecialidadId", req.EspecialidadId),
            P("@MedicoId", req.MedicoId),
            P("@Activo", req.Activo),
            P("@UsuarioId", ResolveUserId())
        }, r => new { ventanillaClinicaId = I(r, "VentanillaClinicaId"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Relación ventanilla-clínica guardada.");
    }

    [HttpPatch("ventanilla-clinicas/{id:int}/estado")]
    public async Task<IActionResult> CambiarEstadoVentanillaClinica(int id, [FromBody] EstadoRequest req, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_VentanillaClinica_CambiarEstado", new[]
        {
            P("@VentanillaClinicaId", id),
            P("@Activo", req.Activo),
            P("@UsuarioId", ResolveUserId())
        }, r => new { ventanillaClinicaId = I(r, "VentanillaClinicaId"), activo = B(r, "Activo"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault());
    }

    [HttpDelete("ventanilla-clinicas/{id:int}")]
    public async Task<IActionResult> EliminarVentanillaClinica(int id, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_VentanillaClinica_Eliminar", new[]
        {
            P("@VentanillaClinicaId", id),
            P("@UsuarioId", ResolveUserId())
        }, r => new { ventanillaClinicaId = I(r, "VentanillaClinicaId"), activo = B(r, "Activo"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Relación desactivada.");
    }

    [HttpGet("medicos-clinicas")]
    public async Task<IActionResult> ListarMedicosClinicas(
        [FromQuery] int? sedeId = null,
        [FromQuery] int? medicoId = null,
        [FromQuery] int? especialidadId = null,
        [FromQuery] int? consultorioId = null,
        [FromQuery] string? busqueda = null,
        [FromQuery] bool soloActivos = false,
        CancellationToken ct = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_MedicoClinica_Listar", new[]
        {
            P("@SedeId", sedeId),
            P("@MedicoId", medicoId),
            P("@EspecialidadId", especialidadId),
            P("@ConsultorioId", consultorioId),
            P("@Busqueda", busqueda),
            P("@SoloActivos", soloActivos)
        }, r => new
        {
            medicoId = I(r, "MedicoId"),
            usuarioId = I(r, "UsuarioId"),
            medicoNombre = S(r, "MedicoNombre"),
            correoElectronico = S(r, "CorreoElectronico"),
            numeroColegiado = S(r, "NumeroColegiado"),
            especialidadId = NI(r, "EspecialidadId"),
            especialidadNombre = S(r, "EspecialidadNombre"),
            sedeId = NI(r, "SedeId"),
            sedeNombre = S(r, "SedeNombre"),
            consultorioId = NI(r, "ConsultorioId"),
            consultorioNombre = S(r, "ConsultorioNombre"),
            activo = B(r, "Activo")
        }, ct);

        return OkEnvelope(rows);
    }

    [HttpPost("medicos-clinicas")]
    public async Task<IActionResult> GuardarMedicoClinica([FromBody] MedicoClinicaRequest req, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_MedicoClinica_Guardar", new[]
        {
            P("@MedicoId", req.MedicoId),
            P("@SedeId", req.SedeId),
            P("@ConsultorioId", req.ConsultorioId),
            P("@EspecialidadId", req.EspecialidadId),
            P("@Activo", req.Activo),
            P("@UsuarioId", ResolveUserId())
        }, r => new { medicoId = I(r, "MedicoId"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Asignación de médico guardada.");
    }

    [HttpDelete("medicos-clinicas")]
    public async Task<IActionResult> EliminarMedicoClinica([FromQuery] int medicoId, [FromQuery] int sedeId, [FromQuery] int? especialidadId = null, CancellationToken ct = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_MedicoClinica_Eliminar", new[]
        {
            P("@MedicoId", medicoId),
            P("@SedeId", sedeId),
            P("@EspecialidadId", especialidadId),
            P("@UsuarioId", ResolveUserId())
        }, r => new { medicoId = I(r, "MedicoId"), sedeId = I(r, "SedeId"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Asignación médica desactivada.");
    }

    [HttpGet("parametros")]
    [HttpGet("reglas-operativas")]
    public async Task<IActionResult> ListarParametros([FromQuery] string? categoria = null, [FromQuery] string? busqueda = null, CancellationToken ct = default)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_Parametros_Listar", new[]
        {
            P("@Categoria", categoria),
            P("@Busqueda", busqueda)
        }, r => new
        {
            parametroId = I(r, "ParametroId"),
            categoria = S(r, "Categoria"),
            clave = S(r, "Clave"),
            valor = S(r, "Valor"),
            tipoDato = S(r, "TipoDato"),
            descripcion = S(r, "Descripcion"),
            editable = B(r, "Editable", true),
            fechaActualizacion = ND(r, "FechaActualizacion"),
            actualizadoPor = NI(r, "ActualizadoPor")
        }, ct);

        return OkEnvelope(rows);
    }

    [HttpPost("parametros")]
    [HttpPost("reglas-operativas")]
    public async Task<IActionResult> GuardarParametro([FromBody] ParametroRequest req, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_Parametro_Guardar", new[]
        {
            P("@Categoria", req.Categoria),
            P("@Clave", req.Clave),
            P("@Valor", req.Valor),
            P("@TipoDato", req.TipoDato),
            P("@Descripcion", req.Descripcion),
            P("@UsuarioId", ResolveUserId())
        }, r => new { parametroId = I(r, "ParametroId"), categoria = S(r, "Categoria"), clave = S(r, "Clave"), valor = S(r, "Valor"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Parámetro guardado.");
    }

    [HttpDelete("parametros/{id:int}")]
    [HttpDelete("reglas-operativas/{id:int}")]
    public async Task<IActionResult> EliminarParametro(int id, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_Parametro_Eliminar", new[]
        {
            P("@ParametroId", id),
            P("@UsuarioId", ResolveUserId())
        }, r => new { parametroId = I(r, "ParametroId"), editable = B(r, "Editable"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Parámetro desactivado.");
    }

    [HttpGet("notificaciones")]
    public async Task<IActionResult> ListarNotificaciones(CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_Notificaciones_Listar", Array.Empty<SqlParameter>(), r => new
        {
            configuracionId = I(r, "ConfiguracionId"),
            canal = S(r, "Canal"),
            activo = B(r, "Activo"),
            smtpHost = S(r, "SmtpHost"),
            smtpPuerto = NI(r, "SmtpPuerto"),
            smtpUsarSsl = NB(r, "SmtpUsarSsl"),
            smtpCorreoRemitente = S(r, "SmtpCorreoRemitente"),
            smtpNombreRemitente = S(r, "SmtpNombreRemitente"),
            smtpUsuario = S(r, "SmtpUsuario"),
            whatsAppEndpoint = S(r, "WhatsAppEndpoint"),
            whatsAppNumeroOrigen = S(r, "WhatsAppNumeroOrigen"),
            timeoutSegundos = I(r, "TimeoutSegundos", 30),
            fechaActualizacion = ND(r, "FechaActualizacion"),
            actualizadoPor = NI(r, "ActualizadoPor")
        }, ct);

        return OkEnvelope(rows);
    }

    [HttpPost("notificaciones")]
    public async Task<IActionResult> GuardarNotificacion([FromBody] NotificacionConfigRequest req, CancellationToken ct)
    {
        var rows = await _sql.QueryAsync("dbo.sp_AdminConfig_Notificacion_Guardar", new[]
        {
            P("@Canal", req.Canal),
            P("@Activo", req.Activo),
            P("@SmtpHost", req.SmtpHost),
            P("@SmtpPuerto", req.SmtpPuerto),
            P("@SmtpUsarSsl", req.SmtpUsarSsl),
            P("@SmtpCorreoRemitente", req.SmtpCorreoRemitente),
            P("@SmtpNombreRemitente", req.SmtpNombreRemitente),
            P("@SmtpUsuario", req.SmtpUsuario),
            P("@SmtpPassword", req.SmtpPassword),
            P("@WhatsAppEndpoint", req.WhatsAppEndpoint),
            P("@WhatsAppToken", req.WhatsAppToken),
            P("@WhatsAppNumeroOrigen", req.WhatsAppNumeroOrigen),
            P("@TimeoutSegundos", req.TimeoutSegundos),
            P("@UsuarioId", ResolveUserId())
        }, r => new { configuracionId = I(r, "ConfiguracionId"), canal = S(r, "Canal"), activo = B(r, "Activo"), mensaje = S(r, "Mensaje") }, ct);

        return OkEnvelope(rows.FirstOrDefault(), "Configuración de notificación guardada.");
    }

    private IActionResult OkEnvelope(object? data, string message = "Operación realizada correctamente.")
        => Ok(new { ok = true, code = "OK", message, data });

    private int? ResolveUserId()
    {
        var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("usuarioId")
            ?? User?.FindFirstValue("sub");
        return int.TryParse(raw, out var id) && id > 0 ? id : null;
    }

    private static object MapLookup(SqlDataReader r) => new
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
    };

    private static object MapHorarioMedico(SqlDataReader r) => new
    {
        medicoHorarioId = I(r, "MedicoHorarioId"),
        medicoId = I(r, "MedicoId"),
        medicoNombre = S(r, "MedicoNombre"),
        sedeId = I(r, "SedeId"),
        sedeNombre = S(r, "SedeNombre"),
        consultorioId = I(r, "ConsultorioId"),
        consultorioNombre = S(r, "ConsultorioNombre"),
        especialidadId = NI(r, "EspecialidadId"),
        especialidadNombre = S(r, "EspecialidadNombre"),
        diaSemana = I(r, "DiaSemana"),
        diaNombre = S(r, "DiaNombre"),
        horaInicio = ST(r, "HoraInicio"),
        horaFin = ST(r, "HoraFin"),
        almuerzoInicio = ST(r, "AlmuerzoInicio"),
        almuerzoFin = ST(r, "AlmuerzoFin"),
        duracionSlotMinutos = I(r, "DuracionSlotMinutos"),
        capacidadPorSlot = I(r, "CapacidadPorSlot"),
        activo = B(r, "Activo"),
        fechaCreacion = ND(r, "FechaCreacion"),
        fechaActualizacion = ND(r, "FechaActualizacion")
    };

    private static SqlParameter P(string name, object? value)
        => new(name, value is null ? DBNull.Value : value);

    private static bool Has(SqlDataReader r, string column)
    {
        for (var i = 0; i < r.FieldCount; i++)
        {
            if (string.Equals(r.GetName(i), column, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static object? V(SqlDataReader r, string column)
    {
        if (!Has(r, column)) return null;
        var ordinal = r.GetOrdinal(column);
        return r.IsDBNull(ordinal) ? null : r.GetValue(ordinal);
    }

    private static string? S(SqlDataReader r, string column) => V(r, column)?.ToString();
    private static int I(SqlDataReader r, string column, int defaultValue = 0) => V(r, column) is { } v ? Convert.ToInt32(v) : defaultValue;
    private static int? NI(SqlDataReader r, string column) => V(r, column) is { } v ? Convert.ToInt32(v) : null;
    private static bool B(SqlDataReader r, string column, bool defaultValue = false) => V(r, column) is { } v ? Convert.ToBoolean(v) : defaultValue;
    private static bool? NB(SqlDataReader r, string column) => V(r, column) is { } v ? Convert.ToBoolean(v) : null;
    private static DateTime? ND(SqlDataReader r, string column) => V(r, column) is { } v ? Convert.ToDateTime(v) : null;
    private static string? ST(SqlDataReader r, string column)
    {
        var v = V(r, column);
        if (v is null) return null;
        return v is TimeSpan ts ? ts.ToString(@"hh\:mm") : v.ToString();
    }
}

public sealed record EstadoRequest(bool Activo);

public sealed record HorarioMedicoRequest(
    int? MedicoHorarioId,
    int MedicoId,
    int SedeId,
    int ConsultorioId,
    int DiaSemana,
    string HoraInicio,
    string HoraFin,
    string? AlmuerzoInicio,
    string? AlmuerzoFin,
    int DuracionSlotMinutos,
    int CapacidadPorSlot,
    bool Activo
);

public sealed record SecretariaVentanillaRequest(
    int? SecretariaAsignacionId,
    int UsuarioId,
    int SedeId,
    int EstacionId,
    int? ServicioId,
    string? RolOperativo,
    bool Activo,
    bool EsPrincipal
);

public sealed record VentanillaClinicaRequest(
    int? VentanillaClinicaId,
    int EstacionId,
    int SedeId,
    int ConsultorioId,
    int ServicioId,
    int EspecialidadId,
    int? MedicoId,
    bool Activo
);

public sealed record MedicoClinicaRequest(
    int MedicoId,
    int SedeId,
    int ConsultorioId,
    int EspecialidadId,
    bool Activo
);

public sealed record ParametroRequest(
    string Categoria,
    string Clave,
    string Valor,
    string? TipoDato,
    string? Descripcion
);

public sealed record NotificacionConfigRequest(
    string Canal,
    bool Activo,
    string? SmtpHost,
    int? SmtpPuerto,
    bool? SmtpUsarSsl,
    string? SmtpCorreoRemitente,
    string? SmtpNombreRemitente,
    string? SmtpUsuario,
    string? SmtpPassword,
    string? WhatsAppEndpoint,
    string? WhatsAppToken,
    string? WhatsAppNumeroOrigen,
    int TimeoutSegundos
);
