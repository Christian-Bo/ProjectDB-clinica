using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Citas;
using Clinica.Application.Exceptions;
using Clinica.Infrastructure.Repositories;

namespace Clinica.Infrastructure.Services;

public sealed class CitasService : ICitasService
{
    private readonly CitasRepository _repo;

    public CitasService(CitasRepository repo)
    {
        _repo = repo;
    }

    public async Task<CitaResponseDto> ReservarAsync(ReservarCitaRequestDto dto, Guid idempotencyKey, int usuarioId)
    {
        var result = await _repo.ReservarAsync(dto, idempotencyKey, usuarioId);

        if (result.HttpStatus == 409)
            throw new ConflictException(result.Mensaje, result.Codigo);

        if (!result.Success)
            throw new BusinessException(result.Mensaje, result.Codigo);

        var citaId = result.EntityId
            ?? throw new BusinessException("El SP no devolvio el CitaId.", "ERROR_INTERNO");

        var cita = await _repo.ObtenerAsync(citaId);
        return cita!;
    }

    public async Task<CitaResponseDto> ConfirmarAsync(long citaId, ConfirmarCitaRequestDto dto, Guid idempotencyKey)
    {
        var result = await _repo.ConfirmarAsync(citaId, dto.UsuarioId, idempotencyKey);

        if (result.HttpStatus == 409)
            throw new ConflictException(result.Mensaje, result.Codigo);

        if (!result.Success)
            throw new BusinessException(result.Mensaje, result.Codigo);

        var cita = await _repo.ObtenerAsync(citaId);
        return cita!;
    }

    public async Task CancelarAsync(long citaId, CancelarCitaRequestDto dto)
    {
        var result = await _repo.CancelarAsync(citaId, dto.UsuarioId, dto.MotivoCancelacion);
        if (!result.Success)
            throw new BusinessException(result.Mensaje, result.Codigo);
    }

    public async Task ReprogramarAsync(long citaId, ReprogramarCitaRequestDto dto)
    {
        var result = await _repo.ReprogramarAsync(citaId, dto);

        if (result.HttpStatus == 409)
            throw new ConflictException(result.Mensaje, result.Codigo);

        if (!result.Success)
            throw new BusinessException(result.Mensaje, result.Codigo);
    }

    public async Task<CitaResponseDto?> ObtenerAsync(long citaId)
    {
        var cita = await _repo.ObtenerAsync(citaId);
        if (cita is null)
            throw new NotFoundException($"Cita {citaId} no encontrada.");
        return cita;
    }

    public async Task<List<CitaResponseDto>> ListarAsync(ListarCitasRequestDto filtros)
    {
        return await _repo.ListarAsync(filtros);
    }
}