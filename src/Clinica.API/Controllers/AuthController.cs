using System.Security.Claims;
using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { ok = false, message = "Datos invalidos." });

        var (success, errorCode, message, data) = await _authService.LoginAsync(dto);

        if (!success)
            return Unauthorized(new { ok = false, errorCode, message });

        return Ok(new { ok = true, message, data });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var claim = User.FindFirstValue("usuarioId");
        if (!int.TryParse(claim, out var usuarioId))
            return Unauthorized(new { ok = false, message = "Token invalido." });

        var (success, data) = await _authService.GetMeAsync(usuarioId);
        if (!success)
            return NotFound(new { ok = false, message = "Usuario no encontrado." });

        return Ok(new { ok = true, data });
    }

    [HttpPost("generar-hash")]
    [AllowAnonymous]
    public IActionResult GenerarHash([FromBody] string password)
    {
        var hasher = new Clinica.Infrastructure.Security.PasswordHasher();
        return Ok(new { hash = hasher.Hash(password) });
    }
}