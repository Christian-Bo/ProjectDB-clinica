using Clinica.Application.Contracts;
using Clinica.Application.Models.Compras;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// OrdenesCompraController
// GET   /api/compras                                → listar órdenes
// POST  /api/compras                                → crear orden (BORRADOR)
// GET   /api/compras/{ordenCompraId}                → obtener con detalles
// POST  /api/compras/{ordenCompraId}/detalle        → agregar ítem
// PATCH /api/compras/{ordenCompraId}/estado         → cambiar estado
// POST  /api/compras/recepcion                      → registrar recepción de ítem
//
// FLUJO NORMAL:
//   1. POST /api/compras                  → crea en BORRADOR
//   2. POST /api/compras/{id}/detalle     → agrega medicamentos
//   3. PATCH /api/compras/{id}/estado     → APROBADA → ENVIADA
//   4. POST /api/compras/recepcion        → por cada ítem recibido
//      (el SP llama internamente a sp_RegistrarMovimientoInventario ENTRADA)
// =============================================================================
[Route("api/compras")]
public sealed class OrdenesCompraController : BaseController
{
    private readonly IOrdenesCompraService _svc;
    public OrdenesCompraController(IOrdenesCompraService svc) => _svc = svc;

    /// <summary>
    /// Lista órdenes de compra.
    /// Filtros: ?proveedorId= ?estado=BORRADOR|APROBADA|ENVIADA|RECIBIDA_PARCIAL|RECIBIDA|CANCELADA
    ///          ?fechaDesde= ?fechaHasta=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? proveedorId,
        [FromQuery] string? estado,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken ct)
        => ToActionResult(await _svc.ListarOrdenesAsync(
            new OrdenCompraListarFiltrosDto
            {
                ProveedorId = proveedorId,
                Estado      = estado,
                FechaDesde  = fechaDesde,
                FechaHasta  = fechaHasta
            }, ct));

    /// <summary>
    /// Crea una orden de compra en estado BORRADOR.
    /// NumeroOrden debe ser único. CreadoPor es obligatorio en el SP.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] OrdenCompraCrearDto dto, CancellationToken ct)
    {
        if (dto.CreadoPor == 0)
            dto.CreadoPor = ResolveUserId() ?? 0;
        return ToActionResult(await _svc.CrearOrdenAsync(dto, ct));
    }

    /// <summary>Obtiene el detalle completo de una orden (encabezado + ítems).</summary>
    [HttpGet("{ordenCompraId:int}")]
    public async Task<IActionResult> Obtener(int ordenCompraId, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerOrdenAsync(ordenCompraId, ct));

    /// <summary>
    /// Agrega un medicamento a la orden.
    /// Solo funciona si la orden está en BORRADOR.
    /// </summary>
    [HttpPost("{ordenCompraId:int}/detalle")]
    public async Task<IActionResult> AgregarDetalle(
        int ordenCompraId,
        [FromBody] OrdenCompraAgregarDetalleDto dto,
        CancellationToken ct)
        => ToActionResult(await _svc.AgregarDetalleAsync(ordenCompraId, dto, ct));

    /// <summary>
    /// Cambia el estado de la orden.
    /// Estados válidos: BORRADOR → APROBADA → ENVIADA → RECIBIDA | CANCELADA
    /// AprobadoPor requerido cuando estado = APROBADA o ENVIADA.
    /// </summary>
    [HttpPatch("{ordenCompraId:int}/estado")]
    public async Task<IActionResult> ActualizarEstado(
        int ordenCompraId,
        [FromBody] OrdenCompraActualizarEstadoDto dto,
        CancellationToken ct)
    {
        if (dto.AprobadoPor is null)
            dto.AprobadoPor = ResolveUserId();

        return ToActionResult(await _svc.ActualizarEstadoAsync(ordenCompraId, dto, ct));
    }

    /// <summary>
    /// Registra la recepción física de UN ítem de la orden.
    /// IMPORTANTE: recibe OrdenCompraDetalleId (no OrdenCompraId).
    /// El SP registra automáticamente la ENTRADA en el inventario.
    /// Llamar una vez por cada ítem/línea recibida.
    /// </summary>
    [HttpPost("recepcion")]
    public async Task<IActionResult> RegistrarRecepcion(
        [FromBody] OrdenCompraRegistrarRecepcionDto dto, CancellationToken ct)
    {
        if (dto.UsuarioId == 0)
        {
            var uid = ResolveUserId();
            if (!uid.HasValue)
                return BadRequest(new { ok = false, code = "USUARIO_REQUERIDO",
                    message = "UsuarioId es requerido para registrar recepción." });
            dto.UsuarioId = uid.Value;
        }
        return ToActionResult(await _svc.RegistrarRecepcionAsync(dto, ct));
    }
}
