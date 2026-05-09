namespace Clinica.Application.DTOs.Auth;

public sealed class RegistroRequestDto
{
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string TipoDocumento { get; set; } = "DPI";
    public string NumeroDocumento { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Genero { get; set; } = string.Empty;
    public string Nacionalidad { get; set; } = "Guatemalteca";
    public string? TipoSangre { get; set; }
}