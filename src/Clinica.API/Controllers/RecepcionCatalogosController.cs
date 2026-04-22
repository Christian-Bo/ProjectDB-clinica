using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[Route("api/recepcion/catalogos")]
public sealed class RecepcionCatalogosController : BaseController
{
    private readonly ITicketQueueService _ticketQueueService;

    public RecepcionCatalogosController(ITicketQueueService ticketQueueService)
    {
        _ticketQueueService = ticketQueueService;
    }

    [AllowAnonymous]
    [HttpGet("sedes")]
    public async Task<IActionResult> Sedes(CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetSedesAsync(cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
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
    public async Task<IActionResult> Pacientes(
        [FromQuery] string? texto,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 100);
        var result = await _ticketQueueService.GetPatientsAsync(texto, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            return ToActionResult(result);
        }

        var limitedData = result.Data.Take(normalizedLimit).ToList();

        return ToActionResult(new ServiceOperationResult<IReadOnlyList<PatientSelectionDto>>
        {
            HttpStatus = result.HttpStatus,
            Code = result.Code,
            Message = result.Message,
            Data = limitedData
        });
    }

    [HttpGet("citas-confirmadas")]
    public async Task<IActionResult> CitasConfirmadas(
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] string? texto,
        CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetConfirmedAppointmentsAsync(
            sedeId,
            servicioId,
            texto,
            cancellationToken);

        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("prioridades-ticket")]
    public async Task<IActionResult> PrioridadesTicket(CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetTicketPrioritiesAsync(cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("estados-ticket")]
    public async Task<IActionResult> EstadosTicket(CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetTicketStatesAsync(cancellationToken);
        return ToActionResult(result);
    }
}