using System.Security.Claims;
using Clinica.Application.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// -----------------------------------------------------------------------------
// Controlador base.
// Aqui centralizamos helpers utiles para todos los controladores:
// - Leer Idempotency-Key.
// - Resolver UsuarioId desde body, claim o header de pruebas.
// - Enviar respuestas uniformes: ok, code, message, data.
// -----------------------------------------------------------------------------
[ApiController]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected bool TryGetIdempotencyKey(out Guid? idempotencyKey, out IActionResult? errorResult)
    {
        idempotencyKey = null;
        errorResult = null;

        if (!Request.Headers.TryGetValue("Idempotency-Key", out var rawHeader) || string.IsNullOrWhiteSpace(rawHeader))
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
    // Mientras el modulo de autenticacion no este completo, permitimos resolver
    // el usuario desde:
    // 1) el valor que venga en el body,
    // 2) el claim NameIdentifier,
    // 3) el header X-Usuario-Id.
    // Esto facilita pruebas manuales y Postman sin bloquear al equipo.
    // -------------------------------------------------------------------------
    protected int? ResolveUserId(int? bodyUserId = null)
    {
        if (bodyUserId.HasValue)
        {
            return bodyUserId;
        }

        var fromClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("usuarioId");

        if (int.TryParse(fromClaim, out var claimUserId))
        {
            return claimUserId;
        }

        if (Request.Headers.TryGetValue("X-Usuario-Id", out var rawHeader)
            && int.TryParse(rawHeader.ToString(), out var headerUserId))
        {
            return headerUserId;
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
