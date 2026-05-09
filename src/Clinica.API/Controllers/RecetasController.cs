using Clinica.Application.Contracts;
using Clinica.Application.Models.Recetas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// -----------------------------------------------------------------------------
// Módulo Dev4 — Recetas.
// Las recetas solo se pueden crear desde una consulta cerrada.
// El SP valida alergias y principio activo duplicado; si hay conflicto
// devuelve un código 409/422 que el frontend debe mostrar como alerta roja.
// -----------------------------------------------------------------------------
[AllowAnonymous]
[Route("api/recetas")]
public sealed class RecetasController : BaseController
{
    private readonly IRecetasService _recetasService;

    public RecetasController(IRecetasService recetasService)
    {
        _recetasService = recetasService;
    }

    // -------------------------------------------------------------------------
    // Crea una receta a partir de una consulta cerrada.
    // Si hay alergia o principio activo duplicado, el SP devuelve error clínico.
    // -------------------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearRecetaRequestDto request,
        CancellationToken cancellationToken)
    {
        request.UsuarioId = ResolveUserId(request.UsuarioId);

        var result = await _recetasService.CrearAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    // -------------------------------------------------------------------------
    // Obtiene una receta con todos sus items y datos del paciente/médico.
    // -------------------------------------------------------------------------
    [HttpGet("{recetaId:long}")]
    public async Task<IActionResult> Obtener(long recetaId, CancellationToken cancellationToken)
    {
        var result = await _recetasService.ObtenerAsync(recetaId, cancellationToken);
        return ToActionResult(result);
    }
}
