using System.Data;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Farmacia;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// =============================================================================
// FarmaciaService
// SPs consumidos (parámetros verificados contra 29_SPs_Complementarios):
//   dbo.sp_Medicamento_Upsert  → devuelve IdGenerado o IdActualizado
//   dbo.sp_Medicamento_Obtener → devuelve columnas controladas de medicamentos
//   dbo.sp_Medicamento_Listar  → devuelve listado filtrado de medicamentos
//   dbo.sp_DespacharReceta     → devuelve HttpStatus/Codigo/Mensaje
// =============================================================================
public sealed class FarmaciaService : IFarmaciaService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<FarmaciaService> _logger;

    public FarmaciaService(DatabaseConnection db, ILogger<FarmaciaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<MedicamentoDto>> UpsertMedicamentoAsync(
        MedicamentoUpsertDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_Medicamento_Upsert");
        P(cmd, "@MedicamentoId",             req.MedicamentoId);
        P(cmd, "@CodigoInterno",             req.CodigoInterno);
        P(cmd, "@CodigoBarras",              req.CodigoBarras);
        P(cmd, "@Nombre",                    req.Nombre);
        P(cmd, "@NombreGenerico",            req.NombreGenerico);
        P(cmd, "@PrincipioActivo",           req.PrincipioActivo);
        P(cmd, "@Tipo",                      req.Tipo);
        P(cmd, "@Presentacion",              req.Presentacion);
        P(cmd, "@ConcentracionDescripcion",  req.ConcentracionDescripcion);
        P(cmd, "@UnidadMedida",              req.UnidadMedida);
        P(cmd, "@RequiereReceta",            req.RequiereReceta);
        P(cmd, "@ControladoPorSalud",        req.ControladoPorSalud);
        P(cmd, "@PrecioCompra",              req.PrecioCompra);
        P(cmd, "@PrecioVenta",               req.PrecioVenta);
        P(cmd, "@StockMinimo",               req.StockMinimo);
        P(cmd, "@Estado",                    req.Estado);

        var env = await ExecAsync(cmd, ct);
        if (!env.IsOk) return Fail<MedicamentoDto>(env);

        // El SP devuelve IdGenerado (INSERT) o IdActualizado (UPDATE)
        var id = env.IntId ?? req.MedicamentoId;
        if (!id.HasValue) return Ok<MedicamentoDto>(env, null);

        var dto = await CargarMedicamentoAsync(conn, id.Value, null, ct);
        return Ok(env, dto);
    }

    public async Task<ServiceOperationResult<MedicamentoDto>> ObtenerMedicamentoAsync(
        int medicamentoId, string? codigoInterno, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        var dto = await CargarMedicamentoAsync(conn, medicamentoId, codigoInterno, ct);
        if (dto is null)
            return new ServiceOperationResult<MedicamentoDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "MEDICAMENTO_NO_ENCONTRADO",
                Message = "No se encontró el medicamento indicado."
            };

        return new ServiceOperationResult<MedicamentoDto>
            { HttpStatus = 200, Code = "MEDICAMENTO_OBTENIDO", Message = "OK.", Data = dto };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<MedicamentoDto>>> ListarMedicamentosAsync(
        MedicamentoListarFiltrosDto filtros, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_Medicamento_Listar");
        P(cmd, "@Estado", filtros.Estado);
        P(cmd, "@Texto",  filtros.Texto);

        var list = new List<MedicamentoDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(MapMedicamento(reader));

        return new ServiceOperationResult<IReadOnlyList<MedicamentoDto>>
        {
            HttpStatus = 200,
            Code = "MEDICAMENTOS_LISTADOS",
            Message = list.Count == 0 ? "No se encontraron medicamentos." : $"{list.Count} medicamento(s) encontrado(s).",
            Data = list
        };
    }

    public async Task<ServiceOperationResult<object>> DespacharRecetaAsync(
        long recetaId, int usuarioId, string? observaciones, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_DespacharReceta");
        P(cmd, "@RecetaId",     recetaId);
        P(cmd, "@UsuarioId",    usuarioId);
        P(cmd, "@Observaciones", observaciones);

        var env = await ExecAsync(cmd, ct);
        return new ServiceOperationResult<object>
            { HttpStatus = env.HttpStatus, Code = env.Code, Message = env.Message };
    }

    // =========================================================================
    private async Task<MedicamentoDto?> CargarMedicamentoAsync(
        SqlConnection conn, int? medicamentoId, string? codigoInterno, CancellationToken ct)
    {
        await using var cmd = Sp(conn, "dbo.sp_Medicamento_Obtener");
        P(cmd, "@MedicamentoId", medicamentoId);
        P(cmd, "@CodigoInterno", codigoInterno);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapMedicamento(reader) : null;
    }

    private static MedicamentoDto MapMedicamento(SqlDataReader r) => new()
    {
        MedicamentoId            = r.GetInt32OrDefault("MedicamentoId"),
        CodigoInterno            = r.GetNullableString("CodigoInterno") ?? string.Empty,
        CodigoBarras             = r.GetNullableString("CodigoBarras"),
        Nombre                   = r.GetNullableString("Nombre") ?? string.Empty,
        NombreGenerico           = r.GetNullableString("NombreGenerico"),
        PrincipioActivo          = r.GetNullableString("PrincipioActivo") ?? string.Empty,
        Tipo                     = r.GetNullableString("Tipo") ?? string.Empty,
        Presentacion             = r.GetNullableString("Presentacion"),
        ConcentracionDescripcion = r.GetNullableString("ConcentracionDescripcion"),
        UnidadMedida             = r.GetNullableString("UnidadMedida") ?? string.Empty,
        RequiereReceta           = r.GetBooleanOrDefault("RequiereReceta"),
        ControladoPorSalud       = r.GetBooleanOrDefault("ControladoPorSalud"),
        PrecioCompra             = GetNullableDecimal(r, "PrecioCompra"),
        PrecioVenta              = GetDecimal(r, "PrecioVenta"),
        StockMinimo              = r.GetInt32OrDefault("StockMinimo"),
        Estado                   = r.GetNullableString("Estado") ?? string.Empty,
        FechaCreacion            = r.GetDateTimeOrDefault("FechaCreacion")
    };

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
                IntId      = r.GetNullableInt32("IdGenerado") ?? r.GetNullableInt32("IdActualizado")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en {SP}", cmd.CommandText);
            return SpEnv.Error(ex.Message);
        }
    }

    private static SqlCommand Sp(SqlConnection c, string name) =>
        new() { Connection = c, CommandType = CommandType.StoredProcedure, CommandText = name, CommandTimeout = 60 };

    private static void P(SqlCommand cmd, string n, object? v) =>
        cmd.Parameters.Add(new SqlParameter(n, v ?? DBNull.Value));

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

    private static decimal? GetNullableDecimal(SqlDataReader r, string col)
    {
        for (var i = 0; i < r.FieldCount; i++)
            if (string.Equals(r.GetName(i), col, StringComparison.OrdinalIgnoreCase))
            {
                var ord = r.GetOrdinal(col);
                return r.IsDBNull(ord) ? null : Convert.ToDecimal(r.GetValue(ord));
            }
        return null;
    }

    private static ServiceOperationResult<T> Fail<T>(SpEnv e) =>
        new() { HttpStatus = e.HttpStatus, Code = e.Code, Message = e.Message };

    private static ServiceOperationResult<T> Ok<T>(SpEnv e, T? d) =>
        new() { HttpStatus = e.HttpStatus, Code = e.Code, Message = e.Message, Data = d };

    private sealed class SpEnv
    {
        public int HttpStatus { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public int? IntId { get; init; }
        public bool IsOk => HttpStatus is >= 200 and < 300;
        public static SpEnv Sin() => new() { HttpStatus = 500, Code = "SP_SIN_RESPUESTA", Message = "El SP no devolvió resultado." };
        public static SpEnv Error(string m) => new() { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = m };
    }
}
