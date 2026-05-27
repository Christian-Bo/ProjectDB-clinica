using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.Models.Cobros;

// =============================================================================
// DTOs — Cobros (Cuentas + Pagos)
// Tablas reales: dbo.Cuentas | dbo.CuentaDetalle | dbo.Pagos
// SPs: sp_GenerarCuentaDesdeCita (TVP) | sp_RegistrarPagoCuenta
// =============================================================================

// ---------------------------------------------------------------------------
// Cuentas
// ---------------------------------------------------------------------------

/// <summary>
/// Generar cuenta desde una cita.
/// Usa TVP dbo.TVP_DetallesCuenta internamente.
/// Columnas del TVP: TipoConcepto NVARCHAR(20), Descripcion NVARCHAR(200),
///                  Cantidad DECIMAL(8,2), PrecioUnitario DECIMAL(10,2)
/// </summary>
public sealed class GenerarCuentaRequestDto
{
    [Required]
    public long CitaId { get; set; }

    public int? CreadoPor { get; set; }

    [Required, MinLength(1)]
    public List<DetalleCuentaItemDto> Detalles { get; set; } = new();
}

/// <summary>
/// Un ítem del TVP dbo.TVP_DetallesCuenta.
/// TipoConcepto válidos: CONSULTA | MEDICAMENTO | PROCEDIMIENTO | LAB | IMAGEN | OTRO
/// </summary>
public sealed class DetalleCuentaItemDto
{
    [Required, MaxLength(20)]
    public string TipoConcepto { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    public decimal Cantidad { get; set; } = 1;

    [Required]
    public decimal PrecioUnitario { get; set; }
}

/// <summary>Resumen de una cuenta generada (leído directamente de dbo.Cuentas).</summary>
public sealed class CuentaDto
{
    public long CuentaId { get; init; }
    public long CitaId { get; init; }
    public int PacienteId { get; init; }
    public string PacienteNombre { get; init; } = string.Empty;
    public int? TipoConsultaId { get; init; }
    public string? TipoConsultaNombre { get; init; }
    public decimal SubtotalConsulta { get; init; }
    public decimal SubtotalMedicamentos { get; init; }
    public decimal SubtotalProcedimientos { get; init; }
    public decimal Descuento { get; init; }
    public decimal Total { get; init; }
    public decimal Saldo { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaEmision { get; init; }
    public DateTime? FechaPago { get; init; }
    public string? Observaciones { get; init; }
}

public sealed class CuentaListarFiltrosDto
{
    public int? PacienteId { get; set; }
    public string? Estado { get; set; }
}

public sealed class CuentaDetalleLineaDto
{
    public long CuentaDetalleId { get; init; }
    public long CuentaId { get; init; }
    public string TipoConcepto { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal Subtotal { get; init; }
}

public sealed class CuentaDetalleDto
{
    public CuentaDto Cuenta { get; init; } = new();
    public IReadOnlyList<CuentaDetalleLineaDto> Detalle { get; init; } = Array.Empty<CuentaDetalleLineaDto>();
    public IReadOnlyList<PagoDto> Pagos { get; init; } = Array.Empty<PagoDto>();
}

// ---------------------------------------------------------------------------
// Pagos
// ---------------------------------------------------------------------------

/// <summary>
/// Registrar un pago. Basado en sp_RegistrarPagoCuenta.
/// Soporta Idempotency-Key via header (el controller lo extrae).
/// </summary>
public sealed class RegistrarPagoRequestDto
{
    [Required]
    public long CuentaId { get; set; }

    [Required]
    public int MetodoPagoId { get; set; }

    [Required]
    public decimal Monto { get; set; }

    [MaxLength(100)]
    public string? Referencia { get; set; }

    [MaxLength(500)]
    public string? ComprobanteUrl { get; set; }

    [MaxLength(300)]
    public string? Observaciones { get; set; }

    public int? RegistradoPor { get; set; }
}

/// <summary>Respuesta del pago registrado desde dbo.Pagos.</summary>
public sealed class PagoDto
{
    public long PagoId { get; init; }
    public long CuentaId { get; init; }
    public int MetodoPagoId { get; init; }
    public string? MetodoPagoNombre { get; init; }
    public decimal Monto { get; init; }
    public string? Referencia { get; init; }
    public DateTime FechaPago { get; init; }
    public bool Anulado { get; init; }
}

public sealed class MetodoPagoDto
{
    public int MetodoPagoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool RequiereReferencia { get; init; }
    public bool RequiereComprobante { get; init; }
    public bool Activo { get; init; }
}
