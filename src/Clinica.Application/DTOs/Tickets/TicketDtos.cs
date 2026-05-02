namespace Clinica.Application.DTOs.Tickets;

// ── Requests ────────────────────────────────────────────────────────────────

public sealed record GenerarTicketRequest
{
    public long?  CitaId               { get; init; }
    public long?  PacienteId           { get; init; }
    public int?   SedeId               { get; init; }
    public int?   ServicioId           { get; init; }
    public int?   MedicoId             { get; init; }
    public string PrioridadSolicitada  { get; init; } = "NORMAL";
    public string? MotivoEspecial      { get; init; }
    public int?   UsuarioId            { get; init; }
}

public sealed record GenerarTicketEspecialRequest
{
    public long?  CitaId         { get; init; }
    public long?  PacienteId     { get; init; }
    public int?   SedeId         { get; init; }
    public int?   ServicioId     { get; init; }
    public int?   MedicoId       { get; init; }
    public string MotivoEspecial { get; init; } = string.Empty;
    public int?   UsuarioId      { get; init; }
}

public sealed record LlamarSiguienteRequest
{
    public int  SedeId      { get; init; }
    public int  ServicioId  { get; init; }
    public int? EstacionId  { get; init; }
    public int? UsuarioId   { get; init; }
}

public sealed record FinalizarTicketRequest
{
    public string? Motivo { get; init; }
}

// ── Responses ───────────────────────────────────────────────────────────────

public sealed record TicketDto
{
    public long    TicketId              { get; init; }
    public string  NumeroTicket          { get; init; } = string.Empty;
    public string  Estado                { get; init; } = string.Empty;
    public string  Prioridad             { get; init; } = string.Empty;
    public bool    EsEspecial            { get; init; }
    public string? MotivoEspecial        { get; init; }
    public long?   CitaId                { get; init; }
    public string? CitaEstado            { get; init; }
    public long    PacienteId            { get; init; }
    public string  PacienteNombre        { get; init; } = string.Empty;
    public string? NumeroExpediente      { get; init; }
    public string? PacienteDocumento     { get; init; }
    public int     SedeId                { get; init; }
    public string  SedeNombre            { get; init; } = string.Empty;
    public int     ServicioId            { get; init; }
    public string  ServicioNombre        { get; init; } = string.Empty;
    public string? EspecialidadNombre    { get; init; }
    public int?    MedicoId              { get; init; }
    public string? MedicoNombre          { get; init; }
    public int?    ConsultorioId         { get; init; }
    public string? ConsultorioNombre     { get; init; }
    public int?    AutorizadoPorId       { get; init; }
    public string? AutorizadoPorNombre   { get; init; }
    public DateTime  FechaGeneracion     { get; init; }
    public DateTime? FechaLlamado        { get; init; }
    public DateTime? FechaInicioAtencion { get; init; }
    public DateTime? FechaFinAtencion    { get; init; }
    public int     ContadorLlamados      { get; init; }
}

public sealed record NoShowResultDto
{
    public int RegistrosProcesados { get; init; }
}

public sealed record ResumenOperativoDto
{
    public int?   SedeId                { get; init; }
    public string? SedeNombre           { get; init; }
    public int?   ServicioId            { get; init; }
    public string? ServicioNombre       { get; init; }
    public int    TicketsEnEspera       { get; init; }
    public int    TicketsLlamados       { get; init; }
    public int    TicketsEnAtencion     { get; init; }
    public int    TicketsFinalizados    { get; init; }
    public int    TicketsNoShow         { get; init; }
    public int    TicketsEspecialesHoy  { get; init; }
    public string? UltimoTicketLlamado  { get; init; }
    public DateTime ConsultadoEnUtc     { get; init; } = DateTime.UtcNow;
}
