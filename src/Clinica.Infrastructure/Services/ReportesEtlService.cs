using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Reportes;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// =============================================================================
// ReportesEtlService
// SPs:
//   - dbo.sp_ETL_CargarIncrementalDW
//   - dbo.sp_Reportes_DashboardDecision
//
// Importante: este servicio NO usa SQL inline. Toda la lógica analítica vive en SPs.
// =============================================================================
public sealed class ReportesEtlService : IReportesEtlService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<ReportesEtlService> _logger;

    public ReportesEtlService(DatabaseConnection db, ILogger<ReportesEtlService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<EtlResultDto>> EjecutarEtlAsync(CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandType    = CommandType.StoredProcedure;
        cmd.CommandText    = "dbo.sp_ETL_CargarIncrementalDW";
        cmd.CommandTimeout = 300;

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return new ServiceOperationResult<EtlResultDto>
                {
                    HttpStatus = 500,
                    Code = "SP_SIN_RESPUESTA",
                    Message = "El ETL no devolvió resultado.",
                    Data = new EtlResultDto { RegistrosProcesados = 0, Estado = "ERROR" }
                };

            var status    = reader.GetInt32OrDefault("HttpStatus", 500);
            var code      = reader.GetNullableString("Codigo") ?? "ETL_ERROR";
            var msg       = reader.GetNullableString("Mensaje") ?? string.Empty;
            var registros = reader.GetNullableInt32("Registros") ?? 0;

            return new ServiceOperationResult<EtlResultDto>
            {
                HttpStatus = status,
                Code = code,
                Message = msg,
                Data = new EtlResultDto
                {
                    RegistrosProcesados = registros,
                    Estado = status is >= 200 and < 300 ? "OK" : "ERROR"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando sp_ETL_CargarIncrementalDW");
            return new ServiceOperationResult<EtlResultDto>
            {
                HttpStatus = 500,
                Code = "ERROR_ETL",
                Message = ex.Message,
                Data = new EtlResultDto { RegistrosProcesados = 0, Estado = "ERROR" }
            };
        }
    }

    public async Task<ServiceOperationResult<EtlDecisionDashboardDto>> ObtenerDashboardDecisionAsync(
        int dias = 30,
        CancellationToken ct = default)
    {
        var diasNormalizados = dias switch
        {
            < 7 => 7,
            > 180 => 180,
            _ => dias
        };

        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandType    = CommandType.StoredProcedure;
        cmd.CommandText    = "dbo.sp_Reportes_DashboardDecision";
        cmd.CommandTimeout = 120;
        cmd.Parameters.Add(new SqlParameter("@Dias", SqlDbType.Int) { Value = diasNormalizados });

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var resumen = new EtlDecisionResumenDto();
            if (await reader.ReadAsync(ct))
            {
                resumen = new EtlDecisionResumenDto
                {
                    TotalAtenciones = reader.GetInt32OrDefault("TotalAtenciones"),
                    TotalTickets = reader.GetInt32OrDefault("TotalTickets"),
                    TotalCitas = reader.GetInt32OrDefault("TotalCitas"),
                    TotalOrdenes = reader.GetInt32OrDefault("TotalOrdenes"),
                    TotalRecetas = GetDecimalOrDefault(reader, "TotalRecetas"),
                    EsperaPromMin = GetDecimalOrDefault(reader, "EsperaPromMin"),
                    SlaPromedio = GetDecimalOrDefault(reader, "SlaPromedio"),
                    UltimaEjecucion = reader.GetNullableDateTime("UltimaEjecucion"),
                    RegistrosUltimaEjecucion = reader.GetInt32OrDefault("RegistrosUltimaEjecucion"),
                    DuracionMs = reader.GetNullableInt32("DuracionMs"),
                    EstadoEtl = reader.GetNullableString("EstadoEtl") ?? "PENDIENTE"
                };
            }

            await reader.NextResultAsync(ct);
            var tendencia = new List<EtlDecisionTrendPointDto>();
            while (await reader.ReadAsync(ct))
            {
                tendencia.Add(new EtlDecisionTrendPointDto
                {
                    FechaKey = reader.GetInt32OrDefault("FechaKey"),
                    Fecha = reader.GetNullableString("Fecha") ?? string.Empty,
                    DiaNombre = reader.GetNullableString("DiaNombre") ?? string.Empty,
                    Atenciones = reader.GetInt32OrDefault("Atenciones"),
                    EsperaPromMin = GetDecimalOrDefault(reader, "EsperaPromMin"),
                    SlaPorcentaje = GetDecimalOrDefault(reader, "SlaPorcentaje")
                });
            }

            await reader.NextResultAsync(ct);
            var esperaPorHora = new List<EtlDecisionHourPointDto>();
            while (await reader.ReadAsync(ct))
            {
                esperaPorHora.Add(new EtlDecisionHourPointDto
                {
                    HoraKey = reader.GetInt32OrDefault("HoraKey"),
                    HoraLabel = reader.GetNullableString("HoraLabel") ?? string.Empty,
                    TotalTickets = reader.GetInt32OrDefault("TotalTickets"),
                    EsperaPromMin = GetDecimalOrDefault(reader, "EsperaPromMin")
                });
            }

            await reader.NextResultAsync(ct);
            var prioridades = new List<EtlDecisionPriorityPointDto>();
            while (await reader.ReadAsync(ct))
            {
                prioridades.Add(new EtlDecisionPriorityPointDto
                {
                    Prioridad = reader.GetNullableString("Prioridad") ?? string.Empty,
                    TotalTickets = reader.GetInt32OrDefault("TotalTickets"),
                    Porcentaje = GetDecimalOrDefault(reader, "Porcentaje")
                });
            }

            await reader.NextResultAsync(ct);
            var rendimiento = new List<EtlDecisionServicePerformanceDto>();
            while (await reader.ReadAsync(ct))
            {
                rendimiento.Add(new EtlDecisionServicePerformanceDto
                {
                    SedeNombre = reader.GetNullableString("SedeNombre") ?? string.Empty,
                    ServicioNombre = reader.GetNullableString("ServicioNombre") ?? string.Empty,
                    EspecialidadNombre = reader.GetNullableString("EspecialidadNombre") ?? string.Empty,
                    TotalAtenciones = reader.GetInt32OrDefault("TotalAtenciones"),
                    EsperaPromMin = GetDecimalOrDefault(reader, "EsperaPromMin"),
                    SlaPorcentaje = GetDecimalOrDefault(reader, "SlaPorcentaje")
                });
            }

            await reader.NextResultAsync(ct);
            var recetasTop = new List<EtlDecisionMedicationPointDto>();
            while (await reader.ReadAsync(ct))
            {
                recetasTop.Add(new EtlDecisionMedicationPointDto
                {
                    MedicamentoNombre = reader.GetNullableString("MedicamentoNombre") ?? string.Empty,
                    ServicioNombre = reader.GetNullableString("ServicioNombre") ?? string.Empty,
                    TotalUnidades = GetDecimalOrDefault(reader, "TotalUnidades")
                });
            }

            await reader.NextResultAsync(ct);
            var alertas = new List<EtlDecisionAlertDto>();
            while (await reader.ReadAsync(ct))
            {
                alertas.Add(new EtlDecisionAlertDto
                {
                    Nivel = reader.GetNullableString("Nivel") ?? "INFO",
                    Titulo = reader.GetNullableString("Titulo") ?? string.Empty,
                    Mensaje = reader.GetNullableString("Mensaje") ?? string.Empty,
                    Recomendacion = reader.GetNullableString("Recomendacion") ?? string.Empty
                });
            }

            await reader.NextResultAsync(ct);
            var procesos = new List<EtlDecisionProcessInfoDto>();
            while (await reader.ReadAsync(ct))
            {
                procesos.Add(new EtlDecisionProcessInfoDto
                {
                    Cubo = reader.GetNullableString("Cubo") ?? string.Empty,
                    Descripcion = reader.GetNullableString("Descripcion") ?? string.Empty,
                    Registros = reader.GetInt64OrDefault("Registros"),
                    UltimaCarga = reader.GetNullableDateTime("UltimaCarga")
                });
            }

            return new ServiceOperationResult<EtlDecisionDashboardDto>
            {
                HttpStatus = 200,
                Code = "REPORTES_OK",
                Message = "Dashboard analítico cargado correctamente.",
                Data = new EtlDecisionDashboardDto
                {
                    Resumen = resumen,
                    TendenciaAtenciones = tendencia,
                    EsperaPorHora = esperaPorHora,
                    Prioridades = prioridades,
                    RendimientoServicios = rendimiento,
                    RecetasTop = recetasTop,
                    Alertas = alertas,
                    Procesos = procesos
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando sp_Reportes_DashboardDecision");
            return new ServiceOperationResult<EtlDecisionDashboardDto>
            {
                HttpStatus = 500,
                Code = "ERROR_REPORTES_DW",
                Message = ex.Message,
                Data = new EtlDecisionDashboardDto()
            };
        }
    }

    private static decimal GetDecimalOrDefault(SqlDataReader reader, string columnName, decimal defaultValue = 0m)
    {
        if (!reader.HasColumn(columnName))
        {
            return defaultValue;
        }

        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : Convert.ToDecimal(reader.GetValue(ordinal));
    }
}
