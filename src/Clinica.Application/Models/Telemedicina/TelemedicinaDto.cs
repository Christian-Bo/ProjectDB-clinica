using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.Models.Telemedicina;

// =============================================================================
// DTOs — Telemedicina / Sesiones
// Tabla real: dbo.SesionesTelemedicas
// SPs: sp_SesionTelemedica_Upsert | sp_SesionTelemedica_Obtener | sp_SesionTelemedica_Listar
// NOTA: El PK de la tabla es SesionTeleId (BIGINT IDENTITY)
// =============================================================================

/// <summary>
/// Crear o actualizar sesión. Basado exactamente en sp_SesionTelemedica_Upsert.
/// UrlSala y CodigoSala son OBLIGATORIOS (NOT NULL en la tabla).
/// CodigoSala es UNIQUE en la BD.
/// </summary>
public sealed class SesionTelemedicaUpsertDto
{
    /// <summary>Null → INSERT, con valor → UPDATE.</summary>
    public long? SesionTeleId { get; set; }

    [Required]
    public long CitaId { get; set; }

    public long? ConsultaId { get; set; }

    public int? PlataformaVideoId { get; set; }

    [Required, MaxLength(500)]
    public string UrlSala { get; set; } = string.Empty;

    /// <summary>Código único de la sala. Obligatorio y único en BD.</summary>
    [Required, MaxLength(50)]
    public string CodigoSala { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? PasswordSala { get; set; }

    /// <summary>PROGRAMADA | ACTIVA | FINALIZADA | NO_INICIADA | CANCELADA</summary>
    [MaxLength(20)]
    public string Estado { get; set; } = "PROGRAMADA";

    public DateTime? FechaInicioReal { get; set; }
    public DateTime? FechaFinReal { get; set; }

    [MaxLength(500)]
    public string? GrabacionUrl { get; set; }

    [MaxLength(1000)]
    public string? NotasSesion { get; set; }

    [MaxLength(500)]
    public string? TokenMedico { get; set; }

    [MaxLength(500)]
    public string? TokenPaciente { get; set; }

    public DateTime? TokenExpiracion { get; set; }
}

/// <summary>Respuesta de lectura de una sesión desde dbo.SesionesTelemedicas.</summary>
public sealed class SesionTelemedicaDto
{
    public long SesionTeleId { get; init; }
    public long CitaId { get; init; }
    public long? ConsultaId { get; init; }
    public int? PlataformaVideoId { get; init; }
    public string? PlataformaVideoNombre { get; init; }
    public int? PacienteId { get; init; }
    public string? PacienteNombre { get; init; }
    public int? MedicoId { get; init; }
    public string? MedicoNombre { get; init; }
    public int? SedeId { get; init; }
    public string? SedeNombre { get; init; }
    public int? ServicioId { get; init; }
    public string? ServicioNombre { get; init; }
    public string UrlSala { get; init; } = string.Empty;
    public string CodigoSala { get; init; } = string.Empty;
    public string? PasswordSala { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaInicioReal { get; init; }
    public DateTime? FechaFinReal { get; init; }
    public int? DuracionMinutos { get; init; }
    public string? GrabacionUrl { get; init; }
    public string? NotasSesion { get; init; }
    public string? TokenMedico { get; init; }
    public string? TokenPaciente { get; init; }
    public DateTime? TokenExpiracion { get; init; }
}

/// <summary>Filtros para sp_SesionTelemedica_Listar.</summary>
public sealed class SesionListarFiltrosDto
{
    /// <summary>PROGRAMADA | ACTIVA | FINALIZADA | NO_INICIADA | CANCELADA</summary>
    public string? Estado { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}
