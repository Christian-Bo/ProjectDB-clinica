using Clinica.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// RecetasDespachoController
// POST /api/recetas/{recetaId}/despachar
//
// Solo gestiona el DESPACHO desde farmacia.
// La CREACIÓN de recetas es responsabilidad del módulo clínico (Dev 4).
// El SP sp_DespacharReceta valida stock, registra SALIDA de inventario
// por cada medicamento y marca la receta como DESPACHADA en una transacción.
// =============================================================================
[Route("api/recetas")]
public sealed class RecetasDespachoController : BaseController
{
    private readonly IFarmaciaService _svc;
    public RecetasDespachoController(IFarmaciaService svc) => _svc = svc;

    /// <summary>
    /// Despacha una receta desde farmacia.
    /// Requiere: X-Usuario-Id en header o autenticación JWT.
    /// </summary>
    [HttpPost("{recetaId:long}/despachar")]
    public async Task<IActionResult> Despachar(
        long recetaId,
        [FromQuery] string? observaciones,
        CancellationToken ct)
    {
        var usuarioId = ResolveUserId();
        if (!usuarioId.HasValue)
            return BadRequest(new { ok = false, code = "USUARIO_REQUERIDO",
                message = "Se requiere UsuarioId para despachar una receta." });

        return ToActionResult(
            await _svc.DespacharRecetaAsync(recetaId, usuarioId.Value, observaciones, ct));
    }
}
