using Clinica.Application.Models.Common;
using Clinica.Application.Models.Inventario;

namespace Clinica.Application.Contracts;

public interface IInventarioService
{
    Task<ServiceOperationResult<MovimientoInventarioDto>> RegistrarMovimientoAsync(RegistrarMovimientoRequestDto request, CancellationToken ct = default);
}
