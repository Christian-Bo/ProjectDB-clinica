namespace Clinica.Application.DTOs.Catalogos;

public sealed record CatalogoItemDto
{
    public int    Id          { get; init; }
    public string Nombre      { get; init; } = string.Empty;
    public string Label       { get; init; } = string.Empty;
    public string? Codigo     { get; init; }
    public string? Descripcion { get; init; }
    public bool   Activo      { get; init; }
}

public sealed record PacienteItemDto
{
    public long   PacienteId       { get; init; }
    public string Label            { get; init; } = string.Empty;
    public string? NumeroExpediente { get; init; }
    public string? Documento       { get; init; }
    public string? Telefono        { get; init; }
    public string? CorreoElectronico { get; init; }
    public bool   EsDiscapacitado  { get; init; }
}

public sealed record CitaItemDto
{
    public long   CitaId        { get; init; }
    public long   PacienteId    { get; init; }
    public string Label         { get; init; } = string.Empty;
    public string FechaInicio   { get; init; } = string.Empty;
    public string Estado        { get; init; } = string.Empty;
    public string PacienteNombre { get; init; } = string.Empty;
    public string ServicioNombre { get; init; } = string.Empty;
    public string SedeNombre    { get; init; } = string.Empty;
    public string? MedicoNombre { get; init; }
}

public sealed record KioscoVentanillaDto
{
    public int KioscoVentanillaId { get; init; }
    public int SedeId             { get; init; }
    public string SedeNombre      { get; init; } = string.Empty;
    public int ServicioId         { get; init; }
    public string ServicioNombre  { get; init; } = string.Empty;
    public int? EspecialidadId    { get; init; }
    public string? EspecialidadNombre { get; init; }
    public int NumeroVentanilla   { get; init; }
    public string VentanillaNombre { get; init; } = string.Empty;
    public bool Activo            { get; init; }
}

public sealed record KioscoVentanillaConfigRequest
{
    public int SedeId           { get; init; }
    public int ServicioId       { get; init; }
    public int NumeroVentanilla { get; init; }
    public bool Activo          { get; init; } = true;
    public int? UsuarioId       { get; init; }
}
