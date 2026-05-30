using Clinica.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// ReportesEtlController
// POST /api/reportes/etl-ejecutar
// GET  /api/reportes/dashboard-decisiones
//
// Ejecuta y consulta analítica administrativa basada en Stored Procedures.
// No contiene SQL inline: todo el cálculo se delega a la base de datos.
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

    /// <summary>
    /// Devuelve información ejecutiva de los cubos OLAP para toma de decisiones.
    /// </summary>
    [HttpGet("dashboard-decisiones")]
    public async Task<IActionResult> DashboardDecisiones([FromQuery] int dias = 30, CancellationToken ct = default)
        => ToActionResult(await _svc.ObtenerDashboardDecisionAsync(dias, ct));
}
