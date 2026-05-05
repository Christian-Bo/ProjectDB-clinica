namespace Clinica.Application.DTOs.Pantalla;

public sealed record ColaTicketPreviewDto
{
    public long    TicketId          { get; init; }
    public string  NumeroTicket      { get; init; } = string.Empty;
    public string  PacienteNombre    { get; init; } = string.Empty;
    public string  Prioridad         { get; init; } = string.Empty;
    public string  Estado            { get; init; } = string.Empty;
    public string? FechaReferencia   { get; init; }
    public int?    SedeId            { get; init; }
    public string? SedeNombre        { get; init; }
    public int?    ServicioId        { get; init; }
    public string? ServicioNombre    { get; init; }
    public int?    ConsultorioId     { get; init; }
    public string? ConsultorioNombre { get; init; }
    public string? VentanillaNombre  { get; init; }
}

public sealed record PantallaColaDto
{
    public int    SedeId          { get; init; }
    public string SedeNombre      { get; init; } = string.Empty;
    public int?   ServicioId      { get; init; }
    public List<int> ServicioIds  { get; init; } = [];
    public string ServicioNombre  { get; init; } = string.Empty;
    public string ServiciosNombre { get; init; } = string.Empty;

    // Compatibilidad con vistas anteriores.
    public ColaTicketPreviewDto? Actual { get; init; }
    public List<ColaTicketPreviewDto> Proximos { get; init; } = [];

    // Pantalla pública mejorada.
    public ColaTicketPreviewDto? UltimoLlamado { get; init; }
    public List<ColaTicketPreviewDto> TicketsLlamados { get; init; } = [];
    public List<ColaTicketPreviewDto> UltimosLlamados { get; init; } = [];

    public DateTime ConsultadoEnUtc { get; init; } = DateTime.UtcNow;
}
