namespace Clinica.Application.DTOs.Citas;

public sealed class ReservarCitaRequestDto
{
    public int PacienteId { get; set; }
    public int SedeId { get; set; }
    public int ServicioId { get; set; }
    public int? MedicoId { get; set; }
    public int TipoConsultaId { get; set; }
    public DateTime FechaInicio { get; set; }
    public string Modalidad { get; set; } = "PRESENCIAL";
    public string? MotivoConsulta { get; set; }
}

public sealed class ConfirmarCitaRequestDto
{
    public int UsuarioId { get; set; }
}

public sealed class CancelarCitaRequestDto
{
    public int UsuarioId { get; set; }
    public string MotivoCancelacion { get; set; } = string.Empty;
}

public sealed class ReprogramarCitaRequestDto
{
    public int UsuarioId { get; set; }
    public DateTime NuevaFechaInicio { get; set; }
    public Guid IdempotencyKey { get; set; }
}

public sealed class CitaResponseDto
{
    public long CitaId { get; set; }
    public int PacienteId { get; set; }
    public string NumeroExpediente { get; set; } = string.Empty;
    public int SedeId { get; set; }
    public string NombreSede { get; set; } = string.Empty;
    public int ServicioId { get; set; }
    public string NombreServicio { get; set; } = string.Empty;
    public int? MedicoId { get; set; }
    public string? NombreMedico { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Modalidad { get; set; } = string.Empty;
    public string? MotivoConsulta { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public sealed class ListarCitasRequestDto
{
    public int? PacienteId { get; set; }
    public int? MedicoId { get; set; }
    public int? SedeId { get; set; }
    public int? ServicioId { get; set; }
    public string? Estado { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}