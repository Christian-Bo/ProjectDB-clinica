using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Pacientes;
using Clinica.Application.Exceptions;
using Clinica.Infrastructure.Repositories;

namespace Clinica.Infrastructure.Services;

public sealed class PacientesService : IPacientesService
{
    private readonly PacientesRepository _repo;

    public PacientesService(PacientesRepository repo)
    {
        _repo = repo;
    }

    public async Task<PacienteResponseDto?> ObtenerAsync(int pacienteId)
    {
        var paciente = await _repo.ObtenerAsync(pacienteId);
        if (paciente is null)
            throw new NotFoundException($"Paciente {pacienteId} no encontrado.");
        return paciente;
    }

    public async Task<PacienteResponseDto> UpsertAsync(PacienteUpsertDto dto)
    {
        var result = await _repo.UpsertAsync(dto);

        if (!result.Success)
            throw new BusinessException(result.Codigo, result.Mensaje);

        var pacienteId = dto.PacienteId ?? int.Parse(result.Codigo.Split(':').Last());
        var paciente = await _repo.ObtenerAsync(pacienteId);
        return paciente!;
    }

    public async Task<List<AlergiaResponseDto>> ListarAlergiasAsync(int pacienteId)
    {
        return await _repo.ListarAlergiasAsync(pacienteId);
    }

    public async Task AgregarAlergiaAsync(int pacienteId, AlergiaRequestDto dto)
    {
        var result = await _repo.AgregarAlergiaAsync(pacienteId, dto);
        if (!result.Success)
            throw new BusinessException(result.Codigo, result.Mensaje);
    }

    public async Task QuitarAlergiaAsync(int pacienteId, int alergiaId)
    {
        var result = await _repo.QuitarAlergiaAsync(pacienteId, alergiaId);
        if (!result.Success)
            throw new BusinessException(result.Codigo, result.Mensaje);
    }
}