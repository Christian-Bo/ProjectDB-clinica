using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Catalogos;
using Clinica.Application.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

/// <summary>
/// Módulo 3 — Catálogos de recepción.
/// Provee los combos necesarios para la UI: sedes, servicios, estaciones, pacientes y citas.
/// </summary>
[ApiController]
[Route("api/recepcion/catalogos")]
[Produces("application/json")]
public sealed class RecepcionCatalogosController(ICatalogosRecepcionService service) : ControllerBase
{
    [HttpGet("sedes")]
    public async Task<IActionResult> Sedes(CancellationToken ct)
    {
        var items = await service.ListarSedesAsync(ct);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Success(items));
    }

    [HttpGet("servicios")]
    public async Task<IActionResult> Servicios([FromQuery] int? sedeId, CancellationToken ct)
    {
        var items = await service.ListarServiciosAsync(sedeId, ct);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Success(items));
    }

    [HttpGet("estaciones")]
    public async Task<IActionResult> Estaciones([FromQuery] int? sedeId, CancellationToken ct)
    {
        var items = await service.ListarEstacionesAsync(sedeId, ct);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Success(items));
    }

    [HttpGet("pacientes")]
    public async Task<IActionResult> Pacientes(
        [FromQuery] string? texto,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var items = await service.ListarPacientesAsync(texto, Math.Clamp(limit, 1, 500), ct);
        return Ok(ApiResponse<List<PacienteItemDto>>.Success(items));
    }

    [HttpGet("citas-confirmadas")]
    public async Task<IActionResult> CitasConfirmadas(
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] string? texto,
        CancellationToken ct)
    {
        var items = await service.ListarCitasConfirmadasAsync(sedeId, servicioId, texto, ct);
        return Ok(ApiResponse<List<CitaItemDto>>.Success(items));
    }

    [HttpGet("prioridades-ticket")]
    public async Task<IActionResult> PrioridadesTicket(CancellationToken ct)
    {
        var items = await service.ListarPrioridadesTicketAsync(ct);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Success(items));
    }

    [HttpGet("estados-ticket")]
    public async Task<IActionResult> EstadosTicket(CancellationToken ct)
    {
        var items = await service.ListarEstadosTicketAsync(ct);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Success(items));
    }

    [HttpGet("kiosco/ventanillas")]
    public async Task<IActionResult> KioscoVentanillas([FromQuery] int sedeId, CancellationToken ct)
    {
        var items = await service.ListarKioscoVentanillasAsync(sedeId, ct);
        return Ok(ApiResponse<List<KioscoVentanillaDto>>.Success(items));
    }

    [HttpPost("kiosco/ventanillas")]
    public async Task<IActionResult> ConfigurarKioscoVentanilla(
        [FromBody] KioscoVentanillaConfigRequest request,
        CancellationToken ct)
    {
        var item = await service.ConfigurarKioscoVentanillaAsync(request, ct);
        return Ok(ApiResponse<KioscoVentanillaDto>.Success(item, "Ventanilla configurada."));
    }
}
