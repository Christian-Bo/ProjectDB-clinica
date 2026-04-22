using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.Models.Tickets;

// -----------------------------------------------------------------------------
// DTOs del modulo 3.
// Estan organizados para ser faciles de consumir en frontend y claros de leer.
// Se prioriza que el usuario vea etiquetas/nombres utiles y no solo ids.
// -----------------------------------------------------------------------------
public sealed class GenerateTicketRequestDto
{
    public long? CitaId { get; set; }
    public int? PacienteId { get; set; }
    public int? SedeId { get; set; }
    public int? ServicioId { get; set; }
    public int? MedicoId { get; set; }

    [MaxLength(20)]
    public string PrioridadSolicitada { get; set; } = "NORMAL";

    [MaxLength(300)]
    public string? MotivoEspecial { get; set; }

    public int? UsuarioId { get; set; }
}

public sealed class SpecialTicketRequestDto
{
    public long? CitaId { get; set; }
    public int? PacienteId { get; set; }
    public int? SedeId { get; set; }
    public int? ServicioId { get; set; }
    public int? MedicoId { get; set; }

    [Required]
    [MaxLength(300)]
    public string MotivoEspecial { get; set; } = string.Empty;

    public int? UsuarioId { get; set; }
}

public sealed class CallNextTicketRequestDto
{
    [Required]
    public int SedeId { get; set; }

    [Required]
    public int ServicioId { get; set; }

    public int? EstacionId { get; set; }
    public int? UsuarioId { get; set; }
}

public sealed class FinalizeTicketRequestDto
{
    [MaxLength(200)]
    public string? Motivo { get; set; }
}

public sealed class TicketListFiltersDto
{
    public int? SedeId { get; set; }
    public int? ServicioId { get; set; }
    public string? Estado { get; set; }
    public string? Prioridad { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

public sealed class SelectionOptionDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Codigo { get; init; }
    public string? Descripcion { get; init; }
    public bool Activo { get; init; } = true;
}

public sealed class PatientSelectionDto
{
    public int PacienteId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? NumeroExpediente { get; init; }
    public string? Documento { get; init; }
    public string? Telefono { get; init; }
    public string? CorreoElectronico { get; init; }
    public bool EsDiscapacitado { get; init; }
}

public sealed class AppointmentSelectionDto
{
    public long CitaId { get; init; }
    public int PacienteId { get; init; }
    public string Label { get; init; } = string.Empty;
    public DateTime FechaInicio { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string PacienteNombre { get; init; } = string.Empty;
    public string ServicioNombre { get; init; } = string.Empty;
    public string SedeNombre { get; init; } = string.Empty;
    public string? MedicoNombre { get; init; }
}

public sealed class TicketDetailDto
{
    public long TicketId { get; init; }
    public string NumeroTicket { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string Prioridad { get; init; } = string.Empty;
    public bool EsEspecial { get; init; }
    public string? MotivoEspecial { get; init; }
    public long? CitaId { get; init; }
    public string? CitaEstado { get; init; }
    public int PacienteId { get; init; }
    public string PacienteNombre { get; init; } = string.Empty;
    public string? NumeroExpediente { get; init; }
    public string? PacienteDocumento { get; init; }
    public int SedeId { get; init; }
    public string SedeNombre { get; init; } = string.Empty;
    public int ServicioId { get; init; }
    public string ServicioNombre { get; init; } = string.Empty;
    public string? EspecialidadNombre { get; init; }
    public int? MedicoId { get; init; }
    public string? MedicoNombre { get; init; }
    public int? ConsultorioId { get; init; }
    public string? ConsultorioNombre { get; init; }
    public int? AutorizadoPorId { get; init; }
    public string? AutorizadoPorNombre { get; init; }
    public DateTime FechaGeneracion { get; init; }
    public DateTime? FechaLlamado { get; init; }
    public DateTime? FechaInicioAtencion { get; init; }
    public DateTime? FechaFinAtencion { get; init; }
    public int ContadorLlamados { get; init; }
}

public sealed class QueueTicketPreviewDto
{
    public long TicketId { get; init; }
    public string NumeroTicket { get; init; } = string.Empty;
    public string Prioridad { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public DateTime? FechaReferencia { get; init; }
    public int? ConsultorioId { get; init; }
    public string? ConsultorioNombre { get; set; }
}

public sealed class QueueDisplayResponseDto
{
    public int SedeId { get; init; }
    public string SedeNombre { get; init; } = string.Empty;
    public int ServicioId { get; init; }
    public string ServicioNombre { get; init; } = string.Empty;
    public QueueTicketPreviewDto? Actual { get; init; }
    public IReadOnlyList<QueueTicketPreviewDto> Proximos { get; init; } = Array.Empty<QueueTicketPreviewDto>();
    public DateTime ConsultadoEnUtc { get; init; }
}

public sealed class ReceptionOperationalSummaryDto
{
    public int? SedeId { get; init; }
    public string? SedeNombre { get; init; }
    public int? ServicioId { get; init; }
    public string? ServicioNombre { get; init; }
    public int TicketsEnEspera { get; init; }
    public int TicketsLlamados { get; init; }
    public int TicketsEnAtencion { get; init; }
    public int TicketsFinalizados { get; init; }
    public int TicketsNoShow { get; init; }
    public int TicketsEspecialesHoy { get; init; }
    public string? UltimoTicketLlamado { get; init; }
    public DateTime ConsultadoEnUtc { get; init; }
}

public sealed class NoShowProcessResponseDto
{
    public int RegistrosProcesados { get; init; }
}
