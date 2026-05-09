using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.Models.Compras;

// =============================================================================
// DTOs — Compras (Proveedores + Órdenes de Compra)
// Tablas reales: dbo.Proveedores | dbo.OrdenesCompra | dbo.OrdenesCompraDetalle
// SPs: sp_Proveedor_* | sp_OrdenCompra_*
// =============================================================================

// ---------------------------------------------------------------------------
// Proveedores
// ---------------------------------------------------------------------------

/// <summary>Crear o actualizar proveedor. Basado en sp_Proveedor_Upsert.</summary>
public sealed class ProveedorUpsertDto
{
    public int? ProveedorId { get; set; }

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? NIT { get; set; }

    [MaxLength(100)]
    public string? Contacto { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(200)]
    public string? CorreoElectronico { get; set; }

    [MaxLength(300)]
    public string? Direccion { get; set; }

    /// <summary>ACTIVO | INACTIVO | SUSPENDIDO</summary>
    [MaxLength(20)]
    public string Estado { get; set; } = "ACTIVO";
}

/// <summary>Respuesta de lectura desde dbo.Proveedores.</summary>
public sealed class ProveedorDto
{
    public int ProveedorId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? NIT { get; init; }
    public string? Contacto { get; init; }
    public string? Telefono { get; init; }
    public string? CorreoElectronico { get; init; }
    public string? Direccion { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaRegistro { get; init; }
}

/// <summary>Filtros para sp_Proveedor_Listar.</summary>
public sealed class ProveedorListarFiltrosDto
{
    /// <summary>ACTIVO | INACTIVO | SUSPENDIDO — null devuelve todos</summary>
    public string? Estado { get; set; }

    /// <summary>Busca en Nombre y NIT</summary>
    public string? Texto { get; set; }
}

// ---------------------------------------------------------------------------
// Órdenes de Compra
// ---------------------------------------------------------------------------

/// <summary>
/// Crear una orden de compra. Basado en sp_OrdenCompra_Crear.
/// NumeroOrden es obligatorio y único en la BD.
/// </summary>
public sealed class OrdenCompraCrearDto
{
    [Required]
    public int ProveedorId { get; set; }

    /// <summary>Número único de la orden. Ej: OC-2026-0001</summary>
    [Required, MaxLength(30)]
    public string NumeroOrden { get; set; } = string.Empty;

    public DateTime? FechaEmision { get; set; }

    public DateTime? FechaEntregaPact { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    /// <summary>Id del usuario que crea la orden. Obligatorio en el SP.</summary>
    [Required]
    public int CreadoPor { get; set; }
}

/// <summary>
/// Agregar un ítem a una orden. Basado en sp_OrdenCompra_AgregarDetalle.
/// </summary>
public sealed class OrdenCompraAgregarDetalleDto
{
    [Required]
    public int MedicamentoId { get; set; }

    [Required]
    public decimal CantidadSolicitada { get; set; }

    [Required]
    public decimal PrecioUnitario { get; set; }

    public DateTime? FechaVencimientoLote { get; set; }

    [MaxLength(50)]
    public string? LoteProveedor { get; set; }
}

/// <summary>
/// Cambiar estado de una orden. Basado en sp_OrdenCompra_ActualizarEstado.
/// Estados válidos: BORRADOR | APROBADA | ENVIADA | RECIBIDA_PARCIAL | RECIBIDA | CANCELADA
/// </summary>
public sealed class OrdenCompraActualizarEstadoDto
{
    [Required, MaxLength(20)]
    public string Estado { get; set; } = string.Empty;

    /// <summary>Requerido cuando Estado = APROBADA o ENVIADA</summary>
    public int? AprobadoPor { get; set; }
}

/// <summary>
/// Registrar recepción de UN ítem específico. Basado en sp_OrdenCompra_RegistrarRecepcion.
/// IMPORTANTE: el SP recibe OrdenCompraDetalleId (no OrdenCompraId).
/// El SP internamente llama a sp_RegistrarMovimientoInventario con ENTRADA.
/// </summary>
public sealed class OrdenCompraRegistrarRecepcionDto
{
    /// <summary>Id del ítem/detalle específico a recepcionar (dbo.OrdenesCompraDetalle).</summary>
    [Required]
    public long OrdenCompraDetalleId { get; set; }

    [Required]
    public decimal CantidadRecibida { get; set; }

    public DateTime? FechaVencimientoLote { get; set; }

    [MaxLength(30)]
    public string? CodigoLote { get; set; }

    [Required]
    public int UsuarioId { get; set; }
}

/// <summary>Encabezado de una orden de compra (primer result set de sp_OrdenCompra_Obtener).</summary>
public sealed class OrdenCompraDto
{
    public int OrdenCompraId { get; init; }
    public int ProveedorId { get; init; }
    public string ProveedorNombre { get; init; } = string.Empty;
    public string NumeroOrden { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaEmision { get; init; }
    public DateTime? FechaEntregaPact { get; init; }
    public DateTime? FechaRecepcion { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Impuesto { get; init; }
    public decimal Total { get; init; }
    public string? Observaciones { get; init; }
    public DateTime FechaCreacion { get; init; }
    public IReadOnlyList<OrdenCompraDetalleDto> Detalles { get; init; } = Array.Empty<OrdenCompraDetalleDto>();
}

/// <summary>Ítem de detalle (segundo result set de sp_OrdenCompra_Obtener).</summary>
public sealed class OrdenCompraDetalleDto
{
    public long OrdenCompraDetalleId { get; init; }
    public int OrdenCompraId { get; init; }
    public int MedicamentoId { get; init; }
    public string MedicamentoNombre { get; init; } = string.Empty;
    public decimal CantidadSolicitada { get; init; }
    public decimal CantidadRecibida { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal SubtotalLinea { get; init; }
    public DateTime? FechaVencimientoLote { get; init; }
    public string? LoteProveedor { get; init; }
}

/// <summary>Filtros para sp_OrdenCompra_Listar.</summary>
public sealed class OrdenCompraListarFiltrosDto
{
    public int? ProveedorId { get; set; }
    public string? Estado { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}
