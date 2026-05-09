using Clinica.Application.Models.Common;
using Clinica.Application.Models.Notificaciones;

namespace Clinica.Application.Contracts;

public interface IPlantillasNotificacionService
{
    Task<ServiceOperationResult<PlantillaNotificacionDto>> UpsertPlantillaAsync(PlantillaNotificacionUpsertDto request, CancellationToken ct = default);
    Task<ServiceOperationResult<PlantillaNotificacionDto>> ObtenerPlantillaAsync(int plantillaId, CancellationToken ct = default);
    Task<ServiceOperationResult<IReadOnlyList<PlantillaNotificacionDto>>> ListarPlantillasAsync(PlantillaListarFiltrosDto filtros, CancellationToken ct = default);
}

public interface IColaNotificacionesService
{
    Task<ServiceOperationResult<object>> EncolarAsync(EncolarNotificacionRequestDto request, CancellationToken ct = default);
    Task<ServiceOperationResult<IReadOnlyList<NotificacionPendienteDto>>> ListarPendientesAsync(ColaListarFiltrosDto filtros, CancellationToken ct = default);
    Task<ServiceOperationResult<ProcesarColaResultDto>> ProcesarColaAsync(CancellationToken ct = default);
}
