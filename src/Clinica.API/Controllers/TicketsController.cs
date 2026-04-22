using Clinica.Application.Contracts;
using Clinica.Application.Models.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// -----------------------------------------------------------------------------
// Modulo 3 - Recepcion / Tickets / Cola.
// Este controlador cubre el ciclo de vida del ticket:
// generar -> llamar -> en atencion -> finalizar -> no-show.
// Tambien expone endpoints de consulta para que el frontend trabaje con datos
// utiles, enriquecidos y faciles de mostrar al usuario.
// -----------------------------------------------------------------------------
[Route("api/tickets")]
public sealed class TicketsController : BaseController
{
    private readonly ITicketQueueService _ticketQueueService;

    public TicketsController(ITicketQueueService ticketQueueService)
    {
        _ticketQueueService = ticketQueueService;
    }

    [HttpPost("generar")]
    public async Task<IActionResult> Generar(
        [FromBody] GenerateTicketRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdempotencyKey(out var idempotencyKey, out var errorResult))
        {
            return errorResult!;
        }

        request.UsuarioId = ResolveUserId(request.UsuarioId);

        var result = await _ticketQueueService.GenerateTicketAsync(request, idempotencyKey, cancellationToken);
        return ToActionResult(result);
    }

    // -------------------------------------------------------------------------
    // Endpoint mas amigable para el caso de ticket especial.
    // El frontend no necesita recordar que debe enviar PrioridadSolicitada=ESPECIAL;
    // este endpoint lo normaliza automaticamente.
    // -------------------------------------------------------------------------
    [HttpPost("generar-especial")]
    public async Task<IActionResult> GenerarEspecial(
        [FromBody] SpecialTicketRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdempotencyKey(out var idempotencyKey, out var errorResult))
        {
            return errorResult!;
        }

        var normalizedRequest = new GenerateTicketRequestDto
        {
            CitaId = request.CitaId,
            PacienteId = request.PacienteId,
            SedeId = request.SedeId,
            ServicioId = request.ServicioId,
            MedicoId = request.MedicoId,
            PrioridadSolicitada = "ESPECIAL",
            MotivoEspecial = request.MotivoEspecial,
            UsuarioId = ResolveUserId(request.UsuarioId)
        };

        var result = await _ticketQueueService.GenerateTicketAsync(normalizedRequest, idempotencyKey, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("siguiente")]
    public async Task<IActionResult> LlamarSiguiente(
        [FromBody] CallNextTicketRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdempotencyKey(out var idempotencyKey, out var errorResult))
        {
            return errorResult!;
        }

        request.UsuarioId = ResolveUserId(request.UsuarioId);

        var result = await _ticketQueueService.CallNextAsync(request, idempotencyKey, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{ticketId:long}/en-atencion")]
    public async Task<IActionResult> MarcarEnAtencion(long ticketId, CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.MarkInAttentionAsync(ticketId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{ticketId:long}/finalizar")]
    public async Task<IActionResult> Finalizar(
        long ticketId,
        [FromBody] FinalizeTicketRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.FinishAsync(ticketId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("no-show/procesar")]
    public async Task<IActionResult> ProcesarNoShow(CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.ProcessNoShowAsync(cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] TicketListFiltersDto filters, CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.ListAsync(filters, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("resumen-operativo")]
    public async Task<IActionResult> ResumenOperativo(
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetOperationalSummaryAsync(sedeId, servicioId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{ticketId:long}")]
    public async Task<IActionResult> ObtenerPorId(long ticketId, CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetByIdAsync(ticketId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("por-numero/{numeroTicket}")]
    public async Task<IActionResult> ObtenerPorNumero(string numeroTicket, CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetByNumberAsync(numeroTicket, cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpGet("mi-ticket")]
    public async Task<IActionResult> MiTicket(
        [FromQuery] long? ticketId,
        [FromQuery] string? numeroTicket,
        CancellationToken cancellationToken)
    {
        if (ticketId is null && string.IsNullOrWhiteSpace(numeroTicket))
        {
            return BadRequest(new
            {
                ok = false,
                code = "FILTRO_REQUERIDO",
                message = "Debes enviar ticketId o numeroTicket para consultar el estado del ticket."
            });
        }

        var result = ticketId.HasValue
            ? await _ticketQueueService.GetByIdAsync(ticketId.Value, cancellationToken)
            : await _ticketQueueService.GetByNumberAsync(numeroTicket!, cancellationToken);

        return ToActionResult(result);
    }
}