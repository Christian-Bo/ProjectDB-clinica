using Clinica.Application.DTOs.Pacientes;

namespace Clinica.Application.Contracts;

public interface IPacientesService
{
    Task<PacienteResponseDto?> ObtenerAsync(int pacienteId);
    Task<PacienteResponseDto> UpsertAsync(PacienteUpsertDto dto);
    Task<List<AlergiaResponseDto>> ListarAlergiasAsync(int pacienteId);
    Task AgregarAlergiaAsync(int pacienteId, AlergiaRequestDto dto);
    Task QuitarAlergiaAsync(int pacienteId, int alergiaId);
}