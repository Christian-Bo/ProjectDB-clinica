using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Operativo;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[Route("api/secretaria")]
public sealed class SecretariaController(ISecretariaService service) : BaseController
{
    [HttpGet("contextos")]
    public async Task<ActionResult> Contextos([FromQuery] int usuarioId, CancellationToken ct)
        => Ok(new { ok = true, code = "OK", message = "Contextos de secretaria listados.", data = await service.ObtenerContextosAsync(usuarioId, ct) });

    [HttpPost("contexto")]
    public async Task<ActionResult> ConfigurarContexto([FromBody] SecretariaConfigurarContextoRequest request, CancellationToken ct)
        => Ok(new { ok = true, code = "OK", message = "Contexto configurado.", data = await service.ConfigurarContextoAsync(request, ct) });

    [HttpGet("cola")]
    public async Task<ActionResult> Cola([FromQuery] int usuarioId, [FromQuery] int sedeId, [FromQuery] int? servicioId, [FromQuery] int estacionId, [FromQuery] string? estado, [FromQuery] int top = 50, CancellationToken ct = default)
        => Ok(new { ok = true, code = "OK", message = "Cola de ventanilla listada.", data = await service.ListarColaAsync(usuarioId, sedeId, servicioId, estacionId, estado, top, ct) });

    [HttpPost("tickets/siguiente")]
    public async Task<ActionResult> TomarSiguiente([FromBody] SecretariaTomarSiguienteRequest request, CancellationToken ct)
        => ToActionResult(await service.TomarSiguienteAsync(request, ct));

    [HttpPost("tickets/{ticketId:long}/asistencia")]
    public async Task<ActionResult> RegistrarAsistencia(long ticketId, [FromBody] SecretariaRegistrarAsistenciaRequest request, CancellationToken ct)
        => ToActionResult(await service.RegistrarAsistenciaAsync(ticketId, request, ct));

    [HttpPost("tickets/{ticketId:long}/enviar-medico")]
    public async Task<ActionResult> EnviarMedico(long ticketId, [FromBody] SecretariaEnviarMedicoRequest request, CancellationToken ct)
        => ToActionResult(await service.EnviarMedicoAsync(ticketId, request, ct));

    [HttpPost("tickets/{ticketId:long}/no-show")]
    public async Task<ActionResult> MarcarNoShow(long ticketId, [FromBody] SecretariaNoShowRequest request, CancellationToken ct)
        => ToActionResult(await service.MarcarNoShowAsync(ticketId, request, ct));

    [HttpGet("resumen")]
    public async Task<ActionResult> Resumen([FromQuery] int usuarioId, [FromQuery] int sedeId, [FromQuery] int? servicioId, [FromQuery] int estacionId, CancellationToken ct)
        => Ok(new { ok = true, code = "OK", message = "Resumen de ventanilla.", data = await service.ObtenerResumenAsync(usuarioId, sedeId, servicioId, estacionId, ct) });
}
