using Clinica.Application.Models.Cobros;
using Clinica.Application.Models.Common;
namespace Clinica.Application.Contracts;
public interface ICuentasService
{
    Task<ServiceOperationResult<CuentaDto>> GenerarDesdeCitaAsync(GenerarCuentaRequestDto request, Guid? idempotencyKey, CancellationToken ct = default);
}

public interface IPagosService
{
    Task<ServiceOperationResult<PagoDto>> RegistrarPagoAsync(RegistrarPagoRequestDto request, Guid? idempotencyKey, CancellationToken ct = default);
}
