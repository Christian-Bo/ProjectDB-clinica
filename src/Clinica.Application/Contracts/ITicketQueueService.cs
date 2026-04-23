using Clinica.Application.Models.Common;
using Clinica.Application.Models.Tickets;

namespace Clinica.Application.Contracts;

// -----------------------------------------------------------------------------
// Contrato del modulo 3.
// Todos los controladores hablan con este contrato y nunca con SQL directo.
// Esto facilita pruebas, mantenimiento y evolucion futura.
// -----------------------------------------------------------------------------
public interface ITicketQueueService
{
    Task<ServiceOperationResult<TicketDetailDto>> GenerateTicketAsync(GenerateTicketRequestDto request, Guid? idempotencyKey, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<TicketDetailDto>> CallNextAsync(CallNextTicketRequestDto request, Guid? idempotencyKey, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<TicketDetailDto>> MarkInAttentionAsync(long ticketId, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<TicketDetailDto>> FinishAsync(long ticketId, FinalizeTicketRequestDto request, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<NoShowProcessResponseDto>> ProcessNoShowAsync(CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<IReadOnlyList<TicketDetailDto>>> ListAsync(TicketListFiltersDto filters, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<ReceptionOperationalSummaryDto>> GetOperationalSummaryAsync(int? sedeId, int? servicioId, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<TicketDetailDto>> GetByIdAsync(long ticketId, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<TicketDetailDto>> GetByNumberAsync(string numeroTicket, CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<QueueDisplayResponseDto>> GetQueueDisplayAsync(int sedeId, int servicioId, CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetSedesAsync(CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetServiciosAsync(int? sedeId, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetStationsAsync(int? sedeId, string? tipoEstacion, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<IReadOnlyList<PatientSelectionDto>>> GetPatientsAsync(string? texto, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<IReadOnlyList<AppointmentSelectionDto>>> GetConfirmedAppointmentsAsync(int? sedeId, int? servicioId, string? texto, CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetTicketPrioritiesAsync(CancellationToken cancellationToken = default);
    Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetTicketStatesAsync(CancellationToken cancellationToken = default);
}
