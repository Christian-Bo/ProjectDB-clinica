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
// ProveedoresService
// SPs: sp_Proveedor_Upsert | sp_Proveedor_Obtener | sp_Proveedor_Listar
// Estado válidos: ACTIVO | INACTIVO | SUSPENDIDO
// Devuelven: HttpStatus | Codigo | IdGenerado / IdActualizado
// =============================================================================
public sealed class ProveedoresService : IProveedoresService
{
    private readonly DatabaseConnection _db;
    private readonly ILogger<ProveedoresService> _logger;

    public ProveedoresService(DatabaseConnection db, ILogger<ProveedoresService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<ProveedorDto>> UpsertProveedorAsync(
        ProveedorUpsertDto req, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_Proveedor_Upsert");
        P(cmd, "@ProveedorId",       req.ProveedorId);
        P(cmd, "@Nombre",            req.Nombre);
        P(cmd, "@NIT",               req.NIT);
        P(cmd, "@Contacto",          req.Contacto);
        P(cmd, "@Telefono",          req.Telefono);
        P(cmd, "@CorreoElectronico", req.CorreoElectronico);
        P(cmd, "@Direccion",         req.Direccion);
        P(cmd, "@Estado",            req.Estado);

        var env = await ExecAsync(cmd, ct);
        if (!env.IsOk) return Fail<ProveedorDto>(env);

        var id = env.IntId ?? req.ProveedorId;
        if (!id.HasValue) return Ok<ProveedorDto>(env, null);

        var dto = await CargarAsync(conn, id.Value, ct);
        return Ok(env, dto);
    }

    public async Task<ServiceOperationResult<ProveedorDto>> ObtenerProveedorAsync(
        int proveedorId, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        var dto = await CargarAsync(conn, proveedorId, ct);
        if (dto is null)
            return new ServiceOperationResult<ProveedorDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "PROVEEDOR_NO_ENCONTRADO",
                Message = "No se encontró el proveedor indicado."
            };

        return new ServiceOperationResult<ProveedorDto>
            { HttpStatus = 200, Code = "PROVEEDOR_OBTENIDO", Message = "OK.", Data = dto };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<ProveedorDto>>> ListarProveedoresAsync(
        ProveedorListarFiltrosDto filtros, CancellationToken ct = default)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        await using var cmd = Sp(conn, "dbo.sp_Proveedor_Listar");
        P(cmd, "@Estado", filtros.Estado);
        P(cmd, "@Texto",  filtros.Texto);

        var list = new List<ProveedorDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));

        return new ServiceOperationResult<IReadOnlyList<ProveedorDto>>
        {
            HttpStatus = 200,
            Code = "PROVEEDORES_LISTADOS",
            Message = list.Count == 0 ? "No se encontraron proveedores." : $"{list.Count} proveedor(es).",
            Data = list
        };
    }

    private async Task<ProveedorDto?> CargarAsync(SqlConnection conn, int id, CancellationToken ct)
    {
        await using var cmd = Sp(conn, "dbo.sp_Proveedor_Obtener");
        P(cmd, "@ProveedorId", id);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? Map(r) : null;
    }

    private static ProveedorDto Map(SqlDataReader r) => new()
    {
        ProveedorId       = r.GetInt32OrDefault("ProveedorId"),
        Nombre            = r.GetNullableString("Nombre") ?? string.Empty,
        NIT               = r.GetNullableString("NIT"),
        Contacto          = r.GetNullableString("Contacto"),
        Telefono          = r.GetNullableString("Telefono"),
        CorreoElectronico = r.GetNullableString("CorreoElectronico"),
        Direccion         = r.GetNullableString("Direccion"),
        Estado            = r.GetNullableString("Estado") ?? string.Empty,
        FechaRegistro     = r.GetDateTimeOrDefault("FechaRegistro")
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
        catch (Exception ex) { _logger.LogError(ex, "{SP}", cmd.CommandText); return SpEnv.Error(ex.Message); }
    }

    private static SqlCommand Sp(SqlConnection c, string n) =>
        new() { Connection = c, CommandType = CommandType.StoredProcedure, CommandText = n, CommandTimeout = 60 };
    private static void P(SqlCommand cmd, string n, object? v) =>
        cmd.Parameters.Add(new SqlParameter(n, v ?? DBNull.Value));
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
        public static SpEnv Sin() => new() { HttpStatus = 500, Code = "SP_SIN_RESPUESTA", Message = "Sin resultado." };
        public static SpEnv Error(string m) => new() { HttpStatus = 500, Code = "ERROR_INFRAESTRUCTURA", Message = m };
    }
}
