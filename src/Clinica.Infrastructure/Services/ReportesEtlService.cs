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
// SP: sp_ETL_CargarIncrementalDW (sin parámetros de entrada)
//   Devuelve: HttpStatus | Codigo | Registros | Mensaje
//   Timeout: 300 s — el ETL puede mover muchos registros al DW.
//   USO: solo administrativo / batch. NO invocar desde flujos clínicos.
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
}
