using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Citas;
using Clinica.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/secretaria/citas")]
[Produces("application/json")]
public sealed class SecretariaCitasController : ControllerBase
{
    private readonly ICitasService _service;

    public SecretariaCitasController(ICitasService service)
    {
        _service = service;
    }

    [HttpGet("pendientes")]
    public async Task<IActionResult> Pendientes(
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        var citas = await _service.ListarAsync(new ListarCitasRequestDto
        {
            SedeId = sedeId,
            ServicioId = servicioId,
            Estado = "PENDIENTE",
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });

        return Ok(ApiResponse<List<CitaResponseDto>>.Success(citas, "Citas pendientes listadas."));
    }

    [HttpPost("{citaId:long}/confirmar")]
    public async Task<IActionResult> Confirmar(
        long citaId,
        [FromBody] ConfirmarCitaRequestDto dto,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey)
    {
        var key = idempotencyKey ?? Guid.NewGuid();
        var cita = await _service.ConfirmarAsync(citaId, dto, key);
        return Ok(ApiResponse<CitaResponseDto>.Success(cita, "Cita confirmada por secretaría."));
    }
}
