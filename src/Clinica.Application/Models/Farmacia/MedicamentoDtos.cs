using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.Models.Farmacia;

// =============================================================================
// DTOs — Farmacia / Medicamentos
// Tabla real: dbo.Medicamentos
// SPs: sp_Medicamento_Upsert | sp_Medicamento_Obtener | sp_Medicamento_Listar
// =============================================================================

/// <summary>
/// Crear (MedicamentoId = null) o actualizar (MedicamentoId con valor) un medicamento.
/// Parámetros basados en sp_Medicamento_Upsert exactamente.
/// </summary>
public sealed class MedicamentoUpsertDto
{
    public int? MedicamentoId { get; set; }

    /// <summary>Código interno del sistema. Obligatorio y único. Ej: MED-0001</summary>
    [Required, MaxLength(30)]
    public string CodigoInterno { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CodigoBarras { get; set; }

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? NombreGenerico { get; set; }

    [Required, MaxLength(200)]
    public string PrincipioActivo { get; set; } = string.Empty;

    /// <summary>ALOPÁTICO | VITAMINA | SUPLEMENTO | BIOLÓGICO | HERBAL | OTRO</summary>
    [MaxLength(50)]
    public string Tipo { get; set; } = "ALOPÁTICO";

    [MaxLength(100)]
    public string? Presentacion { get; set; }

    [MaxLength(100)]
    public string? ConcentracionDescripcion { get; set; }

    [MaxLength(30)]
    public string UnidadMedida { get; set; } = "UNIDAD";

    public bool RequiereReceta { get; set; } = true;
    public bool ControladoPorSalud { get; set; } = false;

    public decimal? PrecioCompra { get; set; }

    [Required]
    public decimal PrecioVenta { get; set; }

    public int StockMinimo { get; set; } = 10;

    /// <summary>ACTIVO | DESCONTINUADO | AGOTADO</summary>
    [MaxLength(20)]
    public string Estado { get; set; } = "ACTIVO";
}

/// <summary>Respuesta de lectura de un medicamento desde dbo.Medicamentos.</summary>
public sealed class MedicamentoDto
{
    public int MedicamentoId { get; init; }
    public string CodigoInterno { get; init; } = string.Empty;
    public string? CodigoBarras { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? NombreGenerico { get; init; }
    public string PrincipioActivo { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string? Presentacion { get; init; }
    public string? ConcentracionDescripcion { get; init; }
    public string UnidadMedida { get; init; } = string.Empty;
    public bool RequiereReceta { get; init; }
    public bool ControladoPorSalud { get; init; }
    public decimal? PrecioCompra { get; init; }
    public decimal PrecioVenta { get; init; }
    public int StockMinimo { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }
}

/// <summary>Filtros para sp_Medicamento_Listar.</summary>
public sealed class MedicamentoListarFiltrosDto
{
    /// <summary>ACTIVO | DESCONTINUADO | AGOTADO — null devuelve todos</summary>
    public string? Estado { get; set; }

    /// <summary>Busca en Nombre, PrincipioActivo y CodigoInterno</summary>
    public string? Texto { get; set; }
}

/// <summary>Receta pendiente de despacho en farmacia.</summary>
public sealed class RecetaPendienteDto
{
    public long RecetaId { get; init; }
    public long ConsultaId { get; init; }
    public int PacienteId { get; init; }
    public string PacienteNombre { get; init; } = string.Empty;
    public int? MedicoId { get; init; }
    public string MedicoNombre { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaEmision { get; init; }
    public string? Observaciones { get; init; }
    public int TotalMedicamentos { get; init; }
}