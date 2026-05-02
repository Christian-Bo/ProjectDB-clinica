using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Citas;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
public sealed class CitasController : ControllerBase
{
    private readonly ICitasService _service;

    public CitasController(ICitasService service)
    {
        _service = service;
    }

    // POST /api/reservar/cita
    [HttpPost("api/reservar/cita")]
    public async Task<IActionResult> Reservar(
        [FromBody] ReservarCitaRequestDto dto,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey)
    {
        var key = idempotencyKey ?? Guid.NewGuid();
        var cita = await _service.ReservarAsync(dto, key, 1);
        return CreatedAtAction(nameof(Obtener), new { citaId = cita.CitaId },
            new { success = true, message = "Cita reservada correctamente.", data = cita });
    }

    // POST /api/reservar/cita/{citaId}/confirmar
    [HttpPost("api/reservar/cita/{citaId:long}/confirmar")]
    public async Task<IActionResult> Confirmar(
        long citaId,
        [FromBody] ConfirmarCitaRequestDto dto,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey)
    {
        var key = idempotencyKey ?? Guid.NewGuid();
        var cita = await _service.ConfirmarAsync(citaId, dto, key);
        return Ok(new { success = true, message = "Cita confirmada correctamente.", data = cita });
    }

    // POST /api/citas/{citaId}/cancelar
    [HttpPost("api/citas/{citaId:long}/cancelar")]
    public async Task<IActionResult> Cancelar(long citaId, [FromBody] CancelarCitaRequestDto dto)
    {
        await _service.CancelarAsync(citaId, dto);
        return Ok(new { success = true, message = "Cita cancelada correctamente." });
    }

    // POST /api/citas/{citaId}/reprogramar
    [HttpPost("api/citas/{citaId:long}/reprogramar")]
    public async Task<IActionResult> Reprogramar(long citaId, [FromBody] ReprogramarCitaRequestDto dto)
    {
        await _service.ReprogramarAsync(citaId, dto);
        return Ok(new { success = true, message = "Cita reprogramada correctamente." });
    }

    // GET /api/citas/{citaId}
    [HttpGet("api/citas/{citaId:long}")]
    public async Task<IActionResult> Obtener(long citaId)
    {
        var cita = await _service.ObtenerAsync(citaId);
        return Ok(new { success = true, data = cita });
    }

    // GET /api/citas
    [HttpGet("api/citas")]
    public async Task<IActionResult> Listar([FromQuery] ListarCitasRequestDto filtros)
    {
        var citas = await _service.ListarAsync(filtros);
        return Ok(new { success = true, data = citas });
    }
}