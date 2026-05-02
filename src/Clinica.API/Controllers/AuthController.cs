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
            return BadRequest(new { success = false, message = "Datos invalidos." });

        var (success, errorCode, message, data) = await _authService.LoginAsync(dto);

        if (!success)
            return Unauthorized(new { success = false, errorCode, message });

        return Ok(new { success = true, message, data });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var claim = User.FindFirstValue("usuarioId");
        if (!int.TryParse(claim, out var usuarioId))
            return Unauthorized(new { success = false, message = "Token invalido." });

        var (success, data) = await _authService.GetMeAsync(usuarioId);
        if (!success)
            return NotFound(new { success = false, message = "Usuario no encontrado." });

        return Ok(new { success = true, data });
    }

    // Dev1 — Registro de usuarios administrativos
    [HttpPost("registro")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Registro([FromBody] RegistroUsuarioRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Datos invalidos." });

        var (success, errorCode, message, data) = await _authService.RegistrarUsuarioAsync(dto);

        if (!success)
            return BadRequest(new { success = false, errorCode, message });

        return StatusCode(201, new { success = true, message, data });
    }

    // Dev2 — Registro de pacientes
    [HttpPost("registro-paciente")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistroPaciente([FromBody] RegistroRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Datos invalidos." });

        var (success, errorCode, message, data) = await _authService.RegistrarPacienteAsync(dto);

        if (!success)
        {
            var statusCode = errorCode is "CORREO_DUPLICADO" or "DOCUMENTO_DUPLICADO" ? 409 : 422;
            return StatusCode(statusCode, new { success = false, errorCode, message });
        }

        return StatusCode(201, new { success = true, message, data });
    }
}