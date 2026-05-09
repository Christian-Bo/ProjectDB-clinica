using Clinica.Application.Contracts;
using Clinica.Application.Models.Inventario;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// MovimientosInventarioController
// POST /api/inventario/movimientos
//
// Registra ENTRADA, SALIDA, AJUSTE, DEVOLUCION o VENCIMIENTO en el KARDEX.
// La lógica de stock reside 100% en sp_RegistrarMovimientoInventario.
// Este controller NO calcula stock — solo pasa parámetros al SP.
// =============================================================================
[Route("api/inventario")]
public sealed class MovimientosInventarioController : BaseController
{
    private readonly IInventarioService _svc;
    public MovimientosInventarioController(IInventarioService svc) => _svc = svc;

    /// <summary>
    /// Registra un movimiento de inventario.
    /// TipoMovimiento válidos: ENTRADA | SALIDA | AJUSTE | DEVOLUCION | VENCIMIENTO
    /// UsuarioId es OBLIGATORIO en el SP.
    /// </summary>
    [HttpPost("movimientos")]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarMovimientoRequestDto dto, CancellationToken ct)
    {
        // UsuarioId es NOT NULL en el SP; si no viene en body, intentamos del claim
        if (dto.UsuarioId == 0)
        {
            var uid = ResolveUserId();
            if (!uid.HasValue)
                return BadRequest(new { ok = false, code = "USUARIO_REQUERIDO",
                    message = "UsuarioId es requerido para registrar movimientos de inventario." });
            dto.UsuarioId = uid.Value;
        }

        return ToActionResult(await _svc.RegistrarMovimientoAsync(dto, ct));
    }
}
