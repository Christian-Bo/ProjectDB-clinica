using Clinica.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// -----------------------------------------------------------------------------
// Endpoints de apoyo para formularios y combos del frontend.
// La idea es que el usuario vea listas claras con nombres y etiquetas amigables,
// en lugar de trabajar manualmente con ids sueltos.
// -----------------------------------------------------------------------------
[AllowAnonymous]
[Route("api/recepcion/catalogos")]
public sealed class RecepcionCatalogosController : BaseController
{
    private readonly ITicketQueueService _ticketQueueService;

    public RecepcionCatalogosController(ITicketQueueService ticketQueueService)
    {
        _ticketQueueService = ticketQueueService;
    }

    [HttpGet("sedes")]
    public async Task<IActionResult> Sedes(CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetSedesAsync(cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("servicios")]
    public async Task<IActionResult> Servicios([FromQuery] int? sedeId, CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetServiciosAsync(sedeId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("estaciones")]
    public async Task<IActionResult> Estaciones(
        [FromQuery] int? sedeId,
        [FromQuery] string? tipoEstacion,
        CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetStationsAsync(sedeId, tipoEstacion, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("pacientes")]
    public async Task<IActionResult> Pacientes([FromQuery] string? texto, CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetPatientsAsync(texto, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("citas-confirmadas")]
    public async Task<IActionResult> CitasConfirmadas(
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] string? texto,
        CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetConfirmedAppointmentsAsync(sedeId, servicioId, texto, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("prioridades-ticket")]
    public async Task<IActionResult> PrioridadesTicket(CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetTicketPrioritiesAsync(cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("estados-ticket")]
    public async Task<IActionResult> EstadosTicket(CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetTicketStatesAsync(cancellationToken);
        return ToActionResult(result);
    }
}
