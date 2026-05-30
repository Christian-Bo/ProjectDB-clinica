namespace Clinica.Application.Models.Reportes;

// =============================================================================
// DTOs — Reportes / ETL
// SPs:
//   - sp_ETL_CargarIncrementalDW
//   - sp_Reportes_DashboardDecision
// =============================================================================

/// <summary>Resultado de ejecutar sp_ETL_CargarIncrementalDW.</summary>
public sealed class EtlResultDto
{
    public int RegistrosProcesados { get; init; }

    /// <summary>OK | ERROR</summary>
    public string Estado { get; init; } = string.Empty;
}

/// <summary>Dashboard analítico para toma de decisiones administrativas.</summary>
public sealed class EtlDecisionDashboardDto
{
    public EtlDecisionResumenDto Resumen { get; init; } = new();
    public IReadOnlyList<EtlDecisionTrendPointDto> TendenciaAtenciones { get; init; } = Array.Empty<EtlDecisionTrendPointDto>();
    public IReadOnlyList<EtlDecisionHourPointDto> EsperaPorHora { get; init; } = Array.Empty<EtlDecisionHourPointDto>();
    public IReadOnlyList<EtlDecisionPriorityPointDto> Prioridades { get; init; } = Array.Empty<EtlDecisionPriorityPointDto>();
    public IReadOnlyList<EtlDecisionServicePerformanceDto> RendimientoServicios { get; init; } = Array.Empty<EtlDecisionServicePerformanceDto>();
    public IReadOnlyList<EtlDecisionMedicationPointDto> RecetasTop { get; init; } = Array.Empty<EtlDecisionMedicationPointDto>();
    public IReadOnlyList<EtlDecisionAlertDto> Alertas { get; init; } = Array.Empty<EtlDecisionAlertDto>();
    public IReadOnlyList<EtlDecisionProcessInfoDto> Procesos { get; init; } = Array.Empty<EtlDecisionProcessInfoDto>();
}

public sealed class EtlDecisionResumenDto
{
    public int TotalAtenciones { get; init; }
    public int TotalTickets { get; init; }
    public int TotalCitas { get; init; }
    public int TotalOrdenes { get; init; }
    public decimal TotalRecetas { get; init; }
    public decimal EsperaPromMin { get; init; }
    public decimal SlaPromedio { get; init; }
    public DateTime? UltimaEjecucion { get; init; }
    public int RegistrosUltimaEjecucion { get; init; }
    public int? DuracionMs { get; init; }
    public string EstadoEtl { get; init; } = "PENDIENTE";
}

public sealed class EtlDecisionTrendPointDto
{
    public int FechaKey { get; init; }
    public string Fecha { get; init; } = string.Empty;
    public string DiaNombre { get; init; } = string.Empty;
    public int Atenciones { get; init; }
    public decimal EsperaPromMin { get; init; }
    public decimal SlaPorcentaje { get; init; }
}

public sealed class EtlDecisionHourPointDto
{
    public int HoraKey { get; init; }
    public string HoraLabel { get; init; } = string.Empty;
    public int TotalTickets { get; init; }
    public decimal EsperaPromMin { get; init; }
}

public sealed class EtlDecisionPriorityPointDto
{
    public string Prioridad { get; init; } = string.Empty;
    public int TotalTickets { get; init; }
    public decimal Porcentaje { get; init; }
}

public sealed class EtlDecisionServicePerformanceDto
{
    public string SedeNombre { get; init; } = string.Empty;
    public string ServicioNombre { get; init; } = string.Empty;
    public string EspecialidadNombre { get; init; } = string.Empty;
    public int TotalAtenciones { get; init; }
    public decimal EsperaPromMin { get; init; }
    public decimal SlaPorcentaje { get; init; }
}

public sealed class EtlDecisionMedicationPointDto
{
    public string MedicamentoNombre { get; init; } = string.Empty;
    public string ServicioNombre { get; init; } = string.Empty;
    public decimal TotalUnidades { get; init; }
}

public sealed class EtlDecisionAlertDto
{
    public string Nivel { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
    public string Recomendacion { get; init; } = string.Empty;
}

public sealed class EtlDecisionProcessInfoDto
{
    public string Cubo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public long Registros { get; init; }
    public DateTime? UltimaCarga { get; init; }
}
