namespace Clinica.Application.DTOs.Pacientes;

public sealed class PacienteUpsertDto
{
    public int? PacienteId { get; set; }
    public int? UsuarioId { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Genero { get; set; } = string.Empty;
    public string? Ocupacion { get; set; }
    public string Nacionalidad { get; set; } = "Guatemalteca";
    public string? DireccionResidencia { get; set; }
    public int? MunicipioId { get; set; }
    public string? ContactoEmergenciaNombre { get; set; }
    public string? ContactoEmergenciaTelefono { get; set; }
    public string? ContactoEmergenciaRelacion { get; set; }
    public string? TipoSangre { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class PacienteResponseDto
{
    public int PacienteId { get; set; }
    public int? UsuarioId { get; set; }
    public string NumeroExpediente { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Genero { get; set; } = string.Empty;
    public string? Ocupacion { get; set; }
    public string Nacionalidad { get; set; } = string.Empty;
    public string? DireccionResidencia { get; set; }
    public string? TipoSangre { get; set; }
    public string? ContactoEmergenciaNombre { get; set; }
    public string? ContactoEmergenciaTelefono { get; set; }
    public string? ContactoEmergenciaRelacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}

public sealed class AlergiaRequestDto
{
    public int PrincipioActivoId { get; set; }
    public string? Severidad { get; set; }
    public string? Observaciones { get; set; }
}

public sealed class AlergiaResponseDto
{
    public int AlergiaId { get; set; }
    public int PacienteId { get; set; }
    public int PrincipioActivoId { get; set; }
    public string NombrePrincipioActivo { get; set; } = string.Empty;
    public string? Severidad { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaRegistro { get; set; }
}