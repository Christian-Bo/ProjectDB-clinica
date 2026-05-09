using Clinica.Application.Models.Common;
using Clinica.Application.Models.Ordenes;

namespace Clinica.Application.Contracts;

// -----------------------------------------------------------------------------
// Contrato del servicio de Órdenes (laboratorio / imagen).
// Crea órdenes desde una consulta, consulta órdenes y actualiza su estado.
// La trazabilidad de cambios de estado la maneja sp_ActualizarEstadoOrden.
// -----------------------------------------------------------------------------
public interface IOrdenesService
{
    Task<ServiceOperationResult<OrdenResponseDto>> CrearAsync(
        CrearOrdenRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<OrdenResponseDto>> ObtenerAsync(
        long ordenId,
        CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<ListaOrdenesResponseDto>> ListarAsync(
        OrdenListFiltersDto filters,
        CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<OrdenResponseDto>> ActualizarEstadoAsync(
        long ordenId,
        ActualizarEstadoOrdenRequestDto request,
        CancellationToken cancellationToken = default);
}
