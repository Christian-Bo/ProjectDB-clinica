using System.Text.Json;
using Clinica.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// -----------------------------------------------------------------------------
// Controlador para la pantalla publica.
// Incluye:
// 1) endpoint tradicional para polling.
// 2) endpoint SSE (Server-Sent Events) para actualizacion continua simple,
//    muy util cuando aun no se desea introducir SignalR o WebSockets.
// -----------------------------------------------------------------------------
[AllowAnonymous]
[Route("api/pantalla")]
public sealed class PantallaController : BaseController
{
    private readonly ITicketQueueService _ticketQueueService;

    public PantallaController(ITicketQueueService ticketQueueService)
    {
        _ticketQueueService = ticketQueueService;
    }

    [HttpGet("cola")]
    public async Task<IActionResult> ObtenerCola([FromQuery] int sedeId, [FromQuery] int servicioId, CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetQueueDisplayAsync(sedeId, servicioId, cancellationToken);
        return ToActionResult(result);
    }

    // -------------------------------------------------------------------------
    // SSE: el frontend puede abrir esta ruta y recibir snapshots cada cierto
    // numero de segundos. Es una opcion muy amigable para una pantalla publica.
    // -------------------------------------------------------------------------
    [HttpGet("cola/stream")]
    public async Task StreamCola(
        [FromQuery] int sedeId,
        [FromQuery] int servicioId,
        [FromQuery] int intervalSeconds = 3,
        CancellationToken cancellationToken = default)
    {
        intervalSeconds = Math.Clamp(intervalSeconds, 1, 20);

        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _ticketQueueService.GetQueueDisplayAsync(sedeId, servicioId, cancellationToken);
                var payload = JsonSerializer.Serialize(new
                {
                    ok = result.Success,
                    code = result.Code,
                    message = result.Message,
                    data = result.Data
                });

                await Response.WriteAsync("event: cola\n", cancellationToken);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // La desconexion del cliente es normal en SSE.
        }
    }
}
