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
        var normalizedIds = (servicioIds ?? Array.Empty<int>())
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var servicioIdsCsv = normalizedIds.Length == 0 ? null : string.Join(',', normalizedIds);
        var servicioId = normalizedIds.Length == 1 ? normalizedIds[0] : (int?)null;

        var parameters = new[]
        {
            Sql.Int("@SedeId", sedeId > 0 ? sedeId : null),
            Sql.Int("@ServicioId", servicioId),
            Sql.NVarChar("@ServicioIds", servicioIdsCsv, 4000),
            Sql.Int("@TopProximos", 8),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_ObtenerPantallaCola", parameters, ct);

        ColaTicketPreviewDto? ultimoLlamado = null;
        var ticketsLlamados = new List<ColaTicketPreviewDto>();
        var proximos = new List<ColaTicketPreviewDto>();
        var ultimosLlamados = new List<ColaTicketPreviewDto>();
        string sedeNombre = string.Empty;
        string serviciosNombre = string.Empty;

        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && ds.Tables[0].HasColumn("TicketId"))
        {
            ultimoLlamado = MapPreview(ds.Tables[0].Rows[0]);
            sedeNombre = ultimoLlamado.SedeNombre ?? sedeNombre;
            serviciosNombre = ultimoLlamado.ServicioNombre ?? serviciosNombre;
        }

        if (ds.Tables.Count > 1 && ds.Tables[1].HasColumn("TicketId"))
        {
            foreach (DataRow row in ds.Tables[1].Rows)
                ticketsLlamados.Add(MapPreview(row));
        }

        if (ds.Tables.Count > 2 && ds.Tables[2].HasColumn("TicketId"))
        {
            foreach (DataRow row in ds.Tables[2].Rows)
                proximos.Add(MapPreview(row));
        }

        if (ds.Tables.Count > 3 && ds.Tables[3].HasColumn("TicketId"))
        {
            foreach (DataRow row in ds.Tables[3].Rows)
                ultimosLlamados.Add(MapPreview(row));
        }

        var metaIndex = FindMetadataTable(ds);
        if (metaIndex >= 0 && ds.Tables[metaIndex].Rows.Count > 0)
        {
            var meta = ds.Tables[metaIndex].Rows[0];
            sedeNombre = meta.Table.HasColumn("SedeNombre") ? meta.StrNull("SedeNombre") ?? sedeNombre : sedeNombre;
            serviciosNombre = meta.Table.HasColumn("ServiciosNombre") ? meta.StrNull("ServiciosNombre") ?? serviciosNombre : serviciosNombre;
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
            Proximos = proximos,
            UltimoLlamado = ultimoLlamado,
            TicketsLlamados = ticketsLlamados,
            UltimosLlamados = ultimosLlamados,
            ConsultadoEnUtc = DateTime.UtcNow,
        };
    }

    private static int FindMetadataTable(DataSet ds)
    {
        for (var i = 0; i < ds.Tables.Count; i++)
        {
            var table = ds.Tables[i];
            if (table.HasColumn("SedeNombre") && !table.HasColumn("TicketId"))
                return i;
        }

        return -1;
    }

    private static ColaTicketPreviewDto MapPreview(DataRow row)
    {
        var table = row.Table;
        var consultorioNombre = table.HasColumn("ConsultorioNombre") ? row.StrNull("ConsultorioNombre") : null;
        var servicioNombre = table.HasColumn("ServicioNombre") ? row.StrNull("ServicioNombre") : null;
        var destinoTipo = table.HasColumn("DestinoTipo") ? row.StrNull("DestinoTipo") : null;
        var destinoActual = table.HasColumn("DestinoActual") ? row.StrNull("DestinoActual") : null;
        var ventanillaNombre = table.HasColumn("VentanillaNombre")
            ? row.StrNull("VentanillaNombre")
            : destinoActual ?? consultorioNombre ?? servicioNombre;

        return new ColaTicketPreviewDto
        {
            TicketId = table.HasColumn("TicketId") ? row.Int64("TicketId") : 0,
            NumeroTicket = table.HasColumn("NumeroTicket") ? row.Str("NumeroTicket") : string.Empty,
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
            DestinoTipo = destinoTipo,
            DestinoActual = destinoActual,
        };
    }
}
