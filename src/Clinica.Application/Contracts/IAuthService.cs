using Clinica.Application.DTOs.Auth;

namespace Clinica.Application.Contracts;

public interface IAuthService
{
    Task<(bool Success, string? ErrorCode, string Message, LoginResponseDto? Data)>
        LoginAsync(LoginRequestDto request);

    Task<(bool Success, UserProfileDto? Data)>
        GetMeAsync(int usuarioId);

    Task<(bool Success, string? ErrorCode, string Message, RegistroUsuarioResponseDto? Data)>
        RegistrarUsuarioAsync(RegistroUsuarioRequestDto request);

    Task<(bool Success, string? ErrorCode, string Message, object? Data)>
        RegistrarPacienteAsync(RegistroRequestDto dto);
}