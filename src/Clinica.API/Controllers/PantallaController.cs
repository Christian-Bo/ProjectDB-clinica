using System.Text;
using System.Text.Json;
using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Common;
using Clinica.Application.DTOs.Pantalla;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

/// <summary>
/// Módulo 3 — Pantalla pública.
/// Expone la cola actual mediante polling REST y opcionalmente Server-Sent Events (SSE).
/// </summary>
[ApiController]
[Route("api/pantalla")]
[Produces("application/json")]
public sealed class PantallaController(IPantallaService service) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ─── GET /api/pantalla/cola ──────────────────────────────────────────────

    [HttpGet("cola")]
    public async Task<IActionResult> ObtenerCola(
        [FromQuery] int sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] string? servicioIds,
        CancellationToken ct)
    {
        var ids = ResolverServicioIds(servicioId, servicioIds);

        if (sedeId <= 0 || ids.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("sedeId y al menos un servicio son requeridos.", "PARAM_INVALIDO"));

        var cola = await service.ObtenerColaAsync(sedeId, ids, ct);
        return Ok(ApiResponse<PantallaColaDto>.Success(cola));
    }

    // ─── GET /api/pantalla/cola/stream (Server-Sent Events) ─────────────────
    // El frontend lo usa opcionalmente; si no está disponible cae a polling.

    [HttpGet("cola/stream")]
    public async Task StreamCola(
        [FromQuery] int sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] string? servicioIds,
        [FromQuery] int intervalSeconds = 4,
        CancellationToken ct = default)
    {
        var ids = ResolverServicioIds(servicioId, servicioIds);

        if (sedeId <= 0 || ids.Count == 0)
        {
            Response.StatusCode = 400;
            return;
        }

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var interval = TimeSpan.FromSeconds(Math.Clamp(intervalSeconds, 2, 30));

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var cola = await service.ObtenerColaAsync(sedeId, ids, ct);
                var payload = JsonSerializer.Serialize(
                    ApiResponse<PantallaColaDto>.Success(cola), JsonOpts);

                var msg = $"event: cola\ndata: {payload}\n\n";
                await Response.WriteAsync(msg, Encoding.UTF8, ct);
                await Response.Body.FlushAsync(ct);
                await Task.Delay(interval, ct);
            }
        }
        catch (OperationCanceledException) { /* cliente desconectado — normal */ }
    }

    private static List<int> ResolverServicioIds(int? servicioId, string? servicioIds)
    {
        var ids = new List<int>();

        if (servicioId is > 0)
            ids.Add(servicioId.Value);

        if (!string.IsNullOrWhiteSpace(servicioIds))
        {
            foreach (var raw in servicioIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(raw, out var parsed) && parsed > 0)
                    ids.Add(parsed);
            }
        }

        return ids.Distinct().ToList();
    }
}
