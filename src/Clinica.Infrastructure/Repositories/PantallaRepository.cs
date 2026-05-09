using System.Data;
using Clinica.Application.DTOs.Pantalla;
using Clinica.Infrastructure.Database;

namespace Clinica.Infrastructure.Repositories;

public sealed class PantallaRepository(SqlExecutor db)
{
    public Task<PantallaColaDto> ObtenerColaAsync(int sedeId, int servicioId, CancellationToken ct) =>
        ObtenerColaAsync(sedeId, new[] { servicioId }, ct);

    public async Task<PantallaColaDto> ObtenerColaAsync(int sedeId, IReadOnlyCollection<int> servicioIds, CancellationToken ct)
    {
        var normalizedIds = servicioIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var servicioIdsCsv = string.Join(',', normalizedIds);
        var servicioId = normalizedIds.Length == 1 ? normalizedIds[0] : (int?)null;

        var parameters = new[]
        {
            Sql.Int("@SedeId",       sedeId),
            Sql.Int("@ServicioId",   servicioId),
            Sql.NVarChar("@ServicioIds", servicioIdsCsv, 4000),
            Sql.Int("@TopProximos",  5),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_ObtenerPantallaCola", parameters, ct);

        ColaTicketPreviewDto? ultimoLlamado = null;
        var ticketsLlamados = new List<ColaTicketPreviewDto>();
        var ultimosLlamados = new List<ColaTicketPreviewDto>();
        string sedeNombre = string.Empty;
        string serviciosNombre = string.Empty;

        // Table[0]: último ticket llamado / foco principal.
        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
        {
            ultimoLlamado = MapPreview(ds.Tables[0].Rows[0]);
            sedeNombre = ultimoLlamado.SedeNombre ?? sedeNombre;
            serviciosNombre = ultimoLlamado.ServicioNombre ?? serviciosNombre;
        }

        // Table[1]: tickets actualmente llamados/en atención para la pantalla pública.
        if (ds.Tables.Count > 1)
        {
            foreach (DataRow row in ds.Tables[1].Rows)
                ticketsLlamados.Add(MapPreview(row));
        }

        // Table[2]: últimos llamados históricos. En SPs antiguos, Table[2] podía ser metadata.
        if (ds.Tables.Count > 2 && ds.Tables[2].HasColumn("TicketId"))
        {
            foreach (DataRow row in ds.Tables[2].Rows)
                ultimosLlamados.Add(MapPreview(row));
        }

        // Table[3]: metadatos de sede y servicios.
        if (ds.Tables.Count > 3 && ds.Tables[3].Rows.Count > 0)
        {
            var meta = ds.Tables[3].Rows[0];
            sedeNombre = meta.StrNull("SedeNombre") ?? sedeNombre;
            serviciosNombre = meta.StrNull("ServicioNombre") ?? meta.StrNull("ServiciosNombre") ?? serviciosNombre;
        }
        // Compatibilidad con SP anterior: Table[2] podía ser metadatos.
        else if (ds.Tables.Count > 2 && ds.Tables[2].Rows.Count > 0 && ds.Tables[2].HasColumn("SedeNombre") && !ds.Tables[2].HasColumn("TicketId"))
        {
            var meta = ds.Tables[2].Rows[0];
            sedeNombre = meta.StrNull("SedeNombre") ?? sedeNombre;
            serviciosNombre = meta.StrNull("ServicioNombre") ?? serviciosNombre;
        }

        if (ticketsLlamados.Count == 0 && ultimoLlamado is not null)
            ticketsLlamados.Add(ultimoLlamado);

        return new PantallaColaDto
        {
            SedeId = sedeId,
            SedeNombre = sedeNombre,
            ServicioId = servicioId ?? normalizedIds.FirstOrDefault(),
            ServicioIds = [.. normalizedIds],
            ServicioNombre = servicioId.HasValue ? serviciosNombre : string.Empty,
            ServiciosNombre = serviciosNombre,
            Actual = ultimoLlamado,
            Proximos = ultimosLlamados,
            UltimoLlamado = ultimoLlamado,
            TicketsLlamados = ticketsLlamados,
            UltimosLlamados = ultimosLlamados,
            ConsultadoEnUtc = DateTime.UtcNow,
        };
    }

    private static ColaTicketPreviewDto MapPreview(DataRow row)
    {
        var table = row.Table;
        var consultorioNombre = table.HasColumn("ConsultorioNombre") ? row.StrNull("ConsultorioNombre") : null;
        var servicioNombre = table.HasColumn("ServicioNombre") ? row.StrNull("ServicioNombre") : null;
        var ventanillaNombre = table.HasColumn("VentanillaNombre")
            ? row.StrNull("VentanillaNombre")
            : consultorioNombre ?? servicioNombre;

        return new ColaTicketPreviewDto
        {
            TicketId = row.Int64("TicketId"),
            NumeroTicket = row.Str("NumeroTicket"),
            PacienteNombre = table.HasColumn("PacienteNombre") ? row.StrNull("PacienteNombre") ?? string.Empty : string.Empty,
            Prioridad = table.HasColumn("Prioridad") ? row.StrNull("Prioridad") ?? "NORMAL" : "NORMAL",
            Estado = table.HasColumn("Estado") ? row.StrNull("Estado") ?? string.Empty : string.Empty,
            FechaReferencia = table.HasColumn("FechaReferencia") ? row.StrNull("FechaReferencia") : null,
            SedeId = table.HasColumn("SedeId") ? row.Int32Null("SedeId") : null,
            SedeNombre = table.HasColumn("SedeNombre") ? row.StrNull("SedeNombre") : null,
            ServicioId = table.HasColumn("ServicioId") ? row.Int32Null("ServicioId") : null,
            ServicioNombre = servicioNombre,
            ConsultorioId = table.HasColumn("ConsultorioId") ? row.Int32Null("ConsultorioId") : null,
            ConsultorioNombre = consultorioNombre,
            VentanillaNombre = ventanillaNombre,
        };
    }
}
