using System.Data;
using System.Linq;
using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Operativo;
using Clinica.Application.Models.Common;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;

namespace Clinica.Infrastructure.Services;

internal static class FlujoMapper
{
    public static ServiceOperationResult<T> Result<T>(DataTable? header, T? data)
    {
        var row = header is { Rows.Count: > 0 } ? header.Rows[0] : null;
        return new ServiceOperationResult<T>
        {
            HttpStatus = row != null && row.Table.HasColumn("StatusCode") ? row.Int32("StatusCode") : 200,
            Code = row != null && row.Table.HasColumn("Code") ? row.Str("Code") : "OK",
            Message = row != null && row.Table.HasColumn("Message") ? row.Str("Message") : "Operacion ejecutada correctamente.",
            Data = data
        };
    }

    public static LookupItemDto Lookup(DataRow r) => new()
    {
        Id = r.Table.HasColumn("Id") ? r.Int32("Id") : 0,
        Nombre = r.Table.HasColumn("Nombre") ? r.Str("Nombre") : string.Empty,
        Label = r.Table.HasColumn("Label") ? r.Str("Label") : (r.Table.HasColumn("Nombre") ? r.Str("Nombre") : string.Empty),
        SedeId = r.Table.HasColumn("SedeId") ? r.Int32Null("SedeId") : null,
        SedeNombre = r.Table.HasColumn("SedeNombre") ? r.StrNull("SedeNombre") : null,
        ServicioId = r.Table.HasColumn("ServicioId") ? r.Int32Null("ServicioId") : null,
        ServicioNombre = r.Table.HasColumn("ServicioNombre") ? r.StrNull("ServicioNombre") : null,
        EspecialidadId = r.Table.HasColumn("EspecialidadId") ? r.Int32Null("EspecialidadId") : null,
        EspecialidadNombre = r.Table.HasColumn("EspecialidadNombre") ? r.StrNull("EspecialidadNombre") : null,
        MedicoId = r.Table.HasColumn("MedicoId") ? r.Int32Null("MedicoId") : null,
        MedicoNombre = r.Table.HasColumn("MedicoNombre") ? r.StrNull("MedicoNombre") : null,
        ConsultorioId = r.Table.HasColumn("ConsultorioId") ? r.Int32Null("ConsultorioId") : null,
        ConsultorioNombre = r.Table.HasColumn("ConsultorioNombre") ? r.StrNull("ConsultorioNombre") : null,
        EstacionId = r.Table.HasColumn("EstacionId") ? r.Int32Null("EstacionId") : null,
        VentanillaNombre = r.Table.HasColumn("VentanillaNombre") ? r.StrNull("VentanillaNombre") : null,
        SecretariaUsuarioId = r.Table.HasColumn("SecretariaUsuarioId") ? r.Int32Null("SecretariaUsuarioId") : null,
        SecretariaNombre = r.Table.HasColumn("SecretariaNombre") ? r.StrNull("SecretariaNombre") : null,
        Activo = !r.Table.HasColumn("Activo") || r.Bool("Activo")
    };

    public static AgendaSlotDto Slot(DataRow r) => new()
    {
        SedeId = r.Int32("SedeId"),
        SedeNombre = r.Str("SedeNombre"),
        ServicioId = r.Table.HasColumn("ServicioId") ? r.Int32Null("ServicioId") : null,
        ServicioNombre = r.Table.HasColumn("ServicioNombre") ? r.StrNull("ServicioNombre") : null,
        EspecialidadId = r.Table.HasColumn("EspecialidadId") ? r.Int32Null("EspecialidadId") : null,
        EspecialidadNombre = r.Table.HasColumn("EspecialidadNombre") ? r.StrNull("EspecialidadNombre") : null,
        MedicoId = r.Int32("MedicoId"),
        MedicoNombre = r.Str("MedicoNombre"),
        ConsultorioId = r.Int32("ConsultorioId"),
        ConsultorioNombre = r.Str("ConsultorioNombre"),
        FechaInicio = r.DateTime("FechaInicio"),
        FechaFin = r.DateTime("FechaFin"),
        HoraInicio = r.Str("HoraInicio"),
        HoraFin = r.Str("HoraFin"),
        DuracionSlotMinutos = r.Int32("DuracionSlotMinutos"),
        CapacidadPorSlot = r.Int32("CapacidadPorSlot"),
        CitasTomadas = r.Int32("CitasTomadas"),
        Disponible = r.Bool("Disponible"),
        EstadoSlot = r.Str("EstadoSlot"),
        Motivo = r.Str("Motivo")
    };

