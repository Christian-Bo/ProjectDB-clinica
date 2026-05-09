using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Catalogos;
using Clinica.Application.DTOs.Pantalla;
using Clinica.Application.DTOs.Tickets;
using Clinica.Infrastructure.Repositories;

namespace Clinica.Infrastructure.Services;

/// <summary>Servicio de Tickets — delega operaciones al repositorio que invoca SPs.</summary>
public sealed class TicketsService(TicketsRepository repo) : ITicketsService
{
    public Task<TicketDto> GenerarTicketAsync(GenerarTicketRequest r, Guid? key, CancellationToken ct) =>
        repo.GenerarTicketAsync(r.CitaId, r.PacienteId, r.SedeId, r.ServicioId,
            r.MedicoId, r.PrioridadSolicitada, r.MotivoEspecial, r.UsuarioId, key, ct);

    public Task<TicketDto> GenerarTicketEspecialAsync(GenerarTicketEspecialRequest r, Guid? key, CancellationToken ct) =>
        repo.GenerarTicketEspecialAsync(r.CitaId, r.PacienteId, r.SedeId, r.ServicioId,
            r.MedicoId, r.MotivoEspecial, r.UsuarioId, key, ct);

    public Task<TicketDto> GenerarTicketKioscoAsync(GenerarTicketKioscoRequest r, Guid? key, CancellationToken ct) =>
        repo.GenerarTicketKioscoAsync(r.PacienteId, r.DocumentoPaciente, r.UsarPacienteNoAplica,
            r.SedeId, r.ServicioId, r.PrioridadSolicitada, r.MotivoEspecial, r.UsuarioId, key, ct);

    public Task<TicketDto> LlamarSiguienteAsync(LlamarSiguienteRequest r, CancellationToken ct) =>
        repo.LlamarSiguienteAsync(r.SedeId, r.ServicioId, r.EstacionId, r.UsuarioId, ct);

    public Task<TicketDto> MarcarEnAtencionAsync(long ticketId, CancellationToken ct) =>
        repo.MarcarEnAtencionAsync(ticketId, ct);

    public Task<TicketDto> FinalizarTicketAsync(long ticketId, string? motivo, CancellationToken ct) =>
        repo.FinalizarTicketAsync(ticketId, motivo, ct);

    public Task<TicketDto> CancelarTicketAsync(long ticketId, string? motivo, int? usuarioId, CancellationToken ct) =>
        repo.CancelarTicketAsync(ticketId, motivo, usuarioId, ct);

    public Task<TicketDto> RellamarTicketAsync(long ticketId, int? usuarioId, CancellationToken ct) =>
        repo.RellamarTicketAsync(ticketId, usuarioId, ct);

    public Task<NoShowResultDto> ProcesarNoShowAsync(CancellationToken ct) =>
        repo.ProcesarNoShowAsync(ct);

    public Task<List<TicketDto>> ListarTicketsAsync(int? sedeId, int? servicioId, string? estado, CancellationToken ct) =>
        repo.ListarTicketsAsync(sedeId, servicioId, estado, ct);

    public Task<TicketDto> ObtenerTicketAsync(long ticketId, CancellationToken ct) =>
        repo.ObtenerTicketAsync(ticketId, ct);

    public Task<TicketDto> ObtenerTicketPorNumeroAsync(string numero, CancellationToken ct) =>
        repo.ObtenerTicketPorNumeroAsync(numero, ct);

    public async Task<TicketDto> ObtenerMiTicketAsync(long? ticketId, string? numero, CancellationToken ct)
    {
        if (ticketId.HasValue)
            return await repo.ObtenerTicketAsync(ticketId.Value, ct);

        if (!string.IsNullOrWhiteSpace(numero))
            return await repo.ObtenerTicketPorNumeroAsync(numero, ct);

        throw new ArgumentException("Debe proporcionar ticketId o numeroTicket.");
    }

    public Task<ResumenOperativoDto> ObtenerResumenOperativoAsync(int? sedeId, int? servicioId, CancellationToken ct) =>
        repo.ObtenerResumenOperativoAsync(sedeId, servicioId, ct);
}

/// <summary>Servicio de Pantalla Pública.</summary>
public sealed class PantallaService(PantallaRepository repo) : IPantallaService
{
    public Task<PantallaColaDto> ObtenerColaAsync(int sedeId, int servicioId, CancellationToken ct) =>
        repo.ObtenerColaAsync(sedeId, servicioId, ct);

    public Task<PantallaColaDto> ObtenerColaAsync(int sedeId, IReadOnlyCollection<int> servicioIds, CancellationToken ct) =>
        repo.ObtenerColaAsync(sedeId, servicioIds, ct);
}

/// <summary>Servicio de Catálogos para Recepción.</summary>
public sealed class CatalogosRecepcionService(CatalogosRepository repo) : ICatalogosRecepcionService
{
    public Task<List<CatalogoItemDto>> ListarSedesAsync(CancellationToken ct)                                                     => repo.ListarSedesAsync(ct);
    public Task<List<CatalogoItemDto>> ListarServiciosAsync(int? sedeId, CancellationToken ct)                                    => repo.ListarServiciosAsync(sedeId, ct);
    public Task<List<CatalogoItemDto>> ListarEstacionesAsync(int? sedeId, CancellationToken ct)                                   => repo.ListarEstacionesAsync(sedeId, ct);
    public Task<List<PacienteItemDto>> ListarPacientesAsync(string? texto, int limit, CancellationToken ct)                       => repo.ListarPacientesAsync(texto, limit, ct);
    public Task<List<CitaItemDto>>     ListarCitasConfirmadasAsync(int? sedeId, int? servicioId, string? texto, CancellationToken ct) => repo.ListarCitasConfirmadasAsync(sedeId, servicioId, texto, ct);
    public Task<List<CatalogoItemDto>> ListarPrioridadesTicketAsync(CancellationToken ct)                                         => repo.ListarPrioridadesAsync(ct);
    public Task<List<CatalogoItemDto>> ListarEstadosTicketAsync(CancellationToken ct)                                             => repo.ListarEstadosAsync(ct);
    public Task<List<KioscoVentanillaDto>> ListarKioscoVentanillasAsync(int sedeId, CancellationToken ct)                         => repo.ListarKioscoVentanillasAsync(sedeId, ct);
    public Task<KioscoVentanillaDto> ConfigurarKioscoVentanillaAsync(KioscoVentanillaConfigRequest request, CancellationToken ct)  => repo.ConfigurarKioscoVentanillaAsync(request, ct);
}
