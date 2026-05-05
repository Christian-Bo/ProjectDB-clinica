using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Common;
using Clinica.Application.DTOs.Tickets;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

/// <summary>
/// Módulo 3 — Tickets.
/// Recibe HTTP, valida formato, delega al servicio, devuelve JSON.
/// Sin SQL. Sin lógica de negocio crítica aquí.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public sealed class TicketsController(ITicketsService service) : ControllerBase
{
    // ─── GET /api/tickets ────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] string? estado,
        CancellationToken ct)
    {
        var tickets = await service.ListarTicketsAsync(sedeId, servicioId, estado, ct);
        return Ok(ApiResponse<List<Application.DTOs.Tickets.TicketDto>>.Success(tickets));
    }

    // ─── GET /api/tickets/resumen-operativo ─────────────────────────────────

    [HttpGet("resumen-operativo")]
    public async Task<IActionResult> ResumenOperativo(
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        CancellationToken ct)
    {
        var resumen = await service.ObtenerResumenOperativoAsync(sedeId, servicioId, ct);
        return Ok(ApiResponse<ResumenOperativoDto>.Success(resumen));
    }

    // ─── GET /api/tickets/{ticketId} ─────────────────────────────────────────

    [HttpGet("{ticketId:long}")]
    public async Task<IActionResult> ObtenerPorId(long ticketId, CancellationToken ct)
    {
        var ticket = await service.ObtenerTicketAsync(ticketId, ct);
        return Ok(ApiResponse<TicketDto>.Success(ticket));
    }

    // ─── GET /api/tickets/por-numero/{numero} ────────────────────────────────

    [HttpGet("por-numero/{numero}")]
    public async Task<IActionResult> ObtenerPorNumero(string numero, CancellationToken ct)
    {
        var ticket = await service.ObtenerTicketPorNumeroAsync(numero, ct);
        return Ok(ApiResponse<TicketDto>.Success(ticket));
    }

    // ─── GET /api/tickets/mi-ticket ─────────────────────────────────────────

    [HttpGet("mi-ticket")]
    public async Task<IActionResult> MiTicket(
        [FromQuery] long? ticketId,
        [FromQuery] string? numeroTicket,
        CancellationToken ct)
    {
        var ticket = await service.ObtenerMiTicketAsync(ticketId, numeroTicket, ct);
        return Ok(ApiResponse<TicketDto>.Success(ticket));
    }

    // ─── POST /api/tickets/generar ──────────────────────────────────────────

    [HttpPost("generar")]
    public async Task<IActionResult> Generar(
        [FromBody] GenerarTicketRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
        CancellationToken ct)
    {
        var ticket = await service.GenerarTicketAsync(request, idempotencyKey, ct);
        return StatusCode(201, ApiResponse<TicketDto>.Success(ticket, "Ticket generado correctamente."));
    }

    // ─── POST /api/tickets/generar-kiosco ───────────────────────────────────

    [HttpPost("generar-kiosco")]
    public async Task<IActionResult> GenerarKiosco(
        [FromBody] GenerarTicketKioscoRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
        CancellationToken ct)
    {
        var ticket = await service.GenerarTicketKioscoAsync(request, idempotencyKey, ct);
        return StatusCode(201, ApiResponse<TicketDto>.Success(ticket, "Ticket generado desde kiosco."));
    }

    // ─── POST /api/tickets/generar-especial ─────────────────────────────────

    [HttpPost("generar-especial")]
    public async Task<IActionResult> GenerarEspecial(
        [FromBody] GenerarTicketEspecialRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
        CancellationToken ct)
    {
        var ticket = await service.GenerarTicketEspecialAsync(request, idempotencyKey, ct);
        return StatusCode(201, ApiResponse<TicketDto>.Success(ticket, "Ticket especial generado."));
    }

    // ─── POST /api/tickets/siguiente ────────────────────────────────────────

    [HttpPost("siguiente")]
    public async Task<IActionResult> LlamarSiguiente(
        [FromBody] LlamarSiguienteRequest request,
        CancellationToken ct)
    {
        var ticket = await service.LlamarSiguienteAsync(request, ct);
        return Ok(ApiResponse<TicketDto>.Success(ticket, "Ticket llamado correctamente."));
    }


    // ─── POST /api/tickets/{ticketId}/rellamar ───────────────────────────────

    [HttpPost("{ticketId:long}/rellamar")]
    public async Task<IActionResult> Rellamar(
        long ticketId,
        [FromBody] RellamarTicketRequest request,
        CancellationToken ct)
    {
        var ticket = await service.RellamarTicketAsync(ticketId, request.UsuarioId, ct);
        return Ok(ApiResponse<TicketDto>.Success(ticket, "Ticket llamado nuevamente."));
    }

    // ─── POST /api/tickets/{ticketId}/en-atencion ────────────────────────────

    [HttpPost("{ticketId:long}/en-atencion")]
    public async Task<IActionResult> MarcarEnAtencion(long ticketId, CancellationToken ct)
    {
        var ticket = await service.MarcarEnAtencionAsync(ticketId, ct);
        return Ok(ApiResponse<TicketDto>.Success(ticket, "Ticket marcado en atención."));
    }

    // ─── POST /api/tickets/{ticketId}/finalizar ──────────────────────────────

    [HttpPost("{ticketId:long}/finalizar")]
    public async Task<IActionResult> Finalizar(
        long ticketId,
        [FromBody] FinalizarTicketRequest request,
        CancellationToken ct)
    {
        var ticket = await service.FinalizarTicketAsync(ticketId, request.Motivo, ct);
        return Ok(ApiResponse<TicketDto>.Success(ticket, "Ticket finalizado."));
    }

    // ─── POST /api/tickets/{ticketId}/cancelar ───────────────────────────────

    [HttpPost("{ticketId:long}/cancelar")]
    public async Task<IActionResult> Cancelar(
        long ticketId,
        [FromBody] CancelarTicketRequest request,
        CancellationToken ct)
    {
        var ticket = await service.CancelarTicketAsync(ticketId, request.Motivo, request.UsuarioId, ct);
        return Ok(ApiResponse<TicketDto>.Success(ticket, "Ticket cancelado."));
    }

    // ─── POST /api/tickets/no-show/procesar ─────────────────────────────────

    [HttpPost("no-show/procesar")]
    public async Task<IActionResult> ProcesarNoShow(CancellationToken ct)
    {
        var result = await service.ProcesarNoShowAsync(ct);
        return Ok(ApiResponse<NoShowResultDto>.Success(result, $"Procesados: {result.RegistrosProcesados} tickets."));
    }
}
