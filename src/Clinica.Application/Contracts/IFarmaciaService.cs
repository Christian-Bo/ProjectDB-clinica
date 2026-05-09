using Clinica.Application.Models.Common;
using Clinica.Application.Models.Farmacia;

namespace Clinica.Application.Contracts;

public interface IFarmaciaService
{
    Task<ServiceOperationResult<MedicamentoDto>> UpsertMedicamentoAsync(
        MedicamentoUpsertDto request, CancellationToken ct = default);

    Task<ServiceOperationResult<MedicamentoDto>> ObtenerMedicamentoAsync(
        int medicamentoId, string? codigoInterno, CancellationToken ct = default);

    Task<ServiceOperationResult<IReadOnlyList<MedicamentoDto>>> ListarMedicamentosAsync(
        MedicamentoListarFiltrosDto filtros, CancellationToken ct = default);

    Task<ServiceOperationResult<object>> DespacharRecetaAsync(
        long recetaId, int usuarioId, string? observaciones, CancellationToken ct = default);

    Task<ServiceOperationResult<IReadOnlyList<RecetaPendienteDto>>> ListarRecetasPendientesAsync(
        int? pacienteId, string? texto, CancellationToken ct = default);
}