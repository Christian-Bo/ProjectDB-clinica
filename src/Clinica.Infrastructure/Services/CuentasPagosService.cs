using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Cobros;
using Clinica.Application.Models.Common;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// =============================================================================
// CuentasService
// SP: sp_GenerarCuentaDesdeCita
//   Params: @CitaId BIGINT, @CreadoPor INT, @Detalles TVP dbo.TVP_DetallesCuenta
//   TVP cols: TipoConcepto NVARCHAR(20), Descripcion NVARCHAR(200),
//             Cantidad DECIMAL(8,2), PrecioUnitario DECIMAL(10,2)
//   Devuelve: HttpStatus | Codigo | CuentaId | Mensaje
// =============================================================================
public sealed class CuentasService : ICuentasService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<CuentasService> _logger;

    public CuentasService(DatabaseConnection db, ILogger<CuentasService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<CuentaDto>> GenerarDesdeCitaAsync(
        GenerarCuentaRequestDto req, Guid? idempotencyKey, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_GenerarCuentaDesdeCita");
        P(cmd, "@CitaId",    req.CitaId);
        P(cmd, "@CreadoPor", req.CreadoPor);

        // TVP dbo.TVP_DetallesCuenta
        var tvp = new DataTable();
        tvp.Columns.Add("TipoConcepto",   typeof(string));
        tvp.Columns.Add("Descripcion",    typeof(string));
        tvp.Columns.Add("Cantidad",       typeof(decimal));
        tvp.Columns.Add("PrecioUnitario", typeof(decimal));

        foreach (var d in req.Detalles)
            tvp.Rows.Add(d.TipoConcepto, d.Descripcion, d.Cantidad, d.PrecioUnitario);

        var tvpParam = cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured));
        tvpParam.TypeName = "dbo.TVP_DetallesCuenta";
        tvpParam.Value    = tvp;

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return Err("SP_SIN_RESPUESTA", "El SP no devolvió resultado.");

            var status   = reader.GetInt32OrDefault("HttpStatus", 500);
            var code     = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO";
            var msg      = reader.GetNullableString("Mensaje") ?? string.Empty;
            var cuentaId = reader.GetNullableInt64("CuentaId");

            if (status is < 200 or >= 300)
                return new ServiceOperationResult<CuentaDto>
                    { HttpStatus = status, Code = code, Message = msg };

            CuentaDto? data = null;
            if (cuentaId.HasValue)
                data = await CargarCuentaAsync(conn, cuentaId.Value, ct);

            return new ServiceOperationResult<CuentaDto>
                { HttpStatus = status, Code = code, Message = msg, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_GenerarCuentaDesdeCita");
            return Err("ERROR_INFRAESTRUCTURA", ex.Message);
        }
    }

    private async Task<CuentaDto?> CargarCuentaAsync(SqlConnection conn, long cuentaId, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 30;
        cmd.CommandText = @"
SELECT TOP (1)
    c.CuentaId, c.CitaId, c.PacienteId,
    COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(u.Nombres,''),' ',ISNULL(u.Apellidos,'')))), ''),
             CONCAT('Paciente #', c.PacienteId)) AS PacienteNombre,
    ISNULL(c.SubtotalConsulta,0)        AS SubtotalConsulta,
    ISNULL(c.SubtotalMedicamentos,0)    AS SubtotalMedicamentos,
    ISNULL(c.SubtotalProcedimientos,0)  AS SubtotalProcedimientos,
    ISNULL(c.Descuento,0)               AS Descuento,
    ISNULL(c.Total,0)                   AS Total,
    ISNULL(c.Saldo,0)                   AS Saldo,
    c.Estado, c.FechaEmision, c.FechaPago
