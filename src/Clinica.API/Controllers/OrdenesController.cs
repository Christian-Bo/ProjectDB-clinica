using Clinica.Application.Contracts;
using Clinica.Application.Models.Ordenes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// -----------------------------------------------------------------------------
// Módulo Dev4 — Órdenes de laboratorio e imagen.
// Cada cambio de estado queda registrado en OrdenesHistorial (trazabilidad).
// El SP sp_ActualizarEstadoOrden maneja las transiciones válidas de estado.
// -----------------------------------------------------------------------------
[AllowAnonymous]
[Route("api/ordenes")]
public sealed class OrdenesController : BaseController
{
    private readonly IOrdenesService _ordenesService;

    public OrdenesController(IOrdenesService ordenesService)
    {
        _ordenesService = ordenesService;
    }

    // -------------------------------------------------------------------------
    // Crea una orden de laboratorio o imagen desde una consulta.
    // -------------------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearOrdenRequestDto request,
        CancellationToken cancellationToken)
    {
        request.UsuarioId = ResolveUserId(request.UsuarioId);

        var result = await _ordenesService.CrearAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    // -------------------------------------------------------------------------
    // Lista órdenes con filtros opcionales.
    // -------------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] OrdenListFiltersDto filters,
        CancellationToken cancellationToken)
    {
        var result = await _ordenesService.ListarAsync(filters, cancellationToken);
        return ToActionResult(result);
    }

    // -------------------------------------------------------------------------
    // Obtiene una orden con su historial de cambios de estado.
    // -------------------------------------------------------------------------
    [HttpGet("{ordenId:long}")]
    public async Task<IActionResult> Obtener(long ordenId, CancellationToken cancellationToken)
    {
        var result = await _ordenesService.ObtenerAsync(ordenId, cancellationToken);
        return ToActionResult(result);
    }

    // -------------------------------------------------------------------------
    // Actualiza el estado de una orden. Cada cambio queda en OrdenesHistorial.
    // -------------------------------------------------------------------------
    [HttpPatch("{ordenId:long}/estado")]
    public async Task<IActionResult> ActualizarEstado(
        long ordenId,
        [FromBody] ActualizarEstadoOrdenRequestDto request,
        CancellationToken cancellationToken)
    {
        request.UsuarioId = ResolveUserId(request.UsuarioId);

        var result = await _ordenesService.ActualizarEstadoAsync(ordenId, request, cancellationToken);
        return ToActionResult(result);
    }
}
