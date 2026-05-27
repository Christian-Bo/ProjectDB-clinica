using Clinica.Application.DTOs.Operativo;
using Clinica.Application.Models.Common;

namespace Clinica.Application.Contracts;

public interface IOperativoCatalogosService
{
    Task<List<LookupItemDto>> LookupAsync(string tipo, int? sedeId, int? servicioId, int? especialidadId, int? medicoId, int? consultorioId, int? usuarioId, string? busqueda, int top, CancellationToken ct = default);
    Task<List<AgendaSlotDto>> ListarDisponibilidadAsync(int sedeId, DateTime fecha, int? servicioId, int? especialidadId, int? medicoId, bool soloDisponibles, CancellationToken ct = default);
}

public interface ISecretariaService
{
    Task<List<SecretariaContextoDto>> ObtenerContextosAsync(int usuarioId, CancellationToken ct = default);
    Task<SecretariaContextoDto?> ConfigurarContextoAsync(SecretariaConfigurarContextoRequest request, CancellationToken ct = default);
    Task<List<SecretariaTicketDto>> ListarColaAsync(int usuarioId, int sedeId, int? servicioId, int estacionId, string? estado, int top, CancellationToken ct = default);
    Task<ServiceOperationResult<SecretariaTicketDto>> TomarSiguienteAsync(SecretariaTomarSiguienteRequest request, CancellationToken ct = default);
    Task<ServiceOperationResult<SecretariaTicketDto>> RegistrarAsistenciaAsync(long ticketId, SecretariaRegistrarAsistenciaRequest request, CancellationToken ct = default);
    Task<ServiceOperationResult<SecretariaTicketDto>> EnviarMedicoAsync(long ticketId, SecretariaEnviarMedicoRequest request, CancellationToken ct = default);
    Task<ServiceOperationResult<object>> MarcarNoShowAsync(long ticketId, SecretariaNoShowRequest request, CancellationToken ct = default);
    Task<SecretariaResumenDto?> ObtenerResumenAsync(int usuarioId, int sedeId, int? servicioId, int estacionId, CancellationToken ct = default);
}

public interface IMedicoColaService
{
    Task<MedicoContextoDto?> ObtenerContextoAsync(int usuarioId, CancellationToken ct = default);
    Task<List<SecretariaTicketDto>> ListarColaAsync(int medicoId, int? sedeId, int? consultorioId, int top, CancellationToken ct = default);
    Task<ServiceOperationResult<SecretariaTicketDto>> LlamarSiguienteAsync(MedicoLlamarSiguienteRequest request, CancellationToken ct = default);
    Task<ServiceOperationResult<SecretariaTicketDto>> MarcarEnConsultaAsync(long ticketId, MedicoMarcarEnConsultaRequest request, CancellationToken ct = default);
}

public interface INotificacionConfiguracionService
{
    Task<List<NotificacionConfiguracionDto>> ObtenerAsync(CancellationToken ct = default);
    Task<ServiceOperationResult<List<NotificacionConfiguracionDto>>> GuardarAsync(GuardarNotificacionConfiguracionRequest request, CancellationToken ct = default);
    Task<ServiceOperationResult<object>> ProbarAsync(string canal, CancellationToken ct = default);
}
