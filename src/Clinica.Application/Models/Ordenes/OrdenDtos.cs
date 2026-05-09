namespace Clinica.Application.Models.Ordenes;

// =============================================================================
// REQUEST DTOs
// =============================================================================

public sealed class CrearOrdenRequestDto
{
    /// <summary>Consulta desde la cual se emite la orden.</summary>
    public long ConsultaId { get; set; }

    /// <summary>Tipo: LABORATORIO o IMAGEN.</summary>
    public string TipoOrden { get; set; } = string.Empty;

    /// <summary>Descripción o indicación clínica de la orden.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Urgencia: NORMAL, URGENTE.</summary>
    public string? Urgencia { get; set; } = "NORMAL";

    /// <summary>Usuario que emite la orden (desde JWT o header de prueba).</summary>
    public int? UsuarioId { get; set; }
}

public sealed class ActualizarEstadoOrdenRequestDto
{
    /// <summary>Nuevo estado: PENDIENTE, EN_PROCESO, COMPLETADA, CANCELADA.</summary>
    public string NuevoEstado { get; set; } = string.Empty;

    /// <summary>Observación del cambio de estado (para trazabilidad).</summary>
    public string? Observacion { get; set; }

    /// <summary>Usuario que ejecuta el cambio (desde JWT o header de prueba).</summary>
    public int? UsuarioId { get; set; }
}

public sealed class OrdenListFiltersDto
{
    public int? PacienteId { get; set; }
    public int? MedicoId { get; set; }
    public string? Estado { get; set; }
    public string? TipoOrden { get; set; }
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; } = 1;
}

// =============================================================================
// RESPONSE DTOs
// =============================================================================

public sealed class OrdenResponseDto
{
    public long OrdenId { get; set; }
    public long ConsultaId { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public string MedicoNombre { get; set; } = string.Empty;
    public string TipoOrden { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Urgencia { get; set; }
    public DateTime FechaEmision { get; set; }
    public IReadOnlyList<OrdenHistorialDto> Historial { get; set; } = [];
}

public sealed class OrdenHistorialDto
{
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime FechaCambio { get; set; }
}

public sealed class ListaOrdenesResponseDto
{
    public int Total { get; set; }
    public IReadOnlyList<OrdenResponseDto> Items { get; set; } = [];
}
