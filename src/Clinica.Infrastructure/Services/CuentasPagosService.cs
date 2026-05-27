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
// Todos los accesos a datos pasan por Stored Procedures.
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

    public async Task<ServiceOperationResult<IReadOnlyList<CuentaDto>>> ListarAsync(
        CuentaListarFiltrosDto filtros, CancellationToken ct = default)
    {
        try
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);

            await using var cmd = Sp(conn, "dbo.sp_Cuenta_Listar");
            P(cmd, "@PacienteId", filtros.PacienteId);
            P(cmd, "@Estado", filtros.Estado);

            var items = new List<CuentaDto>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                items.Add(MapCuenta(reader));

            return new ServiceOperationResult<IReadOnlyList<CuentaDto>>
            {
                HttpStatus = 200,
                Code = "CUENTAS_LISTADAS",
                Message = items.Count == 0 ? "No se encontraron cuentas." : $"{items.Count} cuenta(s).",
                Data = items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_Cuenta_Listar");
            return Err<IReadOnlyList<CuentaDto>>("ERROR_INFRAESTRUCTURA", ex.Message);
        }
    }

    public async Task<ServiceOperationResult<CuentaDetalleDto>> ObtenerAsync(long cuentaId, CancellationToken ct = default)
    {
        try
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);

            var detalle = await CargarCuentaDetalleAsync(conn, cuentaId, ct);
            if (detalle is null)
            {
                return new ServiceOperationResult<CuentaDetalleDto>
                {
                    HttpStatus = 404,
                    Code = "CUENTA_NO_ENCONTRADA",
                    Message = "No se encontró la cuenta solicitada."
                };
            }

            return new ServiceOperationResult<CuentaDetalleDto>
            {
                HttpStatus = 200,
                Code = "CUENTA_OBTENIDA",
                Message = "OK.",
                Data = detalle
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_Cuenta_Obtener");
            return Err<CuentaDetalleDto>("ERROR_INFRAESTRUCTURA", ex.Message);
        }
    }

    public async Task<ServiceOperationResult<CuentaDto>> GenerarDesdeCitaAsync(
        GenerarCuentaRequestDto req, Guid? idempotencyKey, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_GenerarCuentaDesdeCita");
        P(cmd, "@CitaId", req.CitaId);
        P(cmd, "@CreadoPor", req.CreadoPor);

        var tvp = new DataTable();
        tvp.Columns.Add("TipoConcepto", typeof(string));
        tvp.Columns.Add("Descripcion", typeof(string));
        tvp.Columns.Add("Cantidad", typeof(decimal));
        tvp.Columns.Add("PrecioUnitario", typeof(decimal));

        foreach (var d in req.Detalles)
            tvp.Rows.Add(d.TipoConcepto, d.Descripcion, d.Cantidad, d.PrecioUnitario);

        var tvpParam = cmd.Parameters.Add(new SqlParameter("@Detalles", SqlDbType.Structured));
        tvpParam.TypeName = "dbo.TVP_DetallesCuenta";
        tvpParam.Value = tvp;

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return Err<CuentaDto>("SP_SIN_RESPUESTA", "El SP no devolvió resultado.");

            var status = reader.GetInt32OrDefault("HttpStatus", 500);
            var code = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO";
            var msg = reader.GetNullableString("Mensaje") ?? string.Empty;
            var cuentaId = reader.GetNullableInt64("CuentaId");

            if (status is < 200 or >= 300)
                return new ServiceOperationResult<CuentaDto> { HttpStatus = status, Code = code, Message = msg };

            CuentaDto? data = null;
            if (cuentaId.HasValue)
                data = (await CargarCuentaDetalleAsync(conn, cuentaId.Value, ct))?.Cuenta;

            return new ServiceOperationResult<CuentaDto>
                { HttpStatus = status, Code = code, Message = msg, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_GenerarCuentaDesdeCita");
            return Err<CuentaDto>("ERROR_INFRAESTRUCTURA", ex.Message);
        }
    }

    private async Task<CuentaDetalleDto?> CargarCuentaDetalleAsync(SqlConnection conn, long cuentaId, CancellationToken ct)
    {
        await using var cmd = Sp(conn, "dbo.sp_Cuenta_Obtener");
        P(cmd, "@CuentaId", cuentaId);

        CuentaDto? cuenta = null;
        var detalle = new List<CuentaDetalleLineaDto>();
        var pagos = new List<PagoDto>();

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
            cuenta = MapCuenta(reader);

        if (await reader.NextResultAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                detalle.Add(MapDetalle(reader));
        }

        if (await reader.NextResultAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                pagos.Add(MapPago(reader));
        }

        return cuenta is null
            ? null
            : new CuentaDetalleDto { Cuenta = cuenta, Detalle = detalle, Pagos = pagos };
    }

    private static CuentaDto MapCuenta(SqlDataReader r) => new()
    {
        CuentaId = r.GetInt64OrDefault("CuentaId"),
        CitaId = r.GetInt64OrDefault("CitaId"),
        PacienteId = r.GetInt32OrDefault("PacienteId"),
        PacienteNombre = r.GetNullableString("PacienteNombre") ?? string.Empty,
        TipoConsultaId = r.GetNullableInt32("TipoConsultaId"),
        TipoConsultaNombre = r.GetNullableString("TipoConsultaNombre"),
        SubtotalConsulta = Dec(r, "SubtotalConsulta"),
        SubtotalMedicamentos = Dec(r, "SubtotalMedicamentos"),
        SubtotalProcedimientos = Dec(r, "SubtotalProcedimientos"),
        Descuento = Dec(r, "Descuento"),
        Total = Dec(r, "Total"),
        Saldo = Dec(r, "Saldo"),
        Estado = r.GetNullableString("Estado") ?? string.Empty,
        FechaEmision = r.GetDateTimeOrDefault("FechaEmision"),
        FechaPago = r.GetNullableDateTime("FechaPago"),
        Observaciones = r.GetNullableString("Observaciones")
    };

    private static CuentaDetalleLineaDto MapDetalle(SqlDataReader r) => new()
    {
        CuentaDetalleId = r.GetInt64OrDefault("CuentaDetalleId"),
        CuentaId = r.GetInt64OrDefault("CuentaId"),
        TipoConcepto = r.GetNullableString("TipoConcepto") ?? string.Empty,
        Descripcion = r.GetNullableString("Descripcion") ?? string.Empty,
        Cantidad = Dec(r, "Cantidad"),
        PrecioUnitario = Dec(r, "PrecioUnitario"),
        Subtotal = Dec(r, "Subtotal")
    };

    private static PagoDto MapPago(SqlDataReader r) => new()
    {
        PagoId = r.GetInt64OrDefault("PagoId"),
        CuentaId = r.GetInt64OrDefault("CuentaId"),
        MetodoPagoId = r.GetInt32OrDefault("MetodoPagoId"),
        MetodoPagoNombre = r.GetNullableString("MetodoPagoNombre"),
        Monto = Dec(r, "Monto"),
        Referencia = r.GetNullableString("Referencia"),
        FechaPago = r.GetDateTimeOrDefault("FechaPago"),
        Anulado = r.GetBooleanOrDefault("Anulado")
    };

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
    private static ServiceOperationResult<T> Err<T>(string code, string msg) =>
        new() { HttpStatus = 500, Code = code, Message = msg };
}

// =============================================================================
// PagosService
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

    public async Task<ServiceOperationResult<IReadOnlyList<MetodoPagoDto>>> ListarMetodosPagoAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);

            await using var cmd = Sp(conn, "dbo.sp_MetodoPago_Listar");
            var items = new List<MetodoPagoDto>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new MetodoPagoDto
                {
                    MetodoPagoId = reader.GetInt32OrDefault("MetodoPagoId"),
                    Nombre = reader.GetNullableString("Nombre") ?? string.Empty,
                    RequiereReferencia = reader.GetBooleanOrDefault("RequiereReferencia"),
                    RequiereComprobante = reader.GetBooleanOrDefault("RequiereComprobante"),
                    Activo = reader.GetBooleanOrDefault("Activo")
                });
            }

            return new ServiceOperationResult<IReadOnlyList<MetodoPagoDto>>
            {
                HttpStatus = 200,
                Code = "METODOS_PAGO_LISTADOS",
                Message = items.Count == 0 ? "No hay métodos de pago activos." : $"{items.Count} método(s).",
                Data = items
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_MetodoPago_Listar");
            return Err<IReadOnlyList<MetodoPagoDto>>("ERROR_INFRAESTRUCTURA", ex.Message);
        }
    }

    public async Task<ServiceOperationResult<PagoDto>> RegistrarPagoAsync(
        RegistrarPagoRequestDto req, Guid? idempotencyKey, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_RegistrarPagoCuenta");
        P(cmd, "@CuentaId", req.CuentaId);
        P(cmd, "@MetodoPagoId", req.MetodoPagoId);
        P(cmd, "@Monto", req.Monto);
        P(cmd, "@Referencia", req.Referencia);
        P(cmd, "@ComprobanteUrl", req.ComprobanteUrl);
        P(cmd, "@Observaciones", req.Observaciones);
        P(cmd, "@RegistradoPor", req.RegistradoPor);
        P(cmd, "@IdempotencyKey", idempotencyKey);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return Err<PagoDto>("SP_SIN_RESPUESTA", "El SP no devolvió resultado.");

            var status = reader.GetInt32OrDefault("HttpStatus", 500);
            var code = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO";
            var msg = reader.GetNullableString("Mensaje") ?? string.Empty;
            var pagoId = reader.GetNullableInt64("PagoId");

            if (status is < 200 or >= 300)
                return new ServiceOperationResult<PagoDto> { HttpStatus = status, Code = code, Message = msg };

            var data = new PagoDto
            {
                PagoId = pagoId ?? 0,
                CuentaId = req.CuentaId,
                MetodoPagoId = req.MetodoPagoId,
                Monto = req.Monto,
                Referencia = req.Referencia,
                FechaPago = DateTime.UtcNow,
                Anulado = false
            };

            return new ServiceOperationResult<PagoDto>
                { HttpStatus = status, Code = code, Message = msg, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_RegistrarPagoCuenta");
            return Err<PagoDto>("ERROR_INFRAESTRUCTURA", ex.Message);
        }
    }

    private static SqlCommand Sp(SqlConnection c, string n) =>
        new() { Connection = c, CommandType = CommandType.StoredProcedure, CommandText = n, CommandTimeout = 60 };
    private static void P(SqlCommand cmd, string n, object? v) =>
        cmd.Parameters.Add(new SqlParameter(n, v ?? DBNull.Value));
    private static ServiceOperationResult<T> Err<T>(string code, string msg) =>
        new() { HttpStatus = 500, Code = code, Message = msg };
}
