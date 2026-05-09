namespace Clinica.Application.Models.Consultas;

// =============================================================================
// REQUEST DTOs
// =============================================================================

public sealed class AbrirConsultaRequestDto
{
    /// <summary>Ticket válido desde el cual se abre la consulta.</summary>
    public long TicketId { get; set; }

    /// <summary>Médico que atiende. Se resuelve desde JWT cuando está disponible.</summary>
    public int? MedicoId { get; set; }

    /// <summary>Modalidad: PRESENCIAL o TELEMEDICINA.</summary>
    public string Modalidad { get; set; } = "PRESENCIAL";

    /// <summary>Usuario que ejecuta la acción (desde JWT o header de prueba).</summary>
    public int? UsuarioId { get; set; }
}

public sealed class CerrarConsultaRequestDto
{
    /// <summary>Hallazgos clínicos del médico.</summary>
    public string? Hallazgos { get; set; }

    /// <summary>Plan de tratamiento.</summary>
    public string? Plan { get; set; }

    /// <summary>Observaciones adicionales.</summary>
    public string? Observaciones { get; set; }

    /// <summary>Usuario que firma el cierre (desde JWT o header de prueba).</summary>
    public int? UsuarioId { get; set; }

    /// <summary>Al menos un diagnóstico es obligatorio para cerrar.</summary>
    public IList<DiagnosticoRequestDto> Diagnosticos { get; set; } = [];

    // Signos vitales opcionales al cierre
    public decimal? PresionSistolica { get; set; }
    public decimal? PresionDiastolica { get; set; }
    public decimal? FrecuenciaCardiaca { get; set; }
    public decimal? FrecuenciaRespiratoria { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? SaturacionOxigeno { get; set; }
    public decimal? PesoKg { get; set; }
    public decimal? TallaCm { get; set; }
}

public sealed class DiagnosticoRequestDto
{
    /// <summary>Código CIE-10 del diagnóstico. Ej: "J06.9"</summary>
    public string CodigoCIE10 { get; set; } = string.Empty;

    /// <summary>Descripción del diagnóstico.</summary>
    public string DescripcionCIE10 { get; set; } = string.Empty;

    /// <summary>PRINCIPAL, SECUNDARIO, PRESUNTIVO, DEFINITIVO o DIFERENCIAL.</summary>
    public string TipoDiagnostico { get; set; } = "PRINCIPAL";

    public string? Notas { get; set; }
}

public sealed class NotaCorreccionRequestDto
{
    /// <summary>Texto de la corrección. No altera el registro original.</summary>
    public string Nota { get; set; } = string.Empty;

    /// <summary>Usuario que agrega la corrección (desde JWT o header de prueba).</summary>
    public int? UsuarioId { get; set; }
}

// =============================================================================
// RESPONSE DTOs
// =============================================================================

public sealed class ConsultaResponseDto
{
    public long ConsultaId { get; set; }
    public long TicketId { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public int MedicoId { get; set; }
    public string MedicoNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Modalidad { get; set; } = string.Empty;
    public string? MotivoConsulta { get; set; }
    public string? Hallazgos { get; set; }
    public string? Plan { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraCierre { get; set; }
    public SignosVitalesDto? SignosVitales { get; set; }
    public IReadOnlyList<DiagnosticoDto> Diagnosticos { get; set; } = [];
    public IReadOnlyList<NotaCorreccionDto> NotasCorreccion { get; set; } = [];
}

public sealed class SignosVitalesDto
{
    public decimal? PresionSistolica { get; set; }
    public decimal? PresionDiastolica { get; set; }
    public decimal? FrecuenciaCardiaca { get; set; }
    public decimal? FrecuenciaRespiratoria { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? SaturacionOxigeno { get; set; }
    public decimal? PesoKg { get; set; }
    public decimal? TallaCm { get; set; }
    public decimal? Imc { get; set; }
}

public sealed class DiagnosticoDto
{
    public long DiagnosticoId { get; set; }
    public string CodigoCie { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string TipoDiagnostico { get; set; } = string.Empty;
}

public sealed class NotaCorreccionDto
{
    public long NotaId { get; set; }
    public string Nota { get; set; } = string.Empty;
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public sealed class HistorialClinicoResponseDto
{
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public IReadOnlyList<ConsultaResumenDto> Consultas { get; set; } = [];
}

public sealed class ConsultaResumenDto
{
    public long ConsultaId { get; set; }
    public string MedicoNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? MotivoConsulta { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraCierre { get; set; }
    public int TotalDiagnosticos { get; set; }
    public int TotalRecetas { get; set; }
    public int TotalOrdenes { get; set; }
}