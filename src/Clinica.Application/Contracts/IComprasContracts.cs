using Clinica.Application.Models.Common;
using Clinica.Application.Models.Compras;

namespace Clinica.Application.Contracts;

public interface IProveedoresService
{
    Task<ServiceOperationResult<ProveedorDto>> UpsertProveedorAsync(ProveedorUpsertDto request, CancellationToken ct = default);
    Task<ServiceOperationResult<ProveedorDto>> ObtenerProveedorAsync(int proveedorId, CancellationToken ct = default);
    Task<ServiceOperationResult<IReadOnlyList<ProveedorDto>>> ListarProveedoresAsync(ProveedorListarFiltrosDto filtros, CancellationToken ct = default);
}

public interface IOrdenesCompraService
{
    Task<ServiceOperationResult<OrdenCompraDto>> CrearOrdenAsync(OrdenCompraCrearDto request, CancellationToken ct = default);
    Task<ServiceOperationResult<OrdenCompraDto>> AgregarDetalleAsync(int ordenCompraId, OrdenCompraAgregarDetalleDto request, CancellationToken ct = default);
    Task<ServiceOperationResult<object>> ActualizarEstadoAsync(int ordenCompraId, OrdenCompraActualizarEstadoDto request, CancellationToken ct = default);
    Task<ServiceOperationResult<object>> RegistrarRecepcionAsync(OrdenCompraRegistrarRecepcionDto request, CancellationToken ct = default);
    Task<ServiceOperationResult<OrdenCompraDto>> ObtenerOrdenAsync(int ordenCompraId, CancellationToken ct = default);
    Task<ServiceOperationResult<IReadOnlyList<OrdenCompraDto>>> ListarOrdenesAsync(OrdenCompraListarFiltrosDto filtros, CancellationToken ct = default);
}
