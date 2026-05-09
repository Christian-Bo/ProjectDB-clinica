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
            throw new BusinessException(result.Mensaje, result.Codigo);

        var pacienteId = dto.PacienteId ?? GetRequiredPacienteId(result);
        var paciente = await _repo.ObtenerAsync(pacienteId);
        return paciente!;
    }


    private static int GetRequiredPacienteId(Clinica.Infrastructure.Database.SpResult result)
    {
        if (result.EntityId.HasValue)
        {
            return checked((int)result.EntityId.Value);
        }

        throw new BusinessException("El SP no devolvio el PacienteId.", "ERROR_INTERNO");
    }

    public async Task<List<AlergiaResponseDto>> ListarAlergiasAsync(int pacienteId)
    {
        return await _repo.ListarAlergiasAsync(pacienteId);
    }

    public async Task AgregarAlergiaAsync(int pacienteId, AlergiaRequestDto dto)
    {
        var result = await _repo.AgregarAlergiaAsync(pacienteId, dto);
        if (!result.Success)
            throw new BusinessException(result.Mensaje, result.Codigo);
    }

    public async Task QuitarAlergiaAsync(int pacienteId, int alergiaId)
    {
        var result = await _repo.QuitarAlergiaAsync(pacienteId, alergiaId);
        if (!result.Success)
            throw new BusinessException(result.Mensaje, result.Codigo);
    }
    public async Task<PacienteResponseDto?> ObtenerPorUsuarioAsync(int usuarioId)
    {
        return await _repo.ObtenerPorUsuarioAsync(usuarioId);
    }
}