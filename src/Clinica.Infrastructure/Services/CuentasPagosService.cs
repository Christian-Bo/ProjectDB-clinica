using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Cobros;
using Clinica.Application.Models.Common;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

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

    public async Task<ServiceOperationResult<CuentaDetalleDto>> ObtenerAsync(
        long cuentaId, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_Cuenta_Obtener");
        P(cmd, "@CuentaId", cuentaId);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return new ServiceOperationResult<CuentaDetalleDto>
                    { HttpStatus = 404, Code = "CUENTA_NO_ENCONTRADA", Message = "Cuenta no encontrada." };

            var cuenta = new CuentaDto
            {
                CuentaId               = reader.GetInt64OrDefault("CuentaId"),
                CitaId                 = reader.GetInt64OrDefault("CitaId"),
                PacienteId             = reader.GetInt32OrDefault("PacienteId"),
                PacienteNombre         = reader.GetNullableString("PacienteNombre") ?? string.Empty,
                SubtotalConsulta       = Dec(reader, "SubtotalConsulta"),
                SubtotalMedicamentos   = Dec(reader, "SubtotalMedicamentos"),
                SubtotalProcedimientos = Dec(reader, "SubtotalProcedimientos"),
                Descuento              = Dec(reader, "Descuento"),
                Total                  = Dec(reader, "Total"),
                Saldo                  = Dec(reader, "Saldo"),
                Estado                 = reader.GetNullableString("Estado") ?? string.Empty,
                FechaEmision           = reader.GetDateTimeOrDefault("FechaEmision"),
                FechaPago              = reader.GetNullableDateTime("FechaPago")
            };

            var detalle = new List<DetalleCuentaLineaDto>();
            if (await reader.NextResultAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    detalle.Add(new DetalleCuentaLineaDto
                    {
                        CuentaDetalleId = reader.GetInt64OrDefault("CuentaDetalleId"),
                        CuentaId        = reader.GetInt64OrDefault("CuentaId"),
                        TipoConcepto    = reader.GetNullableString("TipoConcepto") ?? string.Empty,
                        Descripcion     = reader.GetNullableString("Descripcion") ?? string.Empty,
                        Cantidad        = Dec(reader, "Cantidad"),
                        PrecioUnitario  = Dec(reader, "PrecioUnitario"),
                        Subtotal        = Dec(reader, "Subtotal")
                    });
                }
            }

            var pagos = new List<PagoDto>();
            if (await reader.NextResultAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    pagos.Add(new PagoDto
                    {
                        PagoId       = reader.GetInt64OrDefault("PagoId"),
                        CuentaId     = reader.GetInt64OrDefault("CuentaId"),
                        MetodoPagoId = reader.GetInt32OrDefault("MetodoPagoId"),
                        Monto        = Dec(reader, "Monto"),
                        Referencia   = reader.GetNullableString("Referencia"),
                        FechaPago    = reader.GetDateTimeOrDefault("FechaPago")
                    });
                }
            }

            return new ServiceOperationResult<CuentaDetalleDto>
            {
                HttpStatus = 200,
                Code    = "CUENTA_OK",
                Message = "Cuenta obtenida correctamente.",
                Data    = new CuentaDetalleDto
                {
                    Cuenta  = cuenta,
                    Detalle = detalle,
                    Pagos   = pagos
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_Cuenta_Obtener");
            return new ServiceOperationResult<CuentaDetalleDto>
                { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = ex.Message };
        }
    }

    public async Task<ServiceOperationResult<IReadOnlyList<CuentaDto>>> ListarAsync(
        int? pacienteId, string? estado, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_Cuenta_Listar");
        P(cmd, "@PacienteId", pacienteId);
        P(cmd, "@Estado",     estado);

        try
        {
            var list = new List<CuentaDto>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new CuentaDto
                {
                    CuentaId               = reader.GetInt64OrDefault("CuentaId"),
                    CitaId                 = reader.GetInt64OrDefault("CitaId"),
                    PacienteId             = reader.GetInt32OrDefault("PacienteId"),
                    PacienteNombre         = reader.GetNullableString("PacienteNombre") ?? string.Empty,
                    SubtotalConsulta       = Dec(reader, "SubtotalConsulta"),
                    SubtotalMedicamentos   = Dec(reader, "SubtotalMedicamentos"),
                    SubtotalProcedimientos = Dec(reader, "SubtotalProcedimientos"),
                    Descuento              = Dec(reader, "Descuento"),
                    Total                  = Dec(reader, "Total"),
                    Saldo                  = Dec(reader, "Saldo"),
                    Estado                 = reader.GetNullableString("Estado") ?? string.Empty,
                    FechaEmision           = reader.GetDateTimeOrDefault("FechaEmision"),
                    FechaPago              = reader.GetNullableDateTime("FechaPago")
                });
            }

            return new ServiceOperationResult<IReadOnlyList<CuentaDto>>
            {
                HttpStatus = 200,
                Code    = "CUENTAS_LISTADAS",
                Message = $"{list.Count} cuenta(s) encontrada(s).",
                Data    = list
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_Cuenta_Listar");
            return new ServiceOperationResult<IReadOnlyList<CuentaDto>>
                { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = ex.Message };
        }
    }

    private async Task<CuentaDto?> CargarCuentaAsync(
        SqlConnection conn, long cuentaId, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandType    = CommandType.Text;
        cmd.CommandTimeout = 30;
        cmd.CommandText    = @"
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
        cmd.Parameters.Add(
            new SqlParameter("@CuentaId", SqlDbType.BigInt) { Value = cuentaId });

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
        new() { Connection = c, CommandType = CommandType.StoredProcedure,
                CommandText = n, CommandTimeout = 60 };

    private static void P(SqlCommand cmd, string n, object? v) =>
        cmd.Parameters.Add(new SqlParameter(n, v ?? DBNull.Value));

    private static ServiceOperationResult<CuentaDto> Err(string code, string msg) =>
        new() { HttpStatus = 500, Code = code, Message = msg };
}

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

    public async Task<ServiceOperationResult<IReadOnlyList<MetodoPagoDto>>> ListarMetodosPagoAsync(
        CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_MetodoPago_Listar");

        try
        {
            var list = new List<MetodoPagoDto>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new MetodoPagoDto
                {
                    MetodoPagoId        = reader.GetInt32OrDefault("MetodoPagoId"),
                    Nombre              = reader.GetNullableString("Nombre") ?? string.Empty,
                    RequiereReferencia  = reader.GetBooleanOrDefault("RequiereReferencia"),
                    RequiereComprobante = reader.GetBooleanOrDefault("RequiereComprobante")
                });
            }

            return new ServiceOperationResult<IReadOnlyList<MetodoPagoDto>>
            {
                HttpStatus = 200,
                Code    = "METODOS_PAGO_OK",
                Message = $"{list.Count} método(s) de pago disponible(s).",
                Data    = list
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_MetodoPago_Listar");
            return new ServiceOperationResult<IReadOnlyList<MetodoPagoDto>>
                { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = ex.Message };
        }
    }

    private static SqlCommand Sp(SqlConnection c, string n) =>
        new() { Connection = c, CommandType = CommandType.StoredProcedure,
                CommandText = n, CommandTimeout = 60 };

    private static void P(SqlCommand cmd, string n, object? v) =>
        cmd.Parameters.Add(new SqlParameter(n, v ?? DBNull.Value));

    private static ServiceOperationResult<PagoDto> Err(string code, string msg) =>
        new() { HttpStatus = 500, Code = code, Message = msg };
}