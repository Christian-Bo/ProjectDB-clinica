using Clinica.Application.DTOs.Citas;

namespace Clinica.Application.Contracts;

public interface ICitasService
{
    Task<CitaResponseDto> ReservarAsync(ReservarCitaRequestDto dto, Guid idempotencyKey, int usuarioId);
    Task<CitaResponseDto> ConfirmarAsync(long citaId, ConfirmarCitaRequestDto dto, Guid idempotencyKey);
    Task CancelarAsync(long citaId, CancelarCitaRequestDto dto);
    Task ReprogramarAsync(long citaId, ReprogramarCitaRequestDto dto);
    Task<CitaResponseDto?> ObtenerAsync(long citaId);
    Task<List<CitaResponseDto>> ListarAsync(ListarCitasRequestDto filtros);
}