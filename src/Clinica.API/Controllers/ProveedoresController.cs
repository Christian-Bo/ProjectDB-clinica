using Clinica.Application.Contracts;
using Clinica.Application.Models.Compras;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// ProveedoresController
// GET  /api/proveedores        → listar
// GET  /api/proveedores/{id}   → obtener por ProveedorId
// POST /api/proveedores        → upsert
// =============================================================================
[Route("api/proveedores")]
public sealed class ProveedoresController : BaseController
{
    private readonly IProveedoresService _svc;
    public ProveedoresController(IProveedoresService svc) => _svc = svc;

    /// <summary>
    /// Lista proveedores.
    /// Filtros: ?estado=ACTIVO|INACTIVO|SUSPENDIDO  ?texto=farmacéutica
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estado,
        [FromQuery] string? texto,
        CancellationToken ct)
        => ToActionResult(await _svc.ListarProveedoresAsync(
            new ProveedorListarFiltrosDto { Estado = estado, Texto = texto }, ct));

    /// <summary>Obtiene un proveedor por su ProveedorId.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerProveedorAsync(id, ct));

    /// <summary>
    /// Crea (ProveedorId null) o actualiza (ProveedorId con valor) un proveedor.
    /// Estado válidos: ACTIVO | INACTIVO | SUSPENDIDO
    /// NIT debe ser único en la BD.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] ProveedorUpsertDto dto, CancellationToken ct)
        => ToActionResult(await _svc.UpsertProveedorAsync(dto, ct));
}
