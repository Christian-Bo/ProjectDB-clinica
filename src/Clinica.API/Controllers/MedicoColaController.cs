using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Operativo;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[Route("api/medico/cola")]
public sealed class MedicoColaController(IMedicoColaService service) : BaseController
{
    [HttpGet("contexto")]
    public async Task<ActionResult> Contexto([FromQuery] int usuarioId, CancellationToken ct)
        => Ok(new { ok = true, code = "OK", message = "Contexto medico obtenido.", data = await service.ObtenerContextoAsync(usuarioId, ct) });

    [HttpGet]
    public async Task<ActionResult> Cola([FromQuery] int medicoId, [FromQuery] int? sedeId, [FromQuery] int? consultorioId, [FromQuery] int top = 50, CancellationToken ct = default)
        => Ok(new { ok = true, code = "OK", message = "Cola medica listada.", data = await service.ListarColaAsync(medicoId, sedeId, consultorioId, top, ct) });

    [HttpPost("siguiente")]
    public async Task<ActionResult> Siguiente([FromBody] MedicoLlamarSiguienteRequest request, CancellationToken ct)
        => ToActionResult(await service.LlamarSiguienteAsync(request, ct));

    [HttpPost("tickets/{ticketId:long}/en-consulta")]
    public async Task<ActionResult> EnConsulta(long ticketId, [FromBody] MedicoMarcarEnConsultaRequest request, CancellationToken ct)
        => ToActionResult(await service.MarcarEnConsultaAsync(ticketId, request, ct));
}
