using Clinica.Application.Contracts;
using Clinica.Application.Models.Notificaciones;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// PlantillasNotificacionController
// GET  /api/notificaciones/plantillas        → listar
// GET  /api/notificaciones/plantillas/{id}   → obtener
// POST /api/notificaciones/plantillas        → upsert
//
// La BD tiene UNIQUE(TipoEvento, Canal) — no se puede repetir la combinación.
// Canal válidos: EMAIL | WHATSAPP | SMS | PUSH | SISTEMA
// =============================================================================
[Route("api/notificaciones/plantillas")]
public sealed class PlantillasNotificacionController : BaseController
{
    private readonly IPlantillasNotificacionService _svc;
    public PlantillasNotificacionController(IPlantillasNotificacionService svc) => _svc = svc;

    /// <summary>
    /// Lista plantillas.
    /// Filtros: ?tipoEvento=CONFIRMACION_CITA  ?canal=EMAIL  ?activo=true
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? tipoEvento,
        [FromQuery] string? canal,
        [FromQuery] bool? activo,
        CancellationToken ct)
        => ToActionResult(await _svc.ListarPlantillasAsync(
            new PlantillaListarFiltrosDto { TipoEvento = tipoEvento, Canal = canal, Activo = activo }, ct));

    /// <summary>Obtiene una plantilla por su PlantillaId.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerPlantillaAsync(id, ct));

    /// <summary>
    /// Crea (PlantillaId null) o actualiza (PlantillaId con valor) una plantilla.
    /// TipoEvento + Canal deben ser únicos.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upsert(
        [FromBody] PlantillaNotificacionUpsertDto dto, CancellationToken ct)
        => ToActionResult(await _svc.UpsertPlantillaAsync(dto, ct));
}

// =============================================================================
// ColaNotificacionesController
// POST /api/notificaciones/encolar    → encolar notificación
// GET  /api/notificaciones/pendientes → listar pendientes
// POST /api/notificaciones/procesar   → procesar cola
// =============================================================================
[Route("api/notificaciones")]
public sealed class ColaNotificacionesController : BaseController
{
    private readonly IColaNotificacionesService _svc;
    public ColaNotificacionesController(IColaNotificacionesService svc) => _svc = svc;

    /// <summary>
    /// Encola una notificación.
    /// El cuerpo debe llegar ya renderizado (variables sustituidas).
    /// FechaProgramada determina cuándo el job la procesará.
    /// </summary>
    [HttpPost("encolar")]
    public async Task<IActionResult> Encolar(
        [FromBody] EncolarNotificacionRequestDto dto, CancellationToken ct)
        => ToActionResult(await _svc.EncolarAsync(dto, ct));

    /// <summary>
    /// Lista notificaciones pendientes o en reintento.
    /// Filtros: ?canal=EMAIL  ?maxRegistros=100
    /// </summary>
    [HttpGet("pendientes")]
    public async Task<IActionResult> Pendientes(
        [FromQuery] string? canal,
        [FromQuery] int maxRegistros = 100,
        CancellationToken ct = default)
        => ToActionResult(await _svc.ListarPendientesAsync(
            new ColaListarFiltrosDto { Canal = canal, MaxRegistros = maxRegistros }, ct));

    /// <summary>
    /// Dispara sp_ProcesarColaNotificaciones.
    /// Marca como ENVIADA todas las notificaciones cuya FechaProgramada ya pasó.
    /// En producción esto lo ejecuta un SQL Agent Job cada minuto.
    /// </summary>
    [HttpPost("procesar")]
    public async Task<IActionResult> Procesar(CancellationToken ct)
        => ToActionResult(await _svc.ProcesarColaAsync(ct));
}
