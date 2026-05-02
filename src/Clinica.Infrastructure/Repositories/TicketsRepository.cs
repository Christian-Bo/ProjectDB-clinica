using System.Data;
using Clinica.Application.DTOs.Tickets;
using Clinica.Application.Exceptions;
using Clinica.Infrastructure.Database;

namespace Clinica.Infrastructure.Repositories;

/// <summary>
/// Repositorio del Módulo 3 — Tickets.
/// Toda la lógica de negocio crítica reside en los Stored Procedures.
/// Este repositorio sólo ejecuta SPs y mapea resultados a DTOs.
/// </summary>
public sealed class TicketsRepository(SqlExecutor db)
{
    // ─── Generar ticket ─────────────────────────────────────────────────────

    public async Task<TicketDto> GenerarTicketAsync(
        long? citaId, long? pacienteId, int? sedeId, int? servicioId,
        int? medicoId, string prioridad, string? motivo, int? usuarioId,
        Guid? idempotencyKey, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.BigInt("@CitaId",              citaId),
            Sql.BigInt("@PacienteId",          pacienteId),
            Sql.Int   ("@SedeId",              sedeId),
            Sql.Int   ("@ServicioId",          servicioId),
            Sql.Int   ("@MedicoId",            medicoId),
            Sql.NVarChar("@PrioridadSolicitada", prioridad, 30),
            Sql.NVarChar("@MotivoEspecial",    motivo, 500),
            Sql.Int   ("@UsuarioId",           usuarioId),
            Sql.UniqueId("@IdempotencyKey",    idempotencyKey),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_GenerarTicket", parameters, ct);
        return MapTicketFromDataSet(ds, "dbo.sp_GenerarTicket");
    }

    // ─── Generar ticket especial ─────────────────────────────────────────────

    public async Task<TicketDto> GenerarTicketEspecialAsync(
        long? citaId, long? pacienteId, int? sedeId, int? servicioId,
        int? medicoId, string motivo, int? usuarioId,
        Guid? idempotencyKey, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.BigInt  ("@PacienteId",    pacienteId),
            Sql.Int     ("@SedeId",        sedeId),
            Sql.Int     ("@ServicioId",    servicioId),
            Sql.Int     ("@UsuarioId",     usuarioId),
            Sql.NVarChar("@MotivoEspecial", motivo, 500),
            Sql.BigInt  ("@CitaId",        citaId),
            Sql.Int     ("@MedicoId",      medicoId),
            Sql.UniqueId("@IdempotencyKey", idempotencyKey),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_GenerarTicketEspecial", parameters, ct);
        return MapTicketFromDataSet(ds, "dbo.sp_GenerarTicketEspecial");
    }

    // ─── Llamar siguiente ────────────────────────────────────────────────────

    public async Task<TicketDto> LlamarSiguienteAsync(
        int sedeId, int servicioId, int? estacionId, int? usuarioId, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.Int("@SedeId",     sedeId),
            Sql.Int("@ServicioId", servicioId),
            Sql.Int("@EstacionId", estacionId),
            Sql.Int("@UsuarioId",  usuarioId),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_LlamarSiguienteTicket", parameters, ct);
        return MapTicketFromDataSet(ds, "dbo.sp_LlamarSiguienteTicket");
    }

    // ─── Marcar en atención ──────────────────────────────────────────────────

    public async Task<TicketDto> MarcarEnAtencionAsync(long ticketId, CancellationToken ct)
    {
        var parameters = new[] { Sql.BigInt("@TicketId", ticketId) };
        var ds = await db.ExecuteSpAsync("dbo.sp_MarcarTicketEnAtencion", parameters, ct);
        return MapTicketFromDataSet(ds, "dbo.sp_MarcarTicketEnAtencion");
    }

    // ─── Finalizar ticket ────────────────────────────────────────────────────

    public async Task<TicketDto> FinalizarTicketAsync(long ticketId, string? motivo, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.BigInt  ("@TicketId", ticketId),
            Sql.NVarChar("@Motivo",   motivo, 500),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_FinalizarTicket", parameters, ct);
        return MapTicketFromDataSet(ds, "dbo.sp_FinalizarTicket");
    }

    // ─── Procesar no-show ────────────────────────────────────────────────────

    public async Task<NoShowResultDto> ProcesarNoShowAsync(CancellationToken ct)
    {
        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_MarcarTicketsNoShow", null, ct);

        if (dt.Rows.Count == 0)
            return new NoShowResultDto { RegistrosProcesados = 0 };

        var row = dt.Rows[0];
        var col = dt.HasColumn("RegistrosProcesados") ? "RegistrosProcesados"
                : dt.HasColumn("registrosProcesados") ? "registrosProcesados"
                : dt.Columns[0].ColumnName;

        return new NoShowResultDto { RegistrosProcesados = row.Int32(col) };
    }

    // ─── Listar tickets ──────────────────────────────────────────────────────