    public static SecretariaContextoDto Contexto(DataRow r) => new()
    {
        SecretariaAsignacionId = r.Int32("SecretariaAsignacionId"),
        UsuarioId = r.Int32("UsuarioId"),
        SecretariaNombre = r.Table.HasColumn("SecretariaNombre") ? r.Str("SecretariaNombre") : string.Empty,
        SedeId = r.Int32("SedeId"),
        SedeNombre = r.Str("SedeNombre"),
        ServicioId = r.Table.HasColumn("ServicioId") ? r.Int32Null("ServicioId") : null,
        ServicioNombre = r.Table.HasColumn("ServicioNombre") ? r.Str("ServicioNombre") : string.Empty,
        EstacionId = r.Int32("EstacionId"),
        EstacionNombre = r.Str("EstacionNombre"),
        VentanillaNombre = r.Table.HasColumn("VentanillaNombre") ? r.Str("VentanillaNombre") : r.Str("EstacionNombre"),
        TipoEstacion = r.Table.HasColumn("TipoEstacion") ? r.Str("TipoEstacion") : string.Empty,
        EsPrincipal = r.Table.HasColumn("EsPrincipal") && r.Bool("EsPrincipal"),
        Activo = !r.Table.HasColumn("Activo") || r.Bool("Activo"),
        RolOperativo = r.Table.HasColumn("RolOperativo") ? r.Str("RolOperativo") : string.Empty,
        TotalClinicas = r.Table.HasColumn("TotalClinicas") ? r.Int32("TotalClinicas") : 0,
        ClinicasAsignadas = r.Table.HasColumn("ClinicasAsignadas") ? r.StrNull("ClinicasAsignadas") : null
    };

    public static SecretariaTicketDto Ticket(DataRow r) => new()
    {
        TicketId = r.Int64("TicketId"),
        NumeroTicket = r.Str("NumeroTicket"),
        Estado = r.Str("Estado"),
        EtapaActual = r.Table.HasColumn("EtapaActual") ? r.Str("EtapaActual") : string.Empty,
        Prioridad = r.Table.HasColumn("Prioridad") ? r.Str("Prioridad") : string.Empty,
        PrioridadNivel = r.Table.HasColumn("PrioridadNivel") ? r.Int32("PrioridadNivel") : 0,
        PacienteId = r.Table.HasColumn("PacienteId") ? r.Int32("PacienteId") : 0,
        PacienteNombre = r.Table.HasColumn("PacienteNombre") ? r.Str("PacienteNombre") : string.Empty,
        NumeroExpediente = r.Table.HasColumn("NumeroExpediente") ? r.Str("NumeroExpediente") : string.Empty,
        CitaId = r.Table.HasColumn("CitaId") ? r.Int64Null("CitaId") : null,
        FechaCita = r.Table.HasColumn("FechaCita") ? r.DateTimeNull("FechaCita") : null,
        SedeId = r.Table.HasColumn("SedeId") ? r.Int32("SedeId") : 0,
        SedeNombre = r.Table.HasColumn("SedeNombre") ? r.Str("SedeNombre") : string.Empty,
        ServicioId = r.Table.HasColumn("ServicioId") ? r.Int32("ServicioId") : 0,
        ServicioNombre = r.Table.HasColumn("ServicioNombre") ? r.Str("ServicioNombre") : string.Empty,
        EspecialidadNombre = r.Table.HasColumn("EspecialidadNombre") ? r.StrNull("EspecialidadNombre") : null,
        EstacionId = r.Table.HasColumn("EstacionId") ? r.Int32Null("EstacionId") : null,
        EstacionNombre = r.Table.HasColumn("EstacionNombre") ? r.StrNull("EstacionNombre") : null,
        VentanillaNombre = r.Table.HasColumn("VentanillaNombre") ? r.StrNull("VentanillaNombre") : null,
        SecretariaUsuarioId = r.Table.HasColumn("SecretariaUsuarioId") ? r.Int32Null("SecretariaUsuarioId") : null,
        SecretariaNombre = r.Table.HasColumn("SecretariaNombre") ? r.StrNull("SecretariaNombre") : null,
        EstadoAsignacion = r.Table.HasColumn("EstadoAsignacion") ? r.StrNull("EstadoAsignacion") : null,
        FechaAsignacion = r.Table.HasColumn("FechaAsignacion") ? r.DateTimeNull("FechaAsignacion") : null,
        FechaToma = r.Table.HasColumn("FechaToma") ? r.DateTimeNull("FechaToma") : null,
        FechaRegistroAsistencia = r.Table.HasColumn("FechaRegistroAsistencia") ? r.DateTimeNull("FechaRegistroAsistencia") : null,
        FechaEnvioMedico = r.Table.HasColumn("FechaEnvioMedico") ? r.DateTimeNull("FechaEnvioMedico") : null,
        MedicoId = r.Table.HasColumn("MedicoId") ? r.Int32Null("MedicoId") : null,
        MedicoNombre = r.Table.HasColumn("MedicoNombre") ? r.StrNull("MedicoNombre") : null,
        ConsultorioId = r.Table.HasColumn("ConsultorioId") ? r.Int32Null("ConsultorioId") : null,
        ConsultorioNombre = r.Table.HasColumn("ConsultorioNombre") ? r.StrNull("ConsultorioNombre") : null,
        MinutosEspera = r.Table.HasColumn("MinutosEspera") ? r.Int32("MinutosEspera") : 0,
        DestinoTipo = r.Table.HasColumn("DestinoTipo") ? r.StrNull("DestinoTipo") : null,
        DestinoActual = r.Table.HasColumn("DestinoActual") ? r.StrNull("DestinoActual") : null
    };

