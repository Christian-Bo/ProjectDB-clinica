using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.Models.Notificaciones;

// =============================================================================
// DTOs — Notificaciones (Plantillas + Cola)
// Tablas reales: dbo.PlantillasNotificacion | dbo.ColaNotificaciones
// SPs: sp_PlantillaNotificacion_* | sp_ColaNotificacion_* | sp_ProcesarColaNotificaciones
// =============================================================================

// ---------------------------------------------------------------------------
// Plantillas
// ---------------------------------------------------------------------------

/// <summary>
/// Crear o actualizar plantilla. Basado en sp_PlantillaNotificacion_Upsert.
/// La BD tiene UNIQUE (TipoEvento, Canal) — no se puede repetir la combinación.
/// </summary>
public sealed class PlantillaNotificacionUpsertDto
{
    public int? PlantillaId { get; set; }

    /// <summary>Ej: CONFIRMACION_CITA, RECORDATORIO_CITA, TURNO_LLAMADO</summary>
    [Required, MaxLength(80)]
    public string TipoEvento { get; set; } = string.Empty;

    /// <summary>EMAIL | WHATSAPP | SMS | PUSH | SISTEMA</summary>
    [Required, MaxLength(20)]
    public string Canal { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Asunto { get; set; }

    [Required]
    public string Cuerpo { get; set; } = string.Empty;

    /// <summary>JSON que documenta las variables de la plantilla. Ej: {"nombre":"string"}</summary>
    public string? VariablesJSON { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime? FechaModificacion { get; set; }
}

/// <summary>Respuesta de lectura desde dbo.PlantillasNotificacion.</summary>
public sealed class PlantillaNotificacionDto
{
    public int PlantillaId { get; init; }
    public string TipoEvento { get; init; } = string.Empty;
    public string Canal { get; init; } = string.Empty;
    public string? Asunto { get; init; }
    public string Cuerpo { get; init; } = string.Empty;
    public string? VariablesJSON { get; init; }
    public bool Activo { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaModificacion { get; init; }
}

/// <summary>Filtros para sp_PlantillaNotificacion_Listar.</summary>
public sealed class PlantillaListarFiltrosDto
{
    public string? TipoEvento { get; set; }
    public string? Canal { get; set; }
    public bool? Activo { get; set; }
}

// ---------------------------------------------------------------------------
// Cola de notificaciones
// ---------------------------------------------------------------------------

/// <summary>
/// Encolar una notificación. Basado en sp_ColaNotificacion_Encolar.
/// El cuerpo se envía ya renderizado (el backend aplica las variables antes de encolar).
/// </summary>
public sealed class EncolarNotificacionRequestDto
{
    public int? PacienteId { get; set; }
    public int? UsuarioId { get; set; }

    /// <summary>Ej: CONFIRMACION_CITA</summary>
    [Required, MaxLength(80)]
    public string TipoEvento { get; set; } = string.Empty;

    /// <summary>EMAIL | WHATSAPP | SMS | PUSH | SISTEMA</summary>
    [Required, MaxLength(20)]
    public string Canal { get; set; } = string.Empty;

    /// <summary>Email, número de teléfono o token push.</summary>
    [Required, MaxLength(200)]
    public string Destinatario { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Asunto { get; set; }

    [Required]
    public string Cuerpo { get; set; } = string.Empty;

    [Required]
    public DateTime FechaProgramada { get; set; }

    public byte MaxIntentos { get; set; } = 3;

    /// <summary>JSON con datos extra para trazabilidad.</summary>
    public string? MetadatosJSON { get; set; }
}

/// <summary>Notificación pendiente desde sp_ColaNotificacion_ListarPendientes.</summary>
public sealed class NotificacionPendienteDto
{
    public long NotificacionId { get; init; }
    public int? PacienteId { get; init; }
    public string TipoEvento { get; init; } = string.Empty;
    public string Canal { get; init; } = string.Empty;
    public string Destinatario { get; init; } = string.Empty;
    public string? Asunto { get; init; }
    public string Estado { get; init; } = string.Empty;
    public byte Intentos { get; init; }
    public byte MaxIntentos { get; init; }
    public DateTime FechaProgramada { get; init; }
    public DateTime FechaCreacion { get; init; }
}

/// <summary>Filtros para sp_ColaNotificacion_ListarPendientes.</summary>
public sealed class ColaListarFiltrosDto
{
    public string? Canal { get; set; }
    public int MaxRegistros { get; set; } = 100;
}

/// <summary>Resultado de sp_ProcesarColaNotificaciones.</summary>
public sealed class ProcesarColaResultDto
{
    public int RegistrosProcesados { get; init; }
}
