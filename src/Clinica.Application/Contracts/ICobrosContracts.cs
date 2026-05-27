using Clinica.Application.Models.Cobros;
using Clinica.Application.Models.Common;

namespace Clinica.Application.Contracts;

public interface ICuentasService
{
    Task<ServiceOperationResult<IReadOnlyList<CuentaDto>>> ListarAsync(CuentaListarFiltrosDto filtros, CancellationToken ct = default);
    Task<ServiceOperationResult<CuentaDetalleDto>> ObtenerAsync(long cuentaId, CancellationToken ct = default);
    Task<ServiceOperationResult<CuentaDto>> GenerarDesdeCitaAsync(GenerarCuentaRequestDto request, Guid? idempotencyKey, CancellationToken ct = default);
}

public interface IPagosService
{
    Task<ServiceOperationResult<IReadOnlyList<MetodoPagoDto>>> ListarMetodosPagoAsync(CancellationToken ct = default);
    Task<ServiceOperationResult<PagoDto>> RegistrarPagoAsync(RegistrarPagoRequestDto request, Guid? idempotencyKey, CancellationToken ct = default);
}