    public async Task<List<TicketDto>> ListarTicketsAsync(
        int? sedeId, int? servicioId, string? estado, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.Int     ("@SedeId",     sedeId),
            Sql.Int     ("@ServicioId", servicioId),
            Sql.NVarChar("@Estado",     estado, 30),
        };

        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_Ticket_Listar", parameters, ct);
        return [.. dt.Rows.Cast<DataRow>().Select(MapTicket)];
    }

    // ─── Obtener ticket por ID ───────────────────────────────────────────────

    public async Task<TicketDto> ObtenerTicketAsync(long ticketId, CancellationToken ct)
    {
        var parameters = new[] { Sql.BigInt("@TicketId", ticketId) };
        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_Ticket_Obtener", parameters, ct);

        if (dt.Rows.Count == 0)
            throw new NotFoundException($"Ticket {ticketId} no encontrado.");

        return MapTicket(dt.Rows[0]);
    }

    // ─── Obtener ticket por número ───────────────────────────────────────────

    public async Task<TicketDto> ObtenerTicketPorNumeroAsync(string numero, CancellationToken ct)
    {
        var parameters = new[] { Sql.NVarChar("@NumeroTicket", numero, 30) };
        var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_Ticket_Obtener", parameters, ct);

        if (dt.Rows.Count == 0)
            throw new NotFoundException($"Ticket '{numero}' no encontrado.");

        return MapTicket(dt.Rows[0]);
    }

    // ─── Resumen operativo ───────────────────────────────────────────────────

    public async Task<ResumenOperativoDto> ObtenerResumenOperativoAsync(
        int? sedeId, int? servicioId, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.Int("@SedeId",     sedeId),
            Sql.Int("@ServicioId", servicioId),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_Recepcion_ResumenOperativo", parameters, ct);

        ResumenOperativoDto resumen = new() { SedeId = sedeId, ServicioId = servicioId };

        // Table 0: métricas totales
        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
        {
            var row = ds.Tables[0].Rows[0];
            resumen = resumen with
            {
                TicketsEnEspera      = row.Int32("TicketsEnEspera"),
                TicketsLlamados      = row.Int32("TicketsLlamados"),
                TicketsEnAtencion    = row.Int32("TicketsEnAtencion"),
                TicketsFinalizados   = row.Int32("TicketsFinalizados"),
                TicketsNoShow        = row.Int32("TicketsNoShow"),
                TicketsEspecialesHoy = row.Int32("TicketsEspecialesHoy"),
            };
        }

        // Table 1: último ticket llamado
        if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
        {
            var row = ds.Tables[1].Rows[0];
            resumen = resumen with
            {
                UltimoTicketLlamado = row.StrNull("NumeroTicket"),
                SedeNombre          = row.StrNull("SedeNombre"),
                ServicioNombre      = row.StrNull("ServicioNombre"),
            };
        }

        return resumen;
    }

    // ─── Mappers internos ────────────────────────────────────────────────────

    private static TicketDto MapTicketFromDataSet(DataSet ds, string spName)
    {
        // Los SPs devuelven en Table[0] el status y en Table[1] el ticket,
        // o directamente en Table[0] el ticket si el SP es más simple.
        DataTable? ticketTable = null;

        // Busca la tabla que tenga la columna TicketId
        foreach (DataTable t in ds.Tables)
        {
            if (t.HasColumn("TicketId") || t.HasColumn("ticketId"))
            {
                ticketTable = t;
                break;
            }
        }

        // Si no encontramos por columna, intentamos leer Table[0] o Table[1]
        if (ticketTable is null)
        {
            // Table[0] podría ser el resultado de estado HTTP del SP (HttpStatus, Code, Message)
            // En ese caso revisamos si hay error
            if (ds.Tables.Count > 0)
            {
                var statusTable = ds.Tables[0];
                if (statusTable.Rows.Count > 0 && statusTable.HasColumn("HttpStatus"))
                {
                    var statusRow  = statusTable.Rows[0];
                    int httpStatus = statusRow.Int32("HttpStatus");
                    string code    = statusRow.Str("Code");
                    string message = statusRow.Str("Message");

                    if (httpStatus == 409)
                        throw new ConflictException(message, code);
                    if (httpStatus is >= 400 and < 500)
                        throw new BusinessException(message, code);
                    if (httpStatus >= 500)
                        throw new InvalidOperationException(message);
                }

                // Intenta tabla siguiente
                ticketTable = ds.Tables.Count > 1 ? ds.Tables[1] : ds.Tables[0];
            }
        }

        if (ticketTable is null || ticketTable.Rows.Count == 0)
            throw new NotFoundException($"{spName} no devolvió datos.");

        return MapTicket(ticketTable.Rows[0]);
    }

    private static TicketDto MapTicket(DataRow row) => new()
    {
        TicketId              = row.Int64("TicketId"),
        NumeroTicket          = row.Str("NumeroTicket"),
        Estado                = row.Str("Estado"),
        Prioridad             = row.Str("Prioridad"),
        EsEspecial            = row.Bool("EsEspecial"),
        MotivoEspecial        = row.StrNull("MotivoEspecial"),
        CitaId                = row.Int64Null("CitaId"),
        CitaEstado            = row.StrNull("CitaEstado"),
        PacienteId            = row.Int64("PacienteId"),
        PacienteNombre        = row.Str("PacienteNombre"),
        NumeroExpediente      = row.StrNull("NumeroExpediente"),
        PacienteDocumento     = row.StrNull("PacienteDocumento"),
        SedeId                = row.Int32("SedeId"),
        SedeNombre            = row.Str("SedeNombre"),
        ServicioId            = row.Int32("ServicioId"),
        ServicioNombre        = row.Str("ServicioNombre"),
        EspecialidadNombre    = row.StrNull("EspecialidadNombre"),
        MedicoId              = row.Int32Null("MedicoId"),
        MedicoNombre          = row.StrNull("MedicoNombre"),
        ConsultorioId         = row.Int32Null("ConsultorioId"),
        ConsultorioNombre     = row.StrNull("ConsultorioNombre"),
        AutorizadoPorId       = row.Int32Null("AutorizadoPorId"),
        AutorizadoPorNombre   = row.StrNull("AutorizadoPorNombre"),
        FechaGeneracion       = row.DateTime("FechaGeneracion"),
        FechaLlamado          = row.DateTimeNull("FechaLlamado"),
        FechaInicioAtencion   = row.DateTimeNull("FechaInicioAtencion"),
        FechaFinAtencion      = row.DateTimeNull("FechaFinAtencion"),
        ContadorLlamados      = row.Int32("ContadorLlamados"),
    };
}
