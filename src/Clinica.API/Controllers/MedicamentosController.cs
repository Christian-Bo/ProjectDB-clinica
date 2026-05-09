using Clinica.Application.Contracts;
using Clinica.Application.Models.Farmacia;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[Route("api/medicamentos")]
public sealed class MedicamentosController : BaseController
{
    private readonly IFarmaciaService _svc;
    public MedicamentosController(IFarmaciaService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estado,
        [FromQuery] string? texto,
        CancellationToken ct)
        => ToActionResult(await _svc.ListarMedicamentosAsync(
            new MedicamentoListarFiltrosDto { Estado = estado, Texto = texto }, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerMedicamentoAsync(id, null, ct));

    [HttpGet("codigo/{codigo}")]
    public async Task<IActionResult> ObtenerPorCodigo(string codigo, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerMedicamentoAsync(0, codigo, ct));

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] MedicamentoUpsertDto dto, CancellationToken ct)
        => ToActionResult(await _svc.UpsertMedicamentoAsync(dto, ct));
}

[Route("api/farmacia")]
public sealed class FarmaciaController : BaseController
{
    private readonly IFarmaciaService _svc;
    public FarmaciaController(IFarmaciaService svc) => _svc = svc;

    [HttpGet("recetas-pendientes")]
    public async Task<IActionResult> RecetasPendientes(
        [FromQuery] int?    pacienteId,
        [FromQuery] string? texto,
        CancellationToken ct)
        => ToActionResult(await _svc.ListarRecetasPendientesAsync(pacienteId, texto, ct));

    [HttpPost("recetas/{recetaId:long}/despachar")]
    public async Task<IActionResult> Despachar(
        long recetaId,
        [FromQuery] string? observaciones,
        CancellationToken ct)
    {
        var usuarioId = ResolveUserId() ?? 1;
        return ToActionResult(
            await _svc.DespacharRecetaAsync(recetaId, usuarioId, observaciones, ct));
    }
}