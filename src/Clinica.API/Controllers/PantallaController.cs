using System.Text.Json;
using System.Text.Json.Serialization;
using Clinica.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[AllowAnonymous]
[Route("api/pantalla")]
public sealed class PantallaController : BaseController
{
    private readonly ITicketQueueService _ticketQueueService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public PantallaController(ITicketQueueService ticketQueueService)
    {
        _ticketQueueService = ticketQueueService;
    }

    [HttpGet("cola")]
    public async Task<IActionResult> ObtenerCola(
        [FromQuery] int sedeId,
        [FromQuery] int servicioId,
        CancellationToken cancellationToken)
    {
        var result = await _ticketQueueService.GetQueueDisplayAsync(sedeId, servicioId, cancellationToken);
        return ToActionResult(result);
    }

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
                var result = await _ticketQueueService.GetQueueDisplayAsync(
                    sedeId,
                    servicioId,
                    cancellationToken);

                var payload = JsonSerializer.Serialize(new
                {
                    ok = result.Success,
                    code = result.Code,
                    message = result.Message,
                    data = result.Data
                }, JsonOptions);

                await Response.WriteAsync("event: cola\n", cancellationToken);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}