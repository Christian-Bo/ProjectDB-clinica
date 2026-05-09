using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Inventario;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// =============================================================================
// InventarioService
// SP consumido: dbo.sp_RegistrarMovimientoInventario
// Devuelve: HttpStatus | Codigo | MovimientoId | StockResultante
// TipoMovimiento válidos: ENTRADA | SALIDA | AJUSTE | DEVOLUCION | VENCIMIENTO
// =============================================================================
public sealed class InventarioService : IInventarioService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<InventarioService> _logger;

    public InventarioService(DatabaseConnection db, ILogger<InventarioService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<MovimientoInventarioDto>> RegistrarMovimientoAsync(
        RegistrarMovimientoRequestDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_RegistrarMovimientoInventario");
        P(cmd, "@MedicamentoId",   req.MedicamentoId);
        P(cmd, "@TipoMovimiento",  req.TipoMovimiento);
        P(cmd, "@Cantidad",        req.Cantidad);
        P(cmd, "@OrigenTipo",      req.OrigenTipo);
        P(cmd, "@OrigenId",        req.OrigenId);
        P(cmd, "@RecetaDetalleId", req.RecetaDetalleId);
        P(cmd, "@Costo",           req.Costo);
        P(cmd, "@PrecioUnitario",  req.PrecioUnitario);
        P(cmd, "@Referencia",      req.Referencia);
        P(cmd, "@Observaciones",   req.Observaciones);
        P(cmd, "@UsuarioId",       req.UsuarioId);
        P(cmd, "@LoteId",          req.LoteId);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return Err("SP_SIN_RESPUESTA", "El SP no devolvió resultado.");

            var status  = reader.GetInt32OrDefault("HttpStatus", 500);
            var code    = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO";
            var msg     = reader.GetNullableString("Mensaje") ?? string.Empty;

            if (status is < 200 or >= 300)
                return new ServiceOperationResult<MovimientoInventarioDto>
                    { HttpStatus = status, Code = code, Message = msg };

            var data = new MovimientoInventarioDto
            {
                MovimientoId     = reader.GetInt64OrDefault("MovimientoId"),
                MedicamentoId    = req.MedicamentoId,
                TipoMovimiento   = req.TipoMovimiento,
                Cantidad         = req.Cantidad,
                StockResultante  = GetDecimal(reader, "StockResultante")
            };

            return new ServiceOperationResult<MovimientoInventarioDto>
                { HttpStatus = status, Code = code, Message = msg, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sp_RegistrarMovimientoInventario");
            return Err("ERROR_INFRAESTRUCTURA", ex.Message);
        }
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

    private static ServiceOperationResult<MovimientoInventarioDto> Err(string code, string msg) =>
        new() { HttpStatus = 500, Code = code, Message = msg };
}
