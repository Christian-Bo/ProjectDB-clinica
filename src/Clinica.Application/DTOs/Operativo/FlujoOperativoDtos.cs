namespace Clinica.Application.DTOs.Operativo;

public sealed class LookupItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int? SedeId { get; set; }
    public string? SedeNombre { get; set; }
    public int? ServicioId { get; set; }
    public string? ServicioNombre { get; set; }
    public int? EspecialidadId { get; set; }
    public string? EspecialidadNombre { get; set; }
    public int? MedicoId { get; set; }
    public string? MedicoNombre { get; set; }
    public int? ConsultorioId { get; set; }
    public string? ConsultorioNombre { get; set; }
    public int? EstacionId { get; set; }
    public string? VentanillaNombre { get; set; }
    public int? SecretariaUsuarioId { get; set; }
    public string? SecretariaNombre { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class AgendaSlotDto
{
    public int SedeId { get; set; }
    public string SedeNombre { get; set; } = string.Empty;
    public int? ServicioId { get; set; }
    public string? ServicioNombre { get; set; }
    public int? EspecialidadId { get; set; }
    public string? EspecialidadNombre { get; set; }
    public int MedicoId { get; set; }
    public string MedicoNombre { get; set; } = string.Empty;
    public int ConsultorioId { get; set; }
    public string ConsultorioNombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
    public int DuracionSlotMinutos { get; set; }
    public int CapacidadPorSlot { get; set; }
    public int CitasTomadas { get; set; }
    public bool Disponible { get; set; }
    public string EstadoSlot { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class SecretariaContextoDto
{
    public int SecretariaAsignacionId { get; set; }
    public int UsuarioId { get; set; }
    public string SecretariaNombre { get; set; } = string.Empty;
    public int SedeId { get; set; }
    public string SedeNombre { get; set; } = string.Empty;
    public int? ServicioId { get; set; }
    public string ServicioNombre { get; set; } = string.Empty;
    public int EstacionId { get; set; }
    public string EstacionNombre { get; set; } = string.Empty;
    public string VentanillaNombre { get; set; } = string.Empty;
    public string TipoEstacion { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
    public bool Activo { get; set; }
    public string RolOperativo { get; set; } = string.Empty;
    public int TotalClinicas { get; set; }
    public string? ClinicasAsignadas { get; set; }
}

public sealed class SecretariaTicketDto
{
    public long TicketId { get; set; }
    public string NumeroTicket { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string EtapaActual { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int PrioridadNivel { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public string NumeroExpediente { get; set; } = string.Empty;
    public long? CitaId { get; set; }
    public DateTime? FechaCita { get; set; }
    public int SedeId { get; set; }
    public string SedeNombre { get; set; } = string.Empty;
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = string.Empty;
    public string? EspecialidadNombre { get; set; }
    public int? EstacionId { get; set; }
    public string? EstacionNombre { get; set; }
    public string? VentanillaNombre { get; set; }
    public int? SecretariaUsuarioId { get; set; }
    public string? SecretariaNombre { get; set; }
    public string? EstadoAsignacion { get; set; }
    public DateTime? FechaAsignacion { get; set; }
    public DateTime? FechaToma { get; set; }
    public DateTime? FechaRegistroAsistencia { get; set; }
    public DateTime? FechaEnvioMedico { get; set; }
    public int? MedicoId { get; set; }
    public string? MedicoNombre { get; set; }
    public int? ConsultorioId { get; set; }
    public string? ConsultorioNombre { get; set; }
    public int MinutosEspera { get; set; }
    public string? DestinoTipo { get; set; }
    public string? DestinoActual { get; set; }
}

public sealed class SecretariaResumenDto
{
    public int TicketsPendientes { get; set; }
    public int TicketsTomados { get; set; }
    public int AsistenciasRegistradas { get; set; }
    public int EnviadosMedico { get; set; }
    public int NoShow { get; set; }
    public decimal PromedioEsperaMinutos { get; set; }
    public string UltimoTicketTomado { get; set; } = string.Empty;
    public string NombreVentanilla { get; set; } = string.Empty;
    public string NombreSede { get; set; } = string.Empty;
    public string NombreServicio { get; set; } = string.Empty;
}

public sealed class SecretariaConfigurarContextoRequest
{
    public int UsuarioId { get; set; }
    public int SedeId { get; set; }
    public int? ServicioId { get; set; }
    public int EstacionId { get; set; }
}

public sealed class SecretariaTomarSiguienteRequest
{
    public int UsuarioId { get; set; }
    public int SedeId { get; set; }
    public int? ServicioId { get; set; }
    public int EstacionId { get; set; }
}

public sealed class SecretariaRegistrarAsistenciaRequest
{
    public int UsuarioId { get; set; }
    public int EstacionId { get; set; }
    public bool DocumentoValidado { get; set; } = true;
    public bool DatosContactoActualizados { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class SecretariaEnviarMedicoRequest
{
    public int UsuarioId { get; set; }
    public int EstacionId { get; set; }
    public int? MedicoId { get; set; }
    public int? ConsultorioId { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class SecretariaNoShowRequest
{
    public int UsuarioId { get; set; }
    public int EstacionId { get; set; }
    public string? Motivo { get; set; }
}

public sealed class MedicoContextoDto
{
    public int MedicoId { get; set; }
    public int UsuarioId { get; set; }
    public string MedicoNombre { get; set; } = string.Empty;
    public int? EspecialidadId { get; set; }
    public string? EspecialidadNombre { get; set; }
    public int? SedeId { get; set; }
    public string? SedeNombre { get; set; }
    public int? ConsultorioId { get; set; }
    public string? ConsultorioNombre { get; set; }
    public string? NumeroColegiado { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public sealed class MedicoLlamarSiguienteRequest
{
    public int MedicoId { get; set; }
    public int? UsuarioId { get; set; }
    public int? SedeId { get; set; }
    public int? ConsultorioId { get; set; }
}

public sealed class MedicoMarcarEnConsultaRequest
{
    public int? UsuarioId { get; set; }
}

public sealed class NotificacionConfiguracionDto
{
    public int ConfiguracionId { get; set; }
    public string Canal { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public string? SmtpHost { get; set; }
    public int? SmtpPuerto { get; set; }
    public bool? SmtpUsarSsl { get; set; }
    public string? SmtpCorreoRemitente { get; set; }
    public string? SmtpNombreRemitente { get; set; }
    public string? SmtpUsuario { get; set; }
    public bool TieneSmtpPassword { get; set; }
    public string? WhatsAppEndpoint { get; set; }
    public bool TieneWhatsAppToken { get; set; }
    public string? WhatsAppNumeroOrigen { get; set; }
    public int TimeoutSegundos { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public int? ActualizadoPor { get; set; }
}

public sealed class GuardarNotificacionConfiguracionRequest
{
    public string Canal { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public string? SmtpHost { get; set; }
    public int? SmtpPuerto { get; set; }
    public bool? SmtpUsarSsl { get; set; }
    public string? SmtpCorreoRemitente { get; set; }
    public string? SmtpNombreRemitente { get; set; }
    public string? SmtpUsuario { get; set; }
    public string? SmtpPassword { get; set; }
    public string? WhatsAppEndpoint { get; set; }
    public string? WhatsAppToken { get; set; }
    public string? WhatsAppNumeroOrigen { get; set; }
    public int TimeoutSegundos { get; set; } = 30;
    public int? UsuarioId { get; set; }
}
