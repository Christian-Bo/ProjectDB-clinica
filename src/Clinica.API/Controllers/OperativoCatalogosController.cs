using Clinica.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[Route("api/catalogos-operativos")]
public sealed class OperativoCatalogosController(IOperativoCatalogosService service) : BaseController
{
    [HttpGet("lookup")]
    public async Task<ActionResult> Lookup(
        [FromQuery] string tipo,
        [FromQuery] int? sedeId,
        [FromQuery] int? servicioId,
        [FromQuery] int? especialidadId,
        [FromQuery] int? medicoId,
        [FromQuery] int? consultorioId,
        [FromQuery] int? usuarioId,
        [FromQuery] string? busqueda,
        [FromQuery] int top = 100,
        CancellationToken ct = default)
        => Ok(new { ok = true, code = "OK", message = "Lookup listado.", data = await service.LookupAsync(tipo, sedeId, servicioId, especialidadId, medicoId, consultorioId, usuarioId, busqueda, top, ct) });

    [HttpGet("sedes")]
    public Task<ActionResult> Sedes(CancellationToken ct)
        => Lookup("SEDES", null, null, null, null, null, null, null, 100, ct);

    [HttpGet("especialidades")]
    public Task<ActionResult> Especialidades(CancellationToken ct)
        => Lookup("ESPECIALIDADES", null, null, null, null, null, null, null, 100, ct);

    [HttpGet("servicios")]
    public Task<ActionResult> Servicios([FromQuery] int? sedeId, [FromQuery] int? especialidadId, CancellationToken ct)
        => Lookup("SERVICIOS", sedeId, null, especialidadId, null, null, null, null, 100, ct);

    [HttpGet("medicos")]
    public Task<ActionResult> Medicos([FromQuery] int? sedeId, [FromQuery] int? especialidadId, [FromQuery] int? usuarioId, CancellationToken ct)
        => Lookup("MEDICOS", sedeId, null, especialidadId, null, null, usuarioId, null, 100, ct);

    [HttpGet("consultorios")]
    public Task<ActionResult> Consultorios([FromQuery] int? sedeId, [FromQuery] int? especialidadId, [FromQuery] int? medicoId, CancellationToken ct)
        => Lookup("CLINICAS", sedeId, null, especialidadId, medicoId, null, null, null, 100, ct);

    [HttpGet("ventanillas")]
    public Task<ActionResult> Ventanillas([FromQuery] int? sedeId, CancellationToken ct)
        => Lookup("VENTANILLAS", sedeId, null, null, null, null, null, null, 100, ct);

    [HttpGet("ventanilla-clinicas")]
    public Task<ActionResult> VentanillaClinicas([FromQuery] int? sedeId, [FromQuery] int? servicioId, [FromQuery] int? especialidadId, CancellationToken ct)
        => Lookup("VENTANILLA_CLINICAS", sedeId, servicioId, especialidadId, null, null, null, null, 100, ct);

    [HttpGet("disponibilidad")]
    public async Task<ActionResult> Disponibilidad([FromQuery] int sedeId, [FromQuery] DateTime fecha, [FromQuery] int? servicioId, [FromQuery] int? especialidadId, [FromQuery] int? medicoId, [FromQuery] bool soloDisponibles = false, CancellationToken ct = default)
        => Ok(new { ok = true, code = "OK", message = "Disponibilidad listada.", data = await service.ListarDisponibilidadAsync(sedeId, fecha, servicioId, especialidadId, medicoId, soloDisponibles, ct) });
}