    public static SecretariaResumenDto Resumen(DataRow r) => new()
    {
        TicketsPendientes = r.Table.HasColumn("TicketsPendientes") ? r.Int32("TicketsPendientes") : 0,
        TicketsTomados = r.Table.HasColumn("TicketsTomados") ? r.Int32("TicketsTomados") : 0,
        AsistenciasRegistradas = r.Table.HasColumn("AsistenciasRegistradas") ? r.Int32("AsistenciasRegistradas") : 0,
        EnviadosMedico = r.Table.HasColumn("EnviadosMedico") ? r.Int32("EnviadosMedico") : 0,
        NoShow = r.Table.HasColumn("NoShow") ? r.Int32("NoShow") : 0,
        PromedioEsperaMinutos = r.Table.HasColumn("PromedioEsperaMinutos") && !r.IsNull("PromedioEsperaMinutos") ? Convert.ToDecimal(r["PromedioEsperaMinutos"]) : 0,
        UltimoTicketTomado = r.Table.HasColumn("UltimoTicketTomado") ? r.Str("UltimoTicketTomado") : string.Empty,
        NombreVentanilla = r.Table.HasColumn("NombreVentanilla") ? r.Str("NombreVentanilla") : string.Empty,
        NombreSede = r.Table.HasColumn("NombreSede") ? r.Str("NombreSede") : string.Empty,
        NombreServicio = r.Table.HasColumn("NombreServicio") ? r.Str("NombreServicio") : string.Empty
    };

    public static MedicoContextoDto MedicoContexto(DataRow r) => new()
    {
        MedicoId = r.Int32("MedicoId"),
        UsuarioId = r.Int32("UsuarioId"),
        MedicoNombre = r.Str("MedicoNombre"),
        EspecialidadId = r.Table.HasColumn("EspecialidadId") ? r.Int32Null("EspecialidadId") : null,
        EspecialidadNombre = r.Table.HasColumn("EspecialidadNombre") ? r.StrNull("EspecialidadNombre") : null,
        SedeId = r.Table.HasColumn("SedeId") ? r.Int32Null("SedeId") : null,
        SedeNombre = r.Table.HasColumn("SedeNombre") ? r.StrNull("SedeNombre") : null,
        ConsultorioId = r.Table.HasColumn("ConsultorioId") ? r.Int32Null("ConsultorioId") : null,
        ConsultorioNombre = r.Table.HasColumn("ConsultorioNombre") ? r.StrNull("ConsultorioNombre") : null,
        NumeroColegiado = r.Table.HasColumn("NumeroColegiado") ? r.StrNull("NumeroColegiado") : null,
        Estado = r.Table.HasColumn("Estado") ? r.Str("Estado") : string.Empty
    };

    public static NotificacionConfiguracionDto Notificacion(DataRow r) => new()
    {
        ConfiguracionId = r.Int32("ConfiguracionId"),
        Canal = r.Str("Canal"),
        Activo = r.Bool("Activo"),
        SmtpHost = r.Table.HasColumn("SmtpHost") ? r.StrNull("SmtpHost") : null,
        SmtpPuerto = r.Table.HasColumn("SmtpPuerto") ? r.Int32Null("SmtpPuerto") : null,
        SmtpUsarSsl = r.Table.HasColumn("SmtpUsarSsl") && !r.IsNull("SmtpUsarSsl") ? Convert.ToBoolean(r["SmtpUsarSsl"]) : null,
        SmtpCorreoRemitente = r.Table.HasColumn("SmtpCorreoRemitente") ? r.StrNull("SmtpCorreoRemitente") : null,
        SmtpNombreRemitente = r.Table.HasColumn("SmtpNombreRemitente") ? r.StrNull("SmtpNombreRemitente") : null,
        SmtpUsuario = r.Table.HasColumn("SmtpUsuario") ? r.StrNull("SmtpUsuario") : null,
        TieneSmtpPassword = r.Table.HasColumn("TieneSmtpPassword") && r.Bool("TieneSmtpPassword"),
        WhatsAppEndpoint = r.Table.HasColumn("WhatsAppEndpoint") ? r.StrNull("WhatsAppEndpoint") : null,
        TieneWhatsAppToken = r.Table.HasColumn("TieneWhatsAppToken") && r.Bool("TieneWhatsAppToken"),
        WhatsAppNumeroOrigen = r.Table.HasColumn("WhatsAppNumeroOrigen") ? r.StrNull("WhatsAppNumeroOrigen") : null,
        TimeoutSegundos = r.Table.HasColumn("TimeoutSegundos") ? r.Int32("TimeoutSegundos") : 30,
        FechaActualizacion = r.Table.HasColumn("FechaActualizacion") ? r.DateTimeNull("FechaActualizacion") : null,
        ActualizadoPor = r.Table.HasColumn("ActualizadoPor") ? r.Int32Null("ActualizadoPor") : null
    };
}

public sealed class OperativoCatalogosService(SqlExecutor db) : IOperativoCatalogosService
{
    public async Task<List<LookupItemDto>> LookupAsync(string tipo, int? sedeId, int? servicioId, int? especialidadId, int? medicoId, int? consultorioId, int? usuarioId, string? busqueda, int top, CancellationToken ct = default)
    {
        var table = await db.ExecuteSpFirstTableAsync("dbo.sp_Lookup_Global", new[]
        {
            new SqlParameter("@Tipo", tipo),
            new SqlParameter("@SedeId", (object?)sedeId ?? DBNull.Value),
            new SqlParameter("@ServicioId", (object?)servicioId ?? DBNull.Value),
            new SqlParameter("@EspecialidadId", (object?)especialidadId ?? DBNull.Value),
            new SqlParameter("@MedicoId", (object?)medicoId ?? DBNull.Value),
            new SqlParameter("@ConsultorioId", (object?)consultorioId ?? DBNull.Value),
            new SqlParameter("@UsuarioId", (object?)usuarioId ?? DBNull.Value),
            new SqlParameter("@Busqueda", (object?)busqueda ?? DBNull.Value),
            new SqlParameter("@Top", top)
        }, ct);
        return table.Rows.Cast<DataRow>().Select(FlujoMapper.Lookup).ToList();
    }

    public async Task<List<AgendaSlotDto>> ListarDisponibilidadAsync(int sedeId, DateTime fecha, int? servicioId, int? especialidadId, int? medicoId, bool soloDisponibles, CancellationToken ct = default)
    {
        var table = await db.ExecuteSpFirstTableAsync("dbo.sp_Agenda_Disponibilidad_Listar", new[]
        {
            new SqlParameter("@SedeId", sedeId),
            new SqlParameter("@Fecha", fecha.Date),
            new SqlParameter("@ServicioId", (object?)servicioId ?? DBNull.Value),
            new SqlParameter("@EspecialidadId", (object?)especialidadId ?? DBNull.Value),
            new SqlParameter("@MedicoId", (object?)medicoId ?? DBNull.Value),
            new SqlParameter("@SoloDisponibles", soloDisponibles)
        }, ct);
        return table.Rows.Cast<DataRow>().Select(FlujoMapper.Slot).ToList();
    }
}

