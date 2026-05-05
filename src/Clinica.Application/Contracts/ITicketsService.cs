using Clinica.Application.DTOs.Catalogos;
using Clinica.Application.DTOs.Pantalla;
using Clinica.Application.DTOs.Tickets;

namespace Clinica.Application.Contracts;

public interface ITicketsService
{
    Task<TicketDto> GenerarTicketAsync(GenerarTicketRequest request, Guid? idempotencyKey, CancellationToken ct = default);
    Task<TicketDto> GenerarTicketEspecialAsync(GenerarTicketEspecialRequest request, Guid? idempotencyKey, CancellationToken ct = default);
    Task<TicketDto> GenerarTicketKioscoAsync(GenerarTicketKioscoRequest request, Guid? idempotencyKey, CancellationToken ct = default);
    Task<TicketDto> LlamarSiguienteAsync(LlamarSiguienteRequest request, CancellationToken ct = default);
    Task<TicketDto> MarcarEnAtencionAsync(long ticketId, CancellationToken ct = default);
    Task<TicketDto> FinalizarTicketAsync(long ticketId, string? motivo, CancellationToken ct = default);
    Task<TicketDto> CancelarTicketAsync(long ticketId, string? motivo, int? usuarioId, CancellationToken ct = default);
    Task<TicketDto> RellamarTicketAsync(long ticketId, int? usuarioId, CancellationToken ct = default);
    Task<NoShowResultDto> ProcesarNoShowAsync(CancellationToken ct = default);
    Task<List<TicketDto>> ListarTicketsAsync(int? sedeId, int? servicioId, string? estado, CancellationToken ct = default);
    Task<TicketDto> ObtenerTicketAsync(long ticketId, CancellationToken ct = default);
    Task<TicketDto> ObtenerTicketPorNumeroAsync(string numeroTicket, CancellationToken ct = default);
    Task<TicketDto> ObtenerMiTicketAsync(long? ticketId, string? numeroTicket, CancellationToken ct = default);
    Task<ResumenOperativoDto> ObtenerResumenOperativoAsync(int? sedeId, int? servicioId, CancellationToken ct = default);
}

public interface IPantallaService
{
    Task<PantallaColaDto> ObtenerColaAsync(int sedeId, int servicioId, CancellationToken ct = default);
    Task<PantallaColaDto> ObtenerColaAsync(int sedeId, IReadOnlyCollection<int> servicioIds, CancellationToken ct = default);
}

public interface ICatalogosRecepcionService
{
    Task<List<CatalogoItemDto>> ListarSedesAsync(CancellationToken ct = default);
    Task<List<CatalogoItemDto>> ListarServiciosAsync(int? sedeId, CancellationToken ct = default);
    Task<List<CatalogoItemDto>> ListarEstacionesAsync(int? sedeId, CancellationToken ct = default);
    Task<List<PacienteItemDto>> ListarPacientesAsync(string? texto, int limit, CancellationToken ct = default);
    Task<List<CitaItemDto>> ListarCitasConfirmadasAsync(int? sedeId, int? servicioId, string? texto, CancellationToken ct = default);
    Task<List<CatalogoItemDto>> ListarPrioridadesTicketAsync(CancellationToken ct = default);
    Task<List<CatalogoItemDto>> ListarEstadosTicketAsync(CancellationToken ct = default);
    Task<List<KioscoVentanillaDto>> ListarKioscoVentanillasAsync(int sedeId, CancellationToken ct = default);
    Task<KioscoVentanillaDto> ConfigurarKioscoVentanillaAsync(KioscoVentanillaConfigRequest request, CancellationToken ct = default);
}
