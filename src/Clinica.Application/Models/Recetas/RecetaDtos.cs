namespace Clinica.Application.Models.Recetas;

// =============================================================================
// REQUEST DTOs
// =============================================================================

public sealed class CrearRecetaRequestDto
{
    /// <summary>Consulta desde la cual se emite la receta. Debe estar cerrada.</summary>
    public long ConsultaId { get; set; }

    /// <summary>Usuario que emite la receta (desde JWT o header de prueba).</summary>
    public int? UsuarioId { get; set; }

    /// <summary>Medicamentos incluidos en la receta.</summary>
    public IList<RecetaItemRequestDto> Items { get; set; } = [];
}

public sealed class RecetaItemRequestDto
{
    public int MedicamentoId { get; set; }
    public string Dosis { get; set; } = string.Empty;
    public string Frecuencia { get; set; } = string.Empty;
    public int DuracionDias { get; set; }
    public string? Indicaciones { get; set; }
}

// =============================================================================
// RESPONSE DTOs
// =============================================================================

public sealed class RecetaResponseDto
{
    public long RecetaId { get; set; }
    public long ConsultaId { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public string MedicoNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaDespacho { get; set; }
    public IReadOnlyList<RecetaItemResponseDto> Items { get; set; } = [];
}

public sealed class RecetaItemResponseDto
{
    public int MedicamentoId { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string PrincipioActivo { get; set; } = string.Empty;
    public string Dosis { get; set; } = string.Empty;
    public string Frecuencia { get; set; } = string.Empty;
    public int DuracionDias { get; set; }
    public string? Indicaciones { get; set; }
}