public sealed class SecretariaService(SqlExecutor db) : ISecretariaService
{
    public async Task<List<SecretariaContextoDto>> ObtenerContextosAsync(int usuarioId, CancellationToken ct = default)
    {
        var table = await db.ExecuteSpFirstTableAsync("dbo.sp_Secretaria_ObtenerContextos", new[] { new SqlParameter("@UsuarioId", usuarioId) }, ct);
        return table.Rows.Cast<DataRow>().Select(FlujoMapper.Contexto).ToList();
    }

    public async Task<SecretariaContextoDto?> ConfigurarContextoAsync(SecretariaConfigurarContextoRequest request, CancellationToken ct = default)
    {
        var table = await db.ExecuteSpFirstTableAsync("dbo.sp_Secretaria_ConfigurarContexto", new[]
        {
            new SqlParameter("@UsuarioId", request.UsuarioId),
            new SqlParameter("@SedeId", request.SedeId),
            new SqlParameter("@ServicioId", (object?)request.ServicioId ?? DBNull.Value),
            new SqlParameter("@EstacionId", request.EstacionId)
        }, ct);
        return table.Rows.Count == 0 ? null : FlujoMapper.Contexto(table.Rows[0]);
    }

    public async Task<List<SecretariaTicketDto>> ListarColaAsync(int usuarioId, int sedeId, int? servicioId, int estacionId, string? estado, int top, CancellationToken ct = default)
    {
        var table = await db.ExecuteSpFirstTableAsync("dbo.sp_Secretaria_ListarCola", new[]
        {
            new SqlParameter("@UsuarioId", usuarioId),
            new SqlParameter("@SedeId", sedeId),
            new SqlParameter("@ServicioId", (object?)servicioId ?? DBNull.Value),
            new SqlParameter("@EstacionId", estacionId),
            new SqlParameter("@Estado", (object?)estado ?? DBNull.Value),
            new SqlParameter("@Top", top)
        }, ct);
        return table.Rows.Cast<DataRow>().Select(FlujoMapper.Ticket).ToList();
    }

    public async Task<ServiceOperationResult<SecretariaTicketDto>> TomarSiguienteAsync(SecretariaTomarSiguienteRequest request, CancellationToken ct = default)
    {
        var ds = await db.ExecuteSpAsync("dbo.sp_Secretaria_TomarSiguiente", new[]
        {
            new SqlParameter("@UsuarioId", request.UsuarioId),
            new SqlParameter("@SedeId", request.SedeId),
            new SqlParameter("@ServicioId", (object?)request.ServicioId ?? DBNull.Value),
            new SqlParameter("@EstacionId", request.EstacionId)
        }, ct);
        return FlujoMapper.Result(ds.Tables.Count > 0 ? ds.Tables[0] : null, MapFirstTicket(ds, 1));
    }

    public async Task<ServiceOperationResult<SecretariaTicketDto>> RegistrarAsistenciaAsync(long ticketId, SecretariaRegistrarAsistenciaRequest request, CancellationToken ct = default)
    {
        var ds = await db.ExecuteSpAsync("dbo.sp_Secretaria_RegistrarAsistencia", new[]
        {
            new SqlParameter("@TicketId", ticketId),
            new SqlParameter("@UsuarioId", request.UsuarioId),
            new SqlParameter("@EstacionId", request.EstacionId),
            new SqlParameter("@DocumentoValidado", request.DocumentoValidado),
            new SqlParameter("@DatosContactoActualizados", request.DatosContactoActualizados),
            new SqlParameter("@Observaciones", (object?)request.Observaciones ?? DBNull.Value)
        }, ct);
        return FlujoMapper.Result(ds.Tables.Count > 0 ? ds.Tables[0] : null, MapFirstTicket(ds, 1));
    }

