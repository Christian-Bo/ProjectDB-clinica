using Clinica.Application.Contracts;
using Clinica.Application.Models.Cobros;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[Route("api/cuentas")]
public sealed class CuentasController : BaseController
{
    private readonly ICuentasService _svc;
    public CuentasController(ICuentasService svc) => _svc = svc;

    [HttpPost("generar-desde-cita")]
    public async Task<IActionResult> GenerarDesdeCita(
        [FromBody] GenerarCuentaRequestDto dto, CancellationToken ct)
    {
        if (!TryGetIdempotencyKey(out var key, out var err)) return err!;
        dto.CreadoPor ??= ResolveUserId();
        return ToActionResult(await _svc.GenerarDesdeCitaAsync(dto, key, ct));
    }

    [HttpGet("{cuentaId:long}")]
    public async Task<IActionResult> Obtener(long cuentaId, CancellationToken ct)
        => ToActionResult(await _svc.ObtenerAsync(cuentaId, ct));

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int?    pacienteId,
        [FromQuery] string? estado,
        CancellationToken ct)
        => ToActionResult(await _svc.ListarAsync(pacienteId, estado, ct));
}

[Route("api/pagos")]
public sealed class PagosController : BaseController
{
    private readonly IPagosService _svc;
    public PagosController(IPagosService svc) => _svc = svc;

    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarPagoRequestDto dto, CancellationToken ct)
    {
        if (!TryGetIdempotencyKey(out var key, out var err)) return err!;
        dto.RegistradoPor ??= ResolveUserId();
        return ToActionResult(await _svc.RegistrarPagoAsync(dto, key, ct));
    }

    [HttpGet("metodos-pago")]
    public async Task<IActionResult> MetodosPago(CancellationToken ct)
        => ToActionResult(await _svc.ListarMetodosPagoAsync(ct));
}