using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.Models.Inventario;

// =============================================================================
// DTOs — Inventario / Movimientos
// Tabla real: dbo.MovimientosInventario
// SP: sp_RegistrarMovimientoInventario
// =============================================================================

/// <summary>
/// Registrar un movimiento de inventario (KARDEX).
/// Parámetros 1:1 con sp_RegistrarMovimientoInventario.
/// </summary>
public sealed class RegistrarMovimientoRequestDto
{
    [Required]
    public int MedicamentoId { get; set; }

    /// <summary>ENTRADA | SALIDA | AJUSTE | DEVOLUCION | VENCIMIENTO</summary>
    [Required, MaxLength(20)]
    public string TipoMovimiento { get; set; } = string.Empty;

    [Required]
    public decimal Cantidad { get; set; }

    /// <summary>RECETA | ORDEN_COMPRA | MANUAL — origen del movimiento</summary>
    [MaxLength(30)]
    public string? OrigenTipo { get; set; }

    /// <summary>Id de la entidad origen (RecetaId, OrdenCompraId, etc.)</summary>
    public long? OrigenId { get; set; }

    public long? RecetaDetalleId { get; set; }

    public decimal? Costo { get; set; }

    public decimal? PrecioUnitario { get; set; }

    [MaxLength(100)]
    public string? Referencia { get; set; }

    [MaxLength(300)]
    public string? Observaciones { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    public int? LoteId { get; set; }
}

/// <summary>Respuesta tras registrar un movimiento de inventario.</summary>
public sealed class MovimientoInventarioDto
{
    public long MovimientoId { get; init; }
    public int MedicamentoId { get; init; }
    public string TipoMovimiento { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal StockResultante { get; init; }
}
