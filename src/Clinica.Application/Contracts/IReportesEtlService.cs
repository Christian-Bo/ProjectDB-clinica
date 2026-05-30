using Clinica.Application.Models.Common;
using Clinica.Application.Models.Reportes;

namespace Clinica.Application.Contracts;

public interface IReportesEtlService
{
    Task<ServiceOperationResult<EtlResultDto>> EjecutarEtlAsync(CancellationToken ct = default);
    Task<ServiceOperationResult<EtlDecisionDashboardDto>> ObtenerDashboardDecisionAsync(int dias = 30, CancellationToken ct = default);
}
