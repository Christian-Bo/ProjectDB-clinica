using System.Data;
using Clinica.Application.DTOs.Pantalla;
using Clinica.Infrastructure.Database;

namespace Clinica.Infrastructure.Repositories;

public sealed class PantallaRepository(SqlExecutor db)
{
    public async Task<PantallaColaDto> ObtenerColaAsync(int sedeId, int servicioId, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.Int("@SedeId",     sedeId),
            Sql.Int("@ServicioId", servicioId),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_ObtenerPantallaCola", parameters, ct);

        ColaTicketPreviewDto? actual = null;
        var proximos = new List<ColaTicketPreviewDto>();
        string sedeNombre    = string.Empty;
        string servicioNombre = string.Empty;

        // Table[0]: ticket actual (0 o 1 fila)
        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
        {
            var row = ds.Tables[0].Rows[0];

            // El SP puede devolver todo en una sola tabla con columna "EsActual"
            // o en dos tablas separadas. Manejamos ambos casos.
            if (ds.Tables[0].HasColumn("EsActual"))
            {
                foreach (DataRow r in ds.Tables[0].Rows)
                {
                    var preview = MapPreview(r);
                    if (r.Bool("EsActual"))
                        actual = preview;
                    else
                        proximos.Add(preview);
                }
            }
            else
            {
                actual = MapPreview(row);
            }

            sedeNombre     = row.StrNull("SedeNombre")     ?? string.Empty;
            servicioNombre = row.StrNull("ServicioNombre") ?? string.Empty;
        }

        // Table[1]: próximos tickets
        if (ds.Tables.Count > 1)
        {
            foreach (DataRow r in ds.Tables[1].Rows)
                proximos.Add(MapPreview(r));
        }

        // Table[2] (opcional): metadatos de sede/servicio
        if (ds.Tables.Count > 2 && ds.Tables[2].Rows.Count > 0)
        {
            var meta = ds.Tables[2].Rows[0];
            sedeNombre     = meta.StrNull("SedeNombre")     ?? sedeNombre;
            servicioNombre = meta.StrNull("ServicioNombre") ?? servicioNombre;
        }

        return new PantallaColaDto
        {
            SedeId          = sedeId,
            SedeNombre      = sedeNombre,
            ServicioId      = servicioId,
            ServicioNombre  = servicioNombre,
            Actual          = actual,
            Proximos        = proximos,
            ConsultadoEnUtc = DateTime.UtcNow,
        };
    }

    private static ColaTicketPreviewDto MapPreview(DataRow row) => new()
    {
        TicketId          = row.Int64("TicketId"),
        NumeroTicket      = row.Str("NumeroTicket"),
        Prioridad         = row.StrNull("Prioridad")        ?? "NORMAL",
        Estado            = row.StrNull("Estado")           ?? string.Empty,
        FechaReferencia   = row.StrNull("FechaReferencia"),
        ConsultorioId     = row.Int32Null("ConsultorioId"),
        ConsultorioNombre = row.StrNull("ConsultorioNombre"),
    };
}