    public async Task<ServiceOperationResult<SecretariaTicketDto>> EnviarMedicoAsync(long ticketId, SecretariaEnviarMedicoRequest request, CancellationToken ct = default)
    {
        var ds = await db.ExecuteSpAsync("dbo.sp_Secretaria_EnviarMedico", new[]
        {
            new SqlParameter("@TicketId", ticketId),
            new SqlParameter("@UsuarioId", request.UsuarioId),
            new SqlParameter("@EstacionId", request.EstacionId),
            new SqlParameter("@MedicoId", (object?)request.MedicoId ?? DBNull.Value),
            new SqlParameter("@ConsultorioId", (object?)request.ConsultorioId ?? DBNull.Value),
            new SqlParameter("@Observaciones", (object?)request.Observaciones ?? DBNull.Value)
        }, ct);
        return FlujoMapper.Result(ds.Tables.Count > 0 ? ds.Tables[0] : null, MapFirstTicket(ds, 1));
    }

    public async Task<ServiceOperationResult<object>> MarcarNoShowAsync(long ticketId, SecretariaNoShowRequest request, CancellationToken ct = default)
    {
        var ds = await db.ExecuteSpAsync("dbo.sp_Secretaria_MarcarNoShow", new[]
        {
            new SqlParameter("@TicketId", ticketId),
            new SqlParameter("@UsuarioId", request.UsuarioId),
            new SqlParameter("@EstacionId", request.EstacionId),
            new SqlParameter("@Motivo", (object?)request.Motivo ?? DBNull.Value)
        }, ct);
        return FlujoMapper.Result<object>(ds.Tables.Count > 0 ? ds.Tables[0] : null, null);
    }

    public async Task<SecretariaResumenDto?> ObtenerResumenAsync(int usuarioId, int sedeId, int? servicioId, int estacionId, CancellationToken ct = default)
    {
        var table = await db.ExecuteSpFirstTableAsync("dbo.sp_Secretaria_ResumenVentanilla", new[]
        {
            new SqlParameter("@UsuarioId", usuarioId),
            new SqlParameter("@SedeId", sedeId),
            new SqlParameter("@ServicioId", (object?)servicioId ?? DBNull.Value),
            new SqlParameter("@EstacionId", estacionId)
        }, ct);
        return table.Rows.Count == 0 ? null : FlujoMapper.Resumen(table.Rows[0]);
    }

    private static SecretariaTicketDto? MapFirstTicket(DataSet ds, int tableIndex)
    {
        if (ds.Tables.Count <= tableIndex || ds.Tables[tableIndex].Rows.Count == 0) return null;
        return FlujoMapper.Ticket(ds.Tables[tableIndex].Rows[0]);
    }
}

public sealed class MedicoColaService(SqlExecutor db) : IMedicoColaService
{
    public async Task<MedicoContextoDto?> ObtenerContextoAsync(int usuarioId, CancellationToken ct = default)
    {
        var table = await db.ExecuteSpFirstTableAsync("dbo.sp_Medico_Contexto_Obtener", new[] { new SqlParameter("@UsuarioId", usuarioId) }, ct);
        return table.Rows.Count == 0 ? null : FlujoMapper.MedicoContexto(table.Rows[0]);
    }

    public async Task<List<SecretariaTicketDto>> ListarColaAsync(int medicoId, int? sedeId, int? consultorioId, int top, CancellationToken ct = default)
    {
        var table = await db.ExecuteSpFirstTableAsync("dbo.sp_Medico_Cola_Listar", new[]
        {
            new SqlParameter("@MedicoId", medicoId),
            new SqlParameter("@SedeId", (object?)sedeId ?? DBNull.Value),
            new SqlParameter("@ConsultorioId", (object?)consultorioId ?? DBNull.Value),
            new SqlParameter("@Top", top)
        }, ct);
        return table.Rows.Cast<DataRow>().Select(FlujoMapper.Ticket).ToList();
    }

    public async Task<ServiceOperationResult<SecretariaTicketDto>> LlamarSiguienteAsync(MedicoLlamarSiguienteRequest request, CancellationToken ct = default)
    {
        var ds = await db.ExecuteSpAsync("dbo.sp_Medico_LlamarSiguiente", new[]
        {
            new SqlParameter("@MedicoId", request.MedicoId),
            new SqlParameter("@UsuarioId", (object?)request.UsuarioId ?? DBNull.Value),
            new SqlParameter("@SedeId", (object?)request.SedeId ?? DBNull.Value),
            new SqlParameter("@ConsultorioId", (object?)request.ConsultorioId ?? DBNull.Value)
        }, ct);
        return FlujoMapper.Result(ds.Tables.Count > 0 ? ds.Tables[0] : null, MapFirstTicket(ds, 1));
    }

