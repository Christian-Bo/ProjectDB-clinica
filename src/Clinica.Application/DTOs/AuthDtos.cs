using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.DTOs.Auth;

public sealed class LoginRequestDto
{
    [Required] public string Username { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
}

public sealed class LoginResponseDto
{
    public string         AccessToken   { get; init; } = string.Empty;
    public int            ExpiresIn     { get; init; }
    public UserProfileDto User          { get; init; } = new();
}

public sealed class UserProfileDto
{
    public int          UsuarioId       { get; init; }
    public string       Username        { get; init; } = string.Empty;
    public string       NombreCompleto  { get; init; } = string.Empty;
    public string       Email           { get; init; } = string.Empty;
    public string       Estado          { get; init; } = string.Empty;
    public bool         RequiereCambio  { get; init; }
    public List<string> Roles           { get; init; } = new();
}