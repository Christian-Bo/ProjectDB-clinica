namespace Clinica.Application.Models.Reportes;

// =============================================================================
// DTOs — Reportes / ETL
// SP: sp_ETL_CargarIncrementalDW
// =============================================================================

/// <summary>Resultado de ejecutar sp_ETL_CargarIncrementalDW.</summary>
public sealed class EtlResultDto
{
    public int RegistrosProcesados { get; init; }

    /// <summary>OK | ERROR</summary>
    public string Estado { get; init; } = string.Empty;
}
