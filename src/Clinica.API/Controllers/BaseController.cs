using System.Security.Claims;
using Clinica.Application.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// -----------------------------------------------------------------------------
// Controlador base.
// Aqui centralizamos helpers utiles para todos los controladores:
// - Leer Idempotency-Key.
// - Resolver UsuarioId desde claims autenticados o fallback de pruebas.
// - Enviar respuestas uniformes: ok, code, message y data.
// -----------------------------------------------------------------------------
[ApiController]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected bool TryGetIdempotencyKey(out Guid? idempotencyKey, out IActionResult? errorResult)
    {
        idempotencyKey = null;
        errorResult = null;

        if (!Request.Headers.TryGetValue("Idempotency-Key", out var rawHeader) ||
            string.IsNullOrWhiteSpace(rawHeader))
        {
            return true;
        }

        if (!Guid.TryParse(rawHeader.ToString(), out var parsed))
        {
            errorResult = BadRequest(new
            {
                ok = false,
                code = "IDEMPOTENCY_KEY_INVALIDA",
                message = "El header Idempotency-Key debe ser un GUID valido."
            });
            return false;
        }

        idempotencyKey = parsed;
        return true;
    }

    // -------------------------------------------------------------------------
    // Regla de seguridad:
    // 1) Si el usuario esta autenticado, se confia primero en sus claims.
    // 2) Solo si NO esta autenticado, se permite fallback desde body o header
    //    para pruebas manuales en Postman mientras el modulo auth no esta listo.
    // -------------------------------------------------------------------------
    protected int? ResolveUserId(int? bodyUserId = null)
    {
        var isAuthenticated = User?.Identity?.IsAuthenticated == true;

        var fromClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub")
            ?? User?.FindFirstValue("usuarioId");

        if (int.TryParse(fromClaim, out var claimUserId) && claimUserId > 0)
        {
            return claimUserId;
        }

        if (!isAuthenticated)
        {
            if (bodyUserId.HasValue && bodyUserId.Value > 0)
            {
                return bodyUserId.Value;
            }

            if (Request.Headers.TryGetValue("X-Usuario-Id", out var rawHeader) &&
                int.TryParse(rawHeader.ToString(), out var headerUserId) &&
                headerUserId > 0)
            {
                return headerUserId;
            }
        }

        return null;
    }

    protected ActionResult ToActionResult<T>(ServiceOperationResult<T> result)
    {
        return StatusCode(result.HttpStatus, new
        {
            ok = result.Success,
            code = result.Code,
            message = result.Message,
            data = result.Data
        });
    }
}