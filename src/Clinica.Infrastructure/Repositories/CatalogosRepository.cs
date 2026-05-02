using System.Data;
using Clinica.Application.DTOs.Catalogos;
using Clinica.Infrastructure.Database;

namespace Clinica.Infrastructure.Repositories;

public sealed class CatalogosRepository(SqlExecutor db)
{
    public async Task<List<CatalogoItemDto>> ListarSedesAsync(CancellationToken ct)
    {
        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_Sede_Listar", null, ct);
        return [.. dt.Rows.Cast<DataRow>().Select(MapCatalogo)];
    }

    public async Task<List<CatalogoItemDto>> ListarServiciosAsync(int? sedeId, CancellationToken ct)
    {
        var parameters = sedeId.HasValue
            ? new[] { Sql.Int("@SedeId", sedeId) }
            : null;
        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_Servicio_Listar", parameters, ct);
        return [.. dt.Rows.Cast<DataRow>().Select(MapCatalogo)];
    }

    public async Task<List<CatalogoItemDto>> ListarEstacionesAsync(int? sedeId, CancellationToken ct)
    {
        var parameters = sedeId.HasValue
            ? new[] { Sql.Int("@SedeId", sedeId) }
            : null;
        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_EstacionAtencion_Listar", parameters, ct);
        return [.. dt.Rows.Cast<DataRow>().Select(MapCatalogo)];
    }

    public async Task<List<PacienteItemDto>> ListarPacientesAsync(string? texto, int limit, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.NVarChar("@Texto", texto, 200),
            Sql.Int     ("@Limit", limit),
        };

        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_Paciente_Listar", parameters, ct);
        return [.. dt.Rows.Cast<DataRow>().Select(MapPaciente)];
    }

    public async Task<List<CitaItemDto>> ListarCitasConfirmadasAsync(
        int? sedeId, int? servicioId, string? texto, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.Int     ("@SedeId",     sedeId),
            Sql.Int     ("@ServicioId", servicioId),
            Sql.NVarChar("@Texto",      texto, 200),
            Sql.NVarChar("@Estado",     "CONFIRMADA", 30),
        };

        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_Cita_Listar", parameters, ct);
        return [.. dt.Rows.Cast<DataRow>().Select(MapCita)];
    }

    public async Task<List<CatalogoItemDto>> ListarPrioridadesAsync(CancellationToken ct)
    {
        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_PrioridadTicket_Listar", null, ct);
        return [.. dt.Rows.Cast<DataRow>().Select(r => new CatalogoItemDto
        {
            Id          = r.Int32("PrioridadId"),
            Nombre      = r.Str("Nombre"),
            Label       = r.Str("Label"),
            Descripcion = r.StrNull("Descripcion"),
            Activo      = true,
        })];
    }

    public async Task<List<CatalogoItemDto>> ListarEstadosAsync(CancellationToken ct)
    {
        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_EstadoTicket_Listar", null, ct);
        return [.. dt.Rows.Cast<DataRow>().Select(r => new CatalogoItemDto
        {
            Id          = r.Int32("EstadoId"),
            Nombre      = r.Str("Nombre"),
            Label       = r.Str("Label"),
            Descripcion = r.StrNull("Descripcion"),
            Activo      = true,
        })];
    }

    // ─── Mappers ─────────────────────────────────────────────────────────────

    private static CatalogoItemDto MapCatalogo(DataRow row)
    {
        // Los SPs de catálogos pueden devolver Id, SedeId, ServicioId, etc.
        var id = row.Table.HasColumn("SedeId")      ? row.Int32("SedeId")
               : row.Table.HasColumn("ServicioId")  ? row.Int32("ServicioId")
               : row.Table.HasColumn("EstacionId")  ? row.Int32("EstacionId")
               : row.Table.HasColumn("Id")          ? row.Int32("Id")
               : 0;

        var nombre = row.Table.HasColumn("Nombre") ? row.Str("Nombre") : string.Empty;
        var label  = row.Table.HasColumn("Label")  ? row.Str("Label")  : nombre;
        var codigo = row.Table.HasColumn("Codigo") ? row.StrNull("Codigo") : null;
        var desc   = row.Table.HasColumn("Descripcion") ? row.StrNull("Descripcion") : null;
        var activo = !row.Table.HasColumn("Activo") || row.Bool("Activo");

        return new CatalogoItemDto
        {
            Id = id, Nombre = nombre, Label = label,
            Codigo = codigo, Descripcion = desc, Activo = activo,
        };
    }

    private static PacienteItemDto MapPaciente(DataRow row) => new()
    {
        PacienteId        = row.Int64("PacienteId"),
        Label             = row.Str("Label"),
        NumeroExpediente  = row.StrNull("NumeroExpediente"),
        Documento         = row.StrNull("Documento"),
        Telefono          = row.StrNull("Telefono"),
        CorreoElectronico = row.StrNull("CorreoElectronico"),
        EsDiscapacitado   = row.Bool("EsDiscapacitado"),
    };

    private static CitaItemDto MapCita(DataRow row) => new()
    {
        CitaId         = row.Int64("CitaId"),
        PacienteId     = row.Int64("PacienteId"),
        Label          = row.Str("Label"),
        FechaInicio    = row.Str("FechaInicio"),
        Estado         = row.Str("Estado"),
        PacienteNombre = row.Str("PacienteNombre"),
        ServicioNombre = row.Str("ServicioNombre"),
        SedeNombre     = row.Str("SedeNombre"),
        MedicoNombre   = row.StrNull("MedicoNombre"),
    };
}
