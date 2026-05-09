using System.Security.Claims;
using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Auth;
using Clinica.Application.DTOs.Common;
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
            return BadRequest(ApiResponse<object>.Fail("Datos invalidos.", "DATOS_INVALIDOS"));

        var (ok, errorCode, message, data) = await _authService.LoginAsync(dto);

        if (!ok)
            return Unauthorized(ApiResponse<object>.Fail(message, errorCode ?? "ERROR"));

        return Ok(ApiResponse<object>.Success(data!, message));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var claim = User.FindFirstValue("usuarioId");
        if (!int.TryParse(claim, out var usuarioId))
            return Unauthorized(ApiResponse<object>.Fail("Token invalido.", "TOKEN_INVALIDO"));

        var (ok, data) = await _authService.GetMeAsync(usuarioId);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Usuario no encontrado.", "NO_ENCONTRADO"));

        return Ok(ApiResponse<object>.Success(data!));
    }

    [HttpPost("registro")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Registro([FromBody] RegistroUsuarioRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Datos invalidos.", "DATOS_INVALIDOS"));

        var (ok, errorCode, message, data) = await _authService.RegistrarUsuarioAsync(dto);

        if (!ok)
            return BadRequest(ApiResponse<object>.Fail(message, errorCode ?? "ERROR"));

        return StatusCode(201, ApiResponse<object>.Success(data!, message));
    }

    [HttpPost("registro-paciente")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistroPaciente([FromBody] RegistroRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Datos invalidos.", "DATOS_INVALIDOS"));

        var (ok, errorCode, message, data) = await _authService.RegistrarPacienteAsync(dto);

        if (!ok)
        {
            var status = errorCode is "CORREO_DUPLICADO" or "DOCUMENTO_DUPLICADO" ? 409 : 422;
            return StatusCode(status, ApiResponse<object>.Fail(message, errorCode ?? "ERROR"));
        }

        return StatusCode(201, ApiResponse<object>.Success(data!, message));
    }
}