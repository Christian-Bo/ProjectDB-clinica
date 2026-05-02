using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Auth;
using Clinica.Infrastructure.Repositories;
using Clinica.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace Clinica.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AuthRepository    _repo;
    private readonly PasswordHasher    _hasher;
    private readonly JwtTokenGenerator _jwt;
    private readonly IConfiguration    _config;

    public AuthService(
        AuthRepository    repo,
        PasswordHasher    hasher,
        JwtTokenGenerator jwt,
        IConfiguration    config)
    {
        _repo   = repo;
        _hasher = hasher;
        _jwt    = jwt;
        _config = config;
    }

    public async Task<(bool Success, string? ErrorCode, string Message, LoginResponseDto? Data)>
        LoginAsync(LoginRequestDto request)
    {
        var usuario = await _repo.ObtenerPorUsernameAsync(request.Username);
        if (usuario is null)
            return (false, "CREDENCIALES_INVALIDAS", "Usuario o contrasena incorrectos.", null);

        if (usuario.Estado == "BLOQUEADO")
            return (false, "CUENTA_BLOQUEADA", "La cuenta esta bloqueada.", null);

        if (usuario.Estado != "ACTIVO")
            return (false, "CUENTA_INACTIVA", "La cuenta no esta activa.", null);

        Console.WriteLine($"=== DEBUG LOGIN ===");
        Console.WriteLine($"Password: [{request.Password}]");
        Console.WriteLine($"Hash: [{usuario.PasswordHash}]");
        var verificacion = _hasher.Verify(request.Password, usuario.PasswordHash);
        Console.WriteLine($"Verificacion: {verificacion}");
        Console.WriteLine($"==================");

        if (!verificacion)
        {
            await _repo.RegistrarIntentoFallidoAsync(usuario.CorreoElectronico, null);
            return (false, "CREDENCIALES_INVALIDAS", "Usuario o contrasena incorrectos.", null);
        }

        var roles = string.IsNullOrWhiteSpace(usuario.RolesActivos)
            ? new List<string>()
            : usuario.RolesActivos
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .ToList();

        var token   = _jwt.Generate(usuario.UsuarioId, usuario.NombreUsuario,
                                    $"{usuario.Nombres} {usuario.Apellidos}", roles);
        var minutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        await _repo.RegistrarSesionAsync(usuario.UsuarioId, token, minutes);

        return (true, null, "Inicio de sesion correcto.", new LoginResponseDto
        {
            AccessToken = token,
            ExpiresIn   = minutes * 60,
            User = new UserProfileDto
            {
                UsuarioId      = usuario.UsuarioId,
                Username       = usuario.NombreUsuario,
                NombreCompleto = $"{usuario.Nombres} {usuario.Apellidos}",
                Email          = usuario.CorreoElectronico,
                Estado         = usuario.Estado,
                RequiereCambio = usuario.RequiereCambioPassword,
                Roles          = roles
            }
        });
    }

    public async Task<(bool Success, UserProfileDto? Data)> GetMeAsync(int usuarioId)
    {
        var usuario = await _repo.ObtenerPorUsernameAsync(usuarioId.ToString());
        if (usuario is null) return (false, null);

        var roles = string.IsNullOrWhiteSpace(usuario.RolesActivos)
            ? new List<string>()
            : usuario.RolesActivos
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .ToList();

        return (true, new UserProfileDto
        {
            UsuarioId      = usuario.UsuarioId,
            Username       = usuario.NombreUsuario,
            NombreCompleto = $"{usuario.Nombres} {usuario.Apellidos}",
            Email          = usuario.CorreoElectronico,
            Estado         = usuario.Estado,
            RequiereCambio = usuario.RequiereCambioPassword,
            Roles          = roles
        });
    }

    public async Task<(bool Success, string? ErrorCode, string Message, object? Data)>
        RegistrarPacienteAsync(RegistroRequestDto dto)
    {
        var passwordHash = _hasher.Hash(dto.Password);
        var salt = Guid.NewGuid().ToString();

        var result = await _repo.RegistrarPacienteAsync(
            dto.Nombres, dto.Apellidos, dto.CorreoElectronico,
            passwordHash, salt, dto.Telefono,
            dto.TipoDocumento, dto.NumeroDocumento,
            dto.FechaNacimiento, dto.Genero,
            dto.Nacionalidad, dto.TipoSangre);

        if (!result.Success)
            return (false, result.ErrorCode, result.Message, null);

        return (true, null, result.Message, new { result.UsuarioId, result.PacienteId });
    }
}