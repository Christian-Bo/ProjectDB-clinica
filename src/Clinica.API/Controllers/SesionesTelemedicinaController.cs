using Clinica.Application.Contracts;
using Clinica.Application.Models.Telemedicina;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// SesionesTelemedicinaController
// GET  /api/telemedicina/sesiones             → listar
// GET  /api/telemedicina/sesiones/{id}        → obtener por SesionTeleId
// GET  /api/telemedicina/sesiones/cita/{citaId} → obtener por CitaId
// POST /api/telemedicina/sesiones             → upsert
//
// ATENCIÓN: El PK es SesionTeleId (BIGINT), no SesionId.
//           CodigoSala es UNIQUE en dbo.SesionesTelemedicas.
//           Una CitaId solo puede tener UNA sesión (UNIQUE constraint en BD).
// Estados válidos: PROGRAMADA | ACTIVA | FINALIZADA | NO_INICIADA | CANCELADA
// =============================================================================
[Route("api/telemedicina/sesiones")]
public sealed class SesionesTelemedicinaController : BaseController
{
    private readonly ISesionesTelemedicinaService _svc;
    public SesionesTelemedicinaController(ISesionesTelemedicinaService svc) => _svc = svc;

    /// <summary>
    /// Lista sesiones de telemedicina.
    /// Filtros: ?estado=PROGRAMADA  ?fechaDesde=  ?fechaHasta=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estado,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken ct)
        => ToActionResult(await _svc.ListarSesionesAsync(
            new SesionListarFiltrosDto { Estado = estado, FechaDesde = fechaDesde, FechaHasta = fechaHasta }, ct));

    /// <summary>Obtiene una sesión por su SesionTeleId.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> ObtenerPorId(long id, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerSesionAsync(id, null, ct));

    /// <summary>Obtiene la sesión asociada a una CitaId específica.</summary>
    [HttpGet("cita/{citaId:long}")]
    public async Task<IActionResult> ObtenerPorCita(long citaId, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerSesionAsync(null, citaId, ct));

    /// <summary>
    /// Crea (SesionTeleId null) o actualiza (SesionTeleId con valor) una sesión.
    /// UrlSala y CodigoSala son OBLIGATORIOS.
    /// CodigoSala debe ser único. Una CitaId solo puede tener una sesión.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upsert(
        [FromBody] SesionTelemedicaUpsertDto dto, CancellationToken ct)
        => ToActionResult(await _svc.UpsertSesionAsync(dto, ct));
}
