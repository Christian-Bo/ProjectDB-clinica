using Clinica.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// ReportesEtlController
// POST /api/reportes/etl-ejecutar
//
// Ejecuta sp_ETL_CargarIncrementalDW.
// USO EXCLUSIVO: administrador / batch.
// NO invocar desde flujos clínicos del paciente.
// Timeout configurado en 300 s en el servicio.
// En producción lo ejecuta el SQL Agent Job JOB_DW_ETL_INCREMENTAL cada 2 min.
// =============================================================================
[Route("api/reportes")]
public sealed class ReportesEtlController : BaseController
{
    private readonly IReportesEtlService _svc;
    public ReportesEtlController(IReportesEtlService svc) => _svc = svc;

    /// <summary>
    /// Ejecuta el ETL incremental hacia el Data Warehouse.
    /// Responde con registros procesados y estado OK | ERROR.
    /// </summary>
    [HttpPost("etl-ejecutar")]
    public async Task<IActionResult> EjecutarEtl(CancellationToken ct)
        => ToActionResult(await _svc.EjecutarEtlAsync(ct));
}