    public async Task<ServiceOperationResult<SecretariaTicketDto>> MarcarEnConsultaAsync(long ticketId, MedicoMarcarEnConsultaRequest request, CancellationToken ct = default)
    {
        var ds = await db.ExecuteSpAsync("dbo.sp_Medico_MarcarEnConsulta", new[]
        {
            new SqlParameter("@TicketId", ticketId),
            new SqlParameter("@UsuarioId", (object?)request.UsuarioId ?? DBNull.Value)
        }, ct);
        return FlujoMapper.Result(ds.Tables.Count > 0 ? ds.Tables[0] : null, MapFirstTicket(ds, 1));
    }

    private static SecretariaTicketDto? MapFirstTicket(DataSet ds, int tableIndex)
    {
        if (ds.Tables.Count <= tableIndex || ds.Tables[tableIndex].Rows.Count == 0) return null;
        return FlujoMapper.Ticket(ds.Tables[tableIndex].Rows[0]);
    }
}

public sealed class NotificacionConfiguracionService(SqlExecutor db) : INotificacionConfiguracionService
{
    public async Task<List<NotificacionConfiguracionDto>> ObtenerAsync(CancellationToken ct = default)
    {
        var table = await db.ExecuteSpFirstTableAsync("dbo.sp_NotificacionConfiguracion_Obtener", null, ct);
        return table.Rows.Cast<DataRow>().Select(FlujoMapper.Notificacion).ToList();
    }

    public async Task<ServiceOperationResult<List<NotificacionConfiguracionDto>>> GuardarAsync(GuardarNotificacionConfiguracionRequest request, CancellationToken ct = default)
    {
        var ds = await db.ExecuteSpAsync("dbo.sp_NotificacionConfiguracion_Guardar", new[]
        {
            new SqlParameter("@Canal", request.Canal),
            new SqlParameter("@Activo", request.Activo),
            new SqlParameter("@SmtpHost", (object?)request.SmtpHost ?? DBNull.Value),
            new SqlParameter("@SmtpPuerto", (object?)request.SmtpPuerto ?? DBNull.Value),
            new SqlParameter("@SmtpUsarSsl", (object?)request.SmtpUsarSsl ?? DBNull.Value),
            new SqlParameter("@SmtpCorreoRemitente", (object?)request.SmtpCorreoRemitente ?? DBNull.Value),
            new SqlParameter("@SmtpNombreRemitente", (object?)request.SmtpNombreRemitente ?? DBNull.Value),
            new SqlParameter("@SmtpUsuario", (object?)request.SmtpUsuario ?? DBNull.Value),
            new SqlParameter("@SmtpPassword", (object?)request.SmtpPassword ?? DBNull.Value),
            new SqlParameter("@WhatsAppEndpoint", (object?)request.WhatsAppEndpoint ?? DBNull.Value),
            new SqlParameter("@WhatsAppToken", (object?)request.WhatsAppToken ?? DBNull.Value),
            new SqlParameter("@WhatsAppNumeroOrigen", (object?)request.WhatsAppNumeroOrigen ?? DBNull.Value),
            new SqlParameter("@TimeoutSegundos", request.TimeoutSegundos),
            new SqlParameter("@UsuarioId", (object?)request.UsuarioId ?? DBNull.Value)
        }, ct);

        var data = ds.Tables.Count > 1
            ? ds.Tables[1].Rows.Cast<DataRow>().Select(FlujoMapper.Notificacion).ToList()
            : new List<NotificacionConfiguracionDto>();

        return FlujoMapper.Result(ds.Tables.Count > 0 ? ds.Tables[0] : null, data);
    }

    public async Task<ServiceOperationResult<object>> ProbarAsync(string canal, CancellationToken ct = default)
    {
        var ds = await db.ExecuteSpAsync("dbo.sp_NotificacionConfiguracion_Probar", new[] { new SqlParameter("@Canal", canal) }, ct);
        return FlujoMapper.Result<object>(ds.Tables.Count > 0 ? ds.Tables[0] : null, null);
    }
}
