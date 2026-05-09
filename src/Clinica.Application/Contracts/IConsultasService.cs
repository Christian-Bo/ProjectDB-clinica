using Clinica.Application.Models.Common;
using Clinica.Application.Models.Consultas;

namespace Clinica.Application.Contracts;

// -----------------------------------------------------------------------------
// Contrato del servicio de Consulta Médica.
// Define las operaciones del módulo Dev4:
// - abrir consulta desde ticket válido
// - cerrar consulta (la deja inmutable)
// - agregar nota de corrección (append-only)
// - obtener consulta completa con datos relacionados
// - historial clínico del paciente
// -----------------------------------------------------------------------------
public interface IConsultasService
{
    Task<ServiceOperationResult<ConsultaResponseDto>> AbrirDesdeTicketAsync(
        AbrirConsultaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<ConsultaResponseDto>> CerrarAsync(
        long consultaId,
        CerrarConsultaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<ConsultaResponseDto>> AgregarNotaCorreccionAsync(
        long consultaId,
        NotaCorreccionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<ConsultaResponseDto>> ObtenerAsync(
        long consultaId,
        CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<HistorialClinicoResponseDto>> ObtenerHistorialAsync(
        int pacienteId,
        CancellationToken cancellationToken = default);
}
