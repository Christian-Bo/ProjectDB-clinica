using Clinica.Application.Contracts;
using Clinica.Application.Models.Farmacia;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// MedicamentosController
// GET  /api/medicamentos              → listar
// GET  /api/medicamentos/{id}         → obtener por MedicamentoId
// GET  /api/medicamentos/codigo/{cod} → obtener por CodigoInterno
// POST /api/medicamentos              → upsert
// =============================================================================
[Route("api/medicamentos")]
public sealed class MedicamentosController : BaseController
{
    private readonly IFarmaciaService _svc;
    public MedicamentosController(IFarmaciaService svc) => _svc = svc;

    /// <summary>Lista medicamentos. Filtros: ?estado=ACTIVO y/o ?texto=paracetamol</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estado,
        [FromQuery] string? texto,
        CancellationToken ct)
        => ToActionResult(await _svc.ListarMedicamentosAsync(
            new MedicamentoListarFiltrosDto { Estado = estado, Texto = texto }, ct));

    /// <summary>Obtiene un medicamento por su MedicamentoId.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerMedicamentoAsync(id, null, ct));

    /// <summary>Obtiene un medicamento por su CodigoInterno (ej: MED-0001).</summary>
    [HttpGet("codigo/{codigo}")]
    public async Task<IActionResult> ObtenerPorCodigo(string codigo, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerMedicamentoAsync(0, codigo, ct));

    /// <summary>
    /// Crea (MedicamentoId null) o actualiza (MedicamentoId con valor) un medicamento.
    /// CodigoInterno, PrincipioActivo y PrecioVenta son obligatorios.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] MedicamentoUpsertDto dto, CancellationToken ct)
        => ToActionResult(await _svc.UpsertMedicamentoAsync(dto, ct));
}
