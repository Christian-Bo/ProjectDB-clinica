using System.Security.Claims;
using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Pacientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/pacientes")]
public sealed class PacientesController : ControllerBase
{
    private readonly IPacientesService _service;

    public PacientesController(IPacientesService service)
    {
        _service = service;
    }

    // GET /api/pacientes/yo
    [HttpGet("yo")]
    [Authorize]
    public async Task<IActionResult> Yo()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "usuarioId")?.Value;
        if (!int.TryParse(claim, out var usuarioId))
            return Unauthorized(new { success = false, message = "Token invalido." });

        var paciente = await _service.ObtenerPorUsuarioAsync(usuarioId);
        if (paciente is null)
            return NotFound(new { success = false, message = "Paciente no encontrado." });

        return Ok(new { success = true, data = paciente });
    }

    // GET /api/pacientes/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var paciente = await _service.ObtenerAsync(id);
        return Ok(new { success = true, data = paciente });
    }

    // PUT /api/pacientes/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] PacienteUpsertDto dto)
    {
        dto.PacienteId = id;
        var paciente = await _service.UpsertAsync(dto);
        return Ok(new { success = true, message = "Paciente actualizado correctamente.", data = paciente });
    }

    // POST /api/pacientes
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] PacienteUpsertDto dto)
    {
        var paciente = await _service.UpsertAsync(dto);
        return CreatedAtAction(nameof(Obtener), new { id = paciente.PacienteId },
            new { success = true, message = "Paciente creado correctamente.", data = paciente });
    }

    // GET /api/pacientes/{id}/alergias
    [HttpGet("{id:int}/alergias")]
    public async Task<IActionResult> ListarAlergias(int id)
    {
        var alergias = await _service.ListarAlergiasAsync(id);
        return Ok(new { success = true, data = alergias });
    }

    // POST /api/pacientes/{id}/alergias
    [HttpPost("{id:int}/alergias")]
    public async Task<IActionResult> AgregarAlergia(int id, [FromBody] AlergiaRequestDto dto)
    {
        await _service.AgregarAlergiaAsync(id, dto);
        return Ok(new { success = true, message = "Alergia agregada correctamente." });
    }

    // POST /api/pacientes/{id}/alergias/{alergiaId}/quitar
    [HttpPost("{id:int}/alergias/{alergiaId:int}/quitar")]
    public async Task<IActionResult> QuitarAlergia(int id, int alergiaId)
    {
        await _service.QuitarAlergiaAsync(id, alergiaId);
        return Ok(new { success = true, message = "Alergia desactivada correctamente." });
    }
}