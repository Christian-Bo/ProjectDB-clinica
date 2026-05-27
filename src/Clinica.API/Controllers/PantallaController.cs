using System.Text;
using System.Text.Json;
using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Common;
using Clinica.Application.DTOs.Pantalla;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/pantalla")]
[Produces("application/json")]
public sealed class PantallaController(IPantallaService service) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [HttpGet("cola")]
    public async Task<IActionResult> ObtenerCola(
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] string? servicioIds,
        CancellationToken ct)
    {
        var resolvedSedeId = sedeId.GetValueOrDefault();
        var ids = ResolverServicioIds(servicioId, servicioIds);

        if (resolvedSedeId <= 0)
        {
            return Ok(ApiResponse<PantallaColaDto>.Success(PantallaColaDto.Empty(), "Pantalla sin sede seleccionada."));
        }

        var cola = await service.ObtenerColaAsync(resolvedSedeId, ids, ct);
        return Ok(ApiResponse<PantallaColaDto>.Success(cola));
    }

    [HttpGet("cola/stream")]
    public async Task StreamCola(
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] string? servicioIds,
        [FromQuery] int intervalSeconds = 4,
        CancellationToken ct = default)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var resolvedSedeId = sedeId.GetValueOrDefault();
        var ids = ResolverServicioIds(servicioId, servicioIds);
        var interval = TimeSpan.FromSeconds(Math.Clamp(intervalSeconds, 3, 30));

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var cola = resolvedSedeId <= 0
                    ? PantallaColaDto.Empty()
                    : await service.ObtenerColaAsync(resolvedSedeId, ids, ct);

                var payload = JsonSerializer.Serialize(ApiResponse<PantallaColaDto>.Success(cola), JsonOpts);
                await Response.WriteAsync($"event: cola\ndata: {payload}\n\n", Encoding.UTF8, ct);
                await Response.Body.FlushAsync(ct);
                await Task.Delay(interval, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static List<int> ResolverServicioIds(int? servicioId, string? servicioIds)
    {
        var ids = new List<int>();

        if (servicioId is > 0)
            ids.Add(servicioId.Value);

        if (!string.IsNullOrWhiteSpace(servicioIds))
        {
            var rawValues = servicioIds
                .Replace(";", ",", StringComparison.Ordinal)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var raw in rawValues)
            {
                if (int.TryParse(raw, out var parsed) && parsed > 0)
                    ids.Add(parsed);
            }
        }

        return ids.Distinct().OrderBy(id => id).ToList();
    }
}
