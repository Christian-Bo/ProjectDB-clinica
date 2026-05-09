namespace Clinica.Domain.Models;

public sealed class UsuarioAuthModel
{
    public int    UsuarioId              { get; init; }
    public string NombreUsuario          { get; init; } = string.Empty;
    public string CorreoElectronico      { get; init; } = string.Empty;
    public string PasswordHash           { get; init; } = string.Empty;
    public string Nombres                { get; init; } = string.Empty;
    public string Apellidos              { get; init; } = string.Empty;
    public string Estado                 { get; init; } = string.Empty;
    public bool   RequiereCambioPassword { get; init; }
    public string? RolesActivos          { get; init; }
}