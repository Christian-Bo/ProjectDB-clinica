using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Compras;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// =============================================================================
// OrdenesCompraService
// SPs consumidos (params verificados):
//   sp_OrdenCompra_Crear             → @ProveedorId, @NumeroOrden, @FechaEmision,
//                                       @FechaEntregaPact, @Observaciones, @CreadoPor
//                                       devuelve: OrdenCompraId
//   sp_OrdenCompra_AgregarDetalle    → @OrdenCompraId, @MedicamentoId,
//                                       @CantidadSolicitada, @PrecioUnitario,
//                                       @FechaVencimientoLote, @LoteProveedor
//                                       devuelve: OrdenCompraDetalleId
//   sp_OrdenCompra_ActualizarEstado  → @OrdenCompraId, @Estado, @AprobadoPor
//   sp_OrdenCompra_RegistrarRecepcion→ @OrdenCompraDetalleId, @CantidadRecibida,
//                                       @FechaVencimientoLote, @CodigoLote, @UsuarioId
//                                       (llama internamente a sp_RegistrarMovimientoInventario)
//   sp_OrdenCompra_Obtener           → 2 result sets: encabezado + detalles
//   sp_OrdenCompra_Listar            → @ProveedorId, @Estado, @FechaDesde, @FechaHasta
// =============================================================================
public sealed class OrdenesCompraService : IOrdenesCompraService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<OrdenesCompraService> _logger;

    public OrdenesCompraService(DatabaseConnection db, ILogger<OrdenesCompraService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<OrdenCompraDto>> CrearOrdenAsync(
        OrdenCompraCrearDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_OrdenCompra_Crear");
        P(cmd, "@ProveedorId",      req.ProveedorId);
        P(cmd, "@NumeroOrden",      req.NumeroOrden);
        P(cmd, "@FechaEmision",     req.FechaEmision);
        P(cmd, "@FechaEntregaPact", req.FechaEntregaPact);
        P(cmd, "@Observaciones",    req.Observaciones);
        P(cmd, "@CreadoPor",        req.CreadoPor);

        var env = await ExecAsync(cmd, ct);
        if (!env.IsOk) return Fail<OrdenCompraDto>(env);
        if (!env.LongId.HasValue) return Fail<OrdenCompraDto>(env);

        var dto = await CargarOrdenAsync(conn, (int)env.LongId.Value, ct);
        return new ServiceOperationResult<OrdenCompraDto>
            { HttpStatus = env.HttpStatus, Code = env.Code, Message = env.Message, Data = dto };
    }

    public async Task<ServiceOperationResult<OrdenCompraDto>> AgregarDetalleAsync(
        int ordenCompraId, OrdenCompraAgregarDetalleDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_OrdenCompra_AgregarDetalle");
        P(cmd, "@OrdenCompraId",       ordenCompraId);
        P(cmd, "@MedicamentoId",       req.MedicamentoId);
        P(cmd, "@CantidadSolicitada",  req.CantidadSolicitada);
        P(cmd, "@PrecioUnitario",      req.PrecioUnitario);
        P(cmd, "@FechaVencimientoLote",req.FechaVencimientoLote);
        P(cmd, "@LoteProveedor",       req.LoteProveedor);

        var env = await ExecAsync(cmd, ct);
        if (!env.IsOk) return Fail<OrdenCompraDto>(env);

        var dto = await CargarOrdenAsync(conn, ordenCompraId, ct);
        return new ServiceOperationResult<OrdenCompraDto>
            { HttpStatus = env.HttpStatus, Code = env.Code, Message = env.Message, Data = dto };
    }

    public async Task<ServiceOperationResult<object>> ActualizarEstadoAsync(
        int ordenCompraId, OrdenCompraActualizarEstadoDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_OrdenCompra_ActualizarEstado");
        P(cmd, "@OrdenCompraId", ordenCompraId);
        P(cmd, "@Estado",        req.Estado);
        P(cmd, "@AprobadoPor",   req.AprobadoPor);

        var env = await ExecAsync(cmd, ct);
        return new ServiceOperationResult<object>
            { HttpStatus = env.HttpStatus, Code = env.Code, Message = env.Message };
    }

    public async Task<ServiceOperationResult<object>> RegistrarRecepcionAsync(
        OrdenCompraRegistrarRecepcionDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_OrdenCompra_RegistrarRecepcion");
        P(cmd, "@OrdenCompraDetalleId",  req.OrdenCompraDetalleId);
        P(cmd, "@CantidadRecibida",      req.CantidadRecibida);
        P(cmd, "@FechaVencimientoLote",  req.FechaVencimientoLote);
        P(cmd, "@CodigoLote",            req.CodigoLote);
        P(cmd, "@UsuarioId",             req.UsuarioId);

        var env = await ExecAsync(cmd, ct);
        return new ServiceOperationResult<object>
            { HttpStatus = env.HttpStatus, Code = env.Code, Message = env.Message };
    }

    public async Task<ServiceOperationResult<OrdenCompraDto>> ObtenerOrdenAsync(
        int ordenCompraId, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        var dto = await CargarOrdenAsync(conn, ordenCompraId, ct);
        if (dto is null)
            return new ServiceOperationResult<OrdenCompraDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "ORDEN_NO_ENCONTRADA",
                Message = "No se encontró la orden de compra."
            };

        return new ServiceOperationResult<OrdenCompraDto>
            { HttpStatus = 200, Code = "ORDEN_OBTENIDA", Message = "OK.", Data = dto };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<OrdenCompraDto>>> ListarOrdenesAsync(
        OrdenCompraListarFiltrosDto filtros, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_OrdenCompra_Listar");
        P(cmd, "@ProveedorId", filtros.ProveedorId);
        P(cmd, "@Estado",      filtros.Estado);
        P(cmd, "@FechaDesde",  filtros.FechaDesde);
        P(cmd, "@FechaHasta",  filtros.FechaHasta);

        var list = new List<OrdenCompraDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new OrdenCompraDto
            {
                OrdenCompraId    = reader.GetInt32OrDefault("OrdenCompraId"),
                ProveedorId      = reader.GetInt32OrDefault("ProveedorId"),
                NumeroOrden      = reader.GetNullableString("NumeroOrden") ?? string.Empty,
                Estado           = reader.GetNullableString("Estado") ?? string.Empty,
                FechaEmision     = reader.GetDateTimeOrDefault("FechaEmision"),
                FechaEntregaPact = reader.GetNullableDateTime("FechaEntregaPact"),
                FechaRecepcion   = reader.GetNullableDateTime("FechaRecepcion"),
                Subtotal         = GetDecimal(reader, "Subtotal"),
                Impuesto         = GetDecimal(reader, "Impuesto"),
                Total            = GetDecimal(reader, "Total"),
                Observaciones    = reader.GetNullableString("Observaciones"),
                FechaCreacion    = reader.GetDateTimeOrDefault("FechaCreacion")
            });
        }

        return new ServiceOperationResult<IReadOnlyList<OrdenCompraDto>>
        {
            HttpStatus = 200,
            Code = "ORDENES_LISTADAS",
            Message = list.Count == 0 ? "No se encontraron órdenes." : $"{list.Count} orden(es).",
            Data = list
        };
    }

    // =========================================================================
    // sp_OrdenCompra_Obtener devuelve 2 result sets:
    //   1ro: SELECT * FROM dbo.OrdenesCompra WHERE OrdenCompraId = @OrdenCompraId
    //   2do: SELECT * FROM dbo.OrdenesCompraDetalle WHERE OrdenCompraId = @OrdenCompraId
    // =========================================================================
    private async Task<OrdenCompraDto?> CargarOrdenAsync(SqlConnection conn, int id, CancellationToken ct)
    {
        await using var cmd = Sp(conn, "dbo.sp_OrdenCompra_Obtener");
        P(cmd, "@OrdenCompraId", id);

        OrdenCompraDto? header = null;
        var detalles = new List<OrdenCompraDetalleDto>();

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            header = new OrdenCompraDto
            {
                OrdenCompraId    = reader.GetInt32OrDefault("OrdenCompraId"),
                ProveedorId      = reader.GetInt32OrDefault("ProveedorId"),
                NumeroOrden      = reader.GetNullableString("NumeroOrden") ?? string.Empty,
                Estado           = reader.GetNullableString("Estado") ?? string.Empty,
                FechaEmision     = reader.GetDateTimeOrDefault("FechaEmision"),
                FechaEntregaPact = reader.GetNullableDateTime("FechaEntregaPact"),
                FechaRecepcion   = reader.GetNullableDateTime("FechaRecepcion"),
                Subtotal         = GetDecimal(reader, "Subtotal"),
                Impuesto         = GetDecimal(reader, "Impuesto"),
                Total            = GetDecimal(reader, "Total"),
                Observaciones    = reader.GetNullableString("Observaciones"),
                FechaCreacion    = reader.GetDateTimeOrDefault("FechaCreacion")
            };
        }

        if (await reader.NextResultAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                detalles.Add(new OrdenCompraDetalleDto
                {
                    OrdenCompraDetalleId = reader.GetInt64OrDefault("OrdenCompraDetalleId"),
                    OrdenCompraId        = reader.GetInt32OrDefault("OrdenCompraId"),
                    MedicamentoId        = reader.GetInt32OrDefault("MedicamentoId"),
                    CantidadSolicitada   = GetDecimal(reader, "CantidadSolicitada"),
                    CantidadRecibida     = GetDecimal(reader, "CantidadRecibida"),
                    PrecioUnitario       = GetDecimal(reader, "PrecioUnitario"),
                    SubtotalLinea        = GetDecimal(reader, "SubtotalLinea"),
                    FechaVencimientoLote = reader.GetNullableDateTime("FechaVencimientoLote"),
                    LoteProveedor        = reader.GetNullableString("LoteProveedor")
                });
            }
        }

        if (header is null) return null;

        return new OrdenCompraDto
        {
            OrdenCompraId    = header.OrdenCompraId,
            ProveedorId      = header.ProveedorId,
            ProveedorNombre  = header.ProveedorNombre,
            NumeroOrden      = header.NumeroOrden,
            Estado           = header.Estado,
            FechaEmision     = header.FechaEmision,
            FechaEntregaPact = header.FechaEntregaPact,
            FechaRecepcion   = header.FechaRecepcion,
            Subtotal         = header.Subtotal,
            Impuesto         = header.Impuesto,
            Total            = header.Total,
            Observaciones    = header.Observaciones,
            FechaCreacion    = header.FechaCreacion,
            Detalles         = detalles
        };
    }

    private async Task<SpEnv> ExecAsync(SqlCommand cmd, CancellationToken ct)
    {
        try
        {
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return SpEnv.Sin();
            return new SpEnv
            {
                HttpStatus = r.GetInt32OrDefault("HttpStatus", 500),
                Code       = r.GetNullableString("Codigo") ?? "SP_SIN_CODIGO",
                Message    = r.GetNullableString("Mensaje") ?? string.Empty,
                LongId     = r.GetNullableInt64("OrdenCompraId") ?? r.GetNullableInt64("OrdenCompraDetalleId")
            };
        }
        catch (Exception ex) { _logger.LogError(ex, "{SP}", cmd.CommandText); return SpEnv.Error(ex.Message); }
    }

    private static decimal GetDecimal(SqlDataReader r, string col)
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
    private static ServiceOperationResult<T> Fail<T>(SpEnv e) =>
        new() { HttpStatus = e.HttpStatus, Code = e.Code, Message = e.Message };

    private sealed class SpEnv
    {
        public int HttpStatus { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public long? LongId { get; init; }
        public bool IsOk => HttpStatus is >= 200 and < 300;
        public static SpEnv Sin() => new() { HttpStatus = 500, Code = "SP_SIN_RESPUESTA", Message = "Sin resultado." };
        public static SpEnv Error(string m) => new() { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = m };
    }
}
