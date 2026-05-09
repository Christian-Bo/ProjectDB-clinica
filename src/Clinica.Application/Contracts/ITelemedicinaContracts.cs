using Clinica.Application.Models.Common;
using Clinica.Application.Models.Telemedicina;

namespace Clinica.Application.Contracts;

public interface ISesionesTelemedicinaService
{
    Task<ServiceOperationResult<SesionTelemedicaDto>> UpsertSesionAsync(SesionTelemedicaUpsertDto request, CancellationToken ct = default);
    Task<ServiceOperationResult<SesionTelemedicaDto>> ObtenerSesionAsync(long? sesionTeleId, long? citaId, CancellationToken ct = default);
    Task<ServiceOperationResult<IReadOnlyList<SesionTelemedicaDto>>> ListarSesionesAsync(SesionListarFiltrosDto filtros, CancellationToken ct = default);
}