FROM dbo.Cuentas c
JOIN  dbo.Pacientes p ON p.PacienteId = c.PacienteId
LEFT JOIN dbo.Usuarios u ON u.UsuarioId = p.UsuarioId
WHERE c.CuentaId = @CuentaId;";
        cmd.Parameters.Add(new SqlParameter("@CuentaId", SqlDbType.BigInt) { Value = cuentaId });

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new CuentaDto
        {
            CuentaId               = r.GetInt64OrDefault("CuentaId"),
            CitaId                 = r.GetInt64OrDefault("CitaId"),
            PacienteId             = r.GetInt32OrDefault("PacienteId"),
            PacienteNombre         = r.GetNullableString("PacienteNombre") ?? string.Empty,
            SubtotalConsulta       = Dec(r, "SubtotalConsulta"),
            SubtotalMedicamentos   = Dec(r, "SubtotalMedicamentos"),
            SubtotalProcedimientos = Dec(r, "SubtotalProcedimientos"),
            Descuento              = Dec(r, "Descuento"),
            Total                  = Dec(r, "Total"),
            Saldo                  = Dec(r, "Saldo"),
            Estado                 = r.GetNullableString("Estado") ?? string.Empty,
            FechaEmision           = r.GetDateTimeOrDefault("FechaEmision"),
            FechaPago              = r.GetNullableDateTime("FechaPago")
        };
    }

    private static decimal Dec(SqlDataReader r, string col)
    {
        for (var i = 0; i < r.FieldCount; i++)
            if (string.Equals(r.GetName(i), col, StringComparison.OrdinalIgnoreCase))
            {
                var ord = r.GetOrdinal(col);
                return r.IsDBNull(ord) ? 0m : Convert.ToDecimal(r.GetValue(ord));
            }
        return 0m;
    }

    private static SqlCommand Sp(SqlConnection c, string n) =>
        new() { Connection = c, CommandType = CommandType.StoredProcedure, CommandText = n, CommandTimeout = 60 };
    private static void P(SqlCommand cmd, string n, object? v) =>
        cmd.Parameters.Add(new SqlParameter(n, v ?? DBNull.Value));
    private static ServiceOperationResult<CuentaDto> Err(string code, string msg) =>
        new() { HttpStatus = 500, Code = code, Message = msg };
}

// =============================================================================
// PagosService
// SP: sp_RegistrarPagoCuenta
//   Params: @CuentaId, @MetodoPagoId, @Monto, @Referencia, @ComprobanteUrl,
//           @Observaciones, @RegistradoPor, @IdempotencyKey UNIQUEIDENTIFIER
//   Devuelve: HttpStatus | Codigo | PagoId | Mensaje
// =============================================================================
public sealed class PagosService : IPagosService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<PagosService> _logger;

    public PagosService(DatabaseConnection db, ILogger<PagosService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<PagoDto>> RegistrarPagoAsync(
        RegistrarPagoRequestDto req, Guid? idempotencyKey, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_RegistrarPagoCuenta");
        P(cmd, "@CuentaId",       req.CuentaId);
        P(cmd, "@MetodoPagoId",   req.MetodoPagoId);
        P(cmd, "@Monto",          req.Monto);
        P(cmd, "@Referencia",     req.Referencia);
        P(cmd, "@ComprobanteUrl", req.ComprobanteUrl);
        P(cmd, "@Observaciones",  req.Observaciones);
        P(cmd, "@RegistradoPor",  req.RegistradoPor);
        P(cmd, "@IdempotencyKey", idempotencyKey);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return Err("SP_SIN_RESPUESTA", "El SP no devolvió resultado.");

            var status = reader.GetInt32OrDefault("HttpStatus", 500);
            var code   = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO";
            var msg    = reader.GetNullableString("Mensaje") ?? string.Empty;
            var pagoId = reader.GetNullableInt64("PagoId");

            if (status is < 200 or >= 300)
                return new ServiceOperationResult<PagoDto>
                    { HttpStatus = status, Code = code, Message = msg };

            var data = new PagoDto
            {
                PagoId       = pagoId ?? 0,
                CuentaId     = req.CuentaId,
                MetodoPagoId = req.MetodoPagoId,
                Monto        = req.Monto,
                Referencia   = req.Referencia,
                FechaPago    = DateTime.UtcNow
            };

            return new ServiceOperationResult<PagoDto>
                { HttpStatus = status, Code = code, Message = msg, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_RegistrarPagoCuenta");
            return Err("ERROR_INFRAESTRUCTURA", ex.Message);
        }
    }

    private static SqlCommand Sp(SqlConnection c, string n) =>
        new() { Connection = c, CommandType = CommandType.StoredProcedure, CommandText = n, CommandTimeout = 60 };
    private static void P(SqlCommand cmd, string n, object? v) =>
        cmd.Parameters.Add(new SqlParameter(n, v ?? DBNull.Value));
    private static ServiceOperationResult<PagoDto> Err(string code, string msg) =>
        new() { HttpStatus = 500, Code = code, Message = msg };
}
