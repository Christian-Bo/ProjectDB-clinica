using Clinica.Application.Contracts;
using Clinica.Application.Models.Consultas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// -----------------------------------------------------------------------------
// Módulo Dev4 — Consulta Médica e Historia Clínica.
//
// Regla crítica de este módulo:
//   - Una consulta ABIERTA se puede cerrar.
//   - Una consulta CERRADA solo admite correcciones append-only.
//   - No existe un endpoint PUT /consultas/{id} para edición libre.
// Esa inmutabilidad la garantiza SQL Server desde sp_CerrarConsulta.
// -----------------------------------------------------------------------------
[AllowAnonymous]
[Route("api/consultas")]
public sealed class ConsultasController : BaseController
{
    private readonly IConsultasService _consultasService;

    public ConsultasController(IConsultasService consultasService)
    {
        _consultasService = consultasService;
    }

    // -------------------------------------------------------------------------
    // Abre una consulta desde un ticket válido.
    // El SP valida que el ticket exista y esté en estado apto para atención.
    // -------------------------------------------------------------------------
    [HttpPost("abrir-desde-ticket")]
    public async Task<IActionResult> AbrirDesdeTicket(
        [FromBody] AbrirConsultaRequestDto request,
        CancellationToken cancellationToken)
    {
        request.UsuarioId = ResolveUserId(request.UsuarioId);

        var result = await _consultasService.AbrirDesdeTicketAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    // -------------------------------------------------------------------------
    // Cierra la consulta. Después de esto el registro es inmutable.
    // -------------------------------------------------------------------------
    [HttpPost("{consultaId:long}/cerrar")]
    public async Task<IActionResult> Cerrar(
        long consultaId,
        [FromBody] CerrarConsultaRequestDto request,
        CancellationToken cancellationToken)
    {
        request.UsuarioId = ResolveUserId(request.UsuarioId);

        var result = await _consultasService.CerrarAsync(consultaId, request, cancellationToken);
        return ToActionResult(result);
    }

    // -------------------------------------------------------------------------
    // Agrega una nota de corrección sobre una consulta ya cerrada.
    // No altera el registro original; agrega una fila en NotasCorreccion.
    // -------------------------------------------------------------------------
    [HttpPost("{consultaId:long}/correcciones")]
    public async Task<IActionResult> AgregarCorreccion(
        long consultaId,
        [FromBody] NotaCorreccionRequestDto request,
        CancellationToken cancellationToken)
    {
        request.UsuarioId = ResolveUserId(request.UsuarioId);

        var result = await _consultasService.AgregarNotaCorreccionAsync(consultaId, request, cancellationToken);
        return ToActionResult(result);
    }

    // -------------------------------------------------------------------------
    // Obtiene la consulta completa: signos vitales, diagnósticos, notas.
    // -------------------------------------------------------------------------
    [HttpGet("{consultaId:long}")]
    public async Task<IActionResult> Obtener(long consultaId, CancellationToken cancellationToken)
    {
        var result = await _consultasService.ObtenerAsync(consultaId, cancellationToken);
        return ToActionResult(result);
    }
}

// -----------------------------------------------------------------------------
// Historial clínico — ruta separada bajo /api/pacientes/{pacienteId}/historial
// para seguir la convención del documento de contratos API.
// -----------------------------------------------------------------------------
[AllowAnonymous]
[Route("api/pacientes")]
public sealed class HistorialController : BaseController
{
    private readonly IConsultasService _consultasService;

    public HistorialController(IConsultasService consultasService)
    {
        _consultasService = consultasService;
    }

    [HttpGet("{pacienteId:int}/historial")]
    public async Task<IActionResult> ObtenerHistorial(int pacienteId, CancellationToken cancellationToken)
    {
        var result = await _consultasService.ObtenerHistorialAsync(pacienteId, cancellationToken);
        return ToActionResult(result);
    }
}
