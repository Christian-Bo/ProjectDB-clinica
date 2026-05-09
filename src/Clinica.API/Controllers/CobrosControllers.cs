using Clinica.Application.Contracts;
using Clinica.Application.Models.Cobros;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// =============================================================================
// CuentasController
// POST /api/cuentas/generar-desde-cita
//
// Genera una cuenta de cobro para una cita.
// Soporta Idempotency-Key: envía el header "Idempotency-Key: <GUID>"
// para evitar cuentas duplicadas si el cliente reintenta la petición.
// =============================================================================
[Route("api/cuentas")]
public sealed class CuentasController : BaseController
{
    private readonly ICuentasService _svc;
    public CuentasController(ICuentasService svc) => _svc = svc;

    /// <summary>
    /// Genera cuenta desde cita. El body incluye los ítems (TVP dbo.TVP_DetallesCuenta).
    /// TipoConcepto válidos: CONSULTA | MEDICAMENTO | PROCEDIMIENTO | LAB | IMAGEN | OTRO
    /// Una cita solo puede tener UNA cuenta (validado en el SP).
    /// </summary>
    [HttpPost("generar-desde-cita")]
    public async Task<IActionResult> GenerarDesdeCita(
        [FromBody] GenerarCuentaRequestDto dto, CancellationToken ct)
    {
        if (!TryGetIdempotencyKey(out var key, out var err)) return err!;
        dto.CreadoPor ??= ResolveUserId();
        return ToActionResult(await _svc.GenerarDesdeCitaAsync(dto, key, ct));
    }
}

// =============================================================================
// PagosController
// POST /api/pagos
//
// Registra un pago sobre una cuenta existente.
// El SP valida que el monto no exceda el saldo actual.
// Soporta Idempotency-Key para evitar pagos duplicados.
// =============================================================================
[Route("api/pagos")]
public sealed class PagosController : BaseController
{
    private readonly IPagosService _svc;
    public PagosController(IPagosService svc) => _svc = svc;

    /// <summary>
    /// Registra un pago. MetodoPagoId debe existir en cfg.MetodosPago.
    /// El SP actualiza el Saldo y Estado de la cuenta automáticamente.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarPagoRequestDto dto, CancellationToken ct)
    {
        if (!TryGetIdempotencyKey(out var key, out var err)) return err!;
        dto.RegistradoPor ??= ResolveUserId();
        return ToActionResult(await _svc.RegistrarPagoAsync(dto, key, ct));
    }
}
