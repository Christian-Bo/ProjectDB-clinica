using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// -----------------------------------------------------------------------------
// Controlador base de autenticacion.
// Se deja listo para que luego conecten login real con sus SPs de seguridad.
// -----------------------------------------------------------------------------
[Route("api/auth")]
public sealed class AuthController : BaseController
{
    [AllowAnonymous]
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            modulo = "auth",
            estado = "pendiente_de_implementar",
            mensaje = "La conexion a BD ya puede probarse desde /api/health/db"
        });
    }
}