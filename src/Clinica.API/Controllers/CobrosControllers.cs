using Clinica.Application.Contracts;
using Clinica.Application.Models.Cobros;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[Route("api/cuentas")]
public sealed class CuentasController : BaseController
{
    private readonly ICuentasService _svc;
    public CuentasController(ICuentasService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? pacienteId,
        [FromQuery] string? estado,
        CancellationToken ct)
    {
        return ToActionResult(await _svc.ListarAsync(new CuentaListarFiltrosDto
        {
            PacienteId = pacienteId,
            Estado = estado
        }, ct));
    }

    [HttpGet("{cuentaId:long}")]
    public async Task<IActionResult> Obtener(long cuentaId, CancellationToken ct)
    {
        return ToActionResult(await _svc.ObtenerAsync(cuentaId, ct));
    }

    [HttpPost("generar-desde-cita")]
    public async Task<IActionResult> GenerarDesdeCita(
        [FromBody] GenerarCuentaRequestDto dto, CancellationToken ct)
    {
        if (!TryGetIdempotencyKey(out var key, out var err)) return err!;
        dto.CreadoPor ??= ResolveUserId();
        return ToActionResult(await _svc.GenerarDesdeCitaAsync(dto, key, ct));
    }
}

[Route("api/pagos")]
public sealed class PagosController : BaseController
{
    private readonly IPagosService _svc;
    public PagosController(IPagosService svc) => _svc = svc;

    [HttpGet("metodos-pago")]
    public async Task<IActionResult> MetodosPago(CancellationToken ct)
    {
        return ToActionResult(await _svc.ListarMetodosPagoAsync(ct));
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarPagoRequestDto dto, CancellationToken ct)
    {
        if (!TryGetIdempotencyKey(out var key, out var err)) return err!;
        dto.RegistradoPor ??= ResolveUserId();
        return ToActionResult(await _svc.RegistrarPagoAsync(dto, key, ct));
    }
}
