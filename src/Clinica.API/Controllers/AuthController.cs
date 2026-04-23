using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

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
            estado = "pendiente",
            mensaje = "El modulo de autenticacion aun no esta implementado."
        });
    }
}