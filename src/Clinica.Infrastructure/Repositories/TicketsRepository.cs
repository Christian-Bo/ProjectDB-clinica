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
            Sql.BigInt  ("@CitaId",               citaId),
            Sql.BigInt  ("@PacienteId",           pacienteId),
            Sql.Int     ("@SedeId",               sedeId),
            Sql.Int     ("@ServicioId",           servicioId),
            // @MedicoId eliminado — sp_GenerarTicket no acepta ese parámetro
            // el SP lo obtiene internamente desde la cita (SELECT @MedicoId = c.MedicoId)
            Sql.NVarChar("@PrioridadSolicitada",  prioridad, 30),
            Sql.NVarChar("@MotivoEspecial",       motivo, 500),
            Sql.Int     ("@UsuarioId",            usuarioId),
            Sql.UniqueId("@IdempotencyKey",       idempotencyKey),
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

    // ─── Generar ticket desde kiosco ───────────────────────────────────────────

    public async Task<TicketDto> GenerarTicketKioscoAsync(
        long? pacienteId, string? documentoPaciente, bool usarPacienteNoAplica,
        int sedeId, int servicioId, string prioridad, string? motivo,
        int? usuarioId, Guid? idempotencyKey, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.BigInt  ("@PacienteId",            pacienteId),
            Sql.NVarChar("@DocumentoPaciente",     documentoPaciente, 30),
            Sql.Bit     ("@UsarPacienteNoAplica",  usarPacienteNoAplica),
            Sql.Int     ("@SedeId",                sedeId),
            Sql.Int     ("@ServicioId",            servicioId),
            Sql.NVarChar("@PrioridadSolicitada",   prioridad, 30),
            Sql.NVarChar("@MotivoEspecial",        motivo, 500),
            Sql.Int     ("@UsuarioId",             usuarioId),
            Sql.UniqueId("@IdempotencyKey",        idempotencyKey),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_GenerarTicketKiosco", parameters, ct);
        return MapTicketFromDataSet(ds, "dbo.sp_GenerarTicketKiosco");
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

    // ─── Cancelar ticket ──────────────────────────────────────────────────────

    public async Task<TicketDto> CancelarTicketAsync(long ticketId, string? motivo, int? usuarioId, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.BigInt  ("@TicketId",  ticketId),
            Sql.NVarChar("@Motivo",    motivo, 500),
            Sql.Int     ("@UsuarioId", usuarioId),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_Ticket_Cancelar", parameters, ct);
        return MapTicketFromDataSet(ds, "dbo.sp_Ticket_Cancelar");
    }

    // ─── Volver a llamar ticket ──────────────────────────────────────────────

    public async Task<TicketDto> RellamarTicketAsync(long ticketId, int? usuarioId, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.BigInt("@TicketId",  ticketId),
            Sql.Int   ("@UsuarioId", usuarioId),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_Ticket_Rellamar", parameters, ct);
        return MapTicketFromDataSet(ds, "dbo.sp_Ticket_Rellamar");
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


    // ─── Seguimiento del paciente ───────────────────────────────────────────

    public async Task<List<TicketSeguimientoPacienteDto>> ObtenerSeguimientoPacienteAsync(
        long pacienteId, int top, CancellationToken ct)
    {
        var parameters = new[]
        {
            Sql.BigInt("@PacienteId", pacienteId),
            Sql.Int("@Top", top <= 0 ? 5 : top),
        };

        var ds = await db.ExecuteSpAsync("dbo.sp_Ticket_SeguimientoPaciente", parameters, ct);
        var ticketsTable = ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        var stepsTable = ds.Tables.Count > 1 ? ds.Tables[1] : new DataTable();

        var tickets = ticketsTable.Rows
            .Cast<DataRow>()
            .Select(MapSeguimientoTicket)
            .ToList();

        var index = tickets.ToDictionary(t => t.TicketId);

        foreach (DataRow row in stepsTable.Rows)
        {
            var ticketId = row.Int64("TicketId");
            if (index.TryGetValue(ticketId, out var ticket))
            {
                ticket.Pasos.Add(MapSeguimientoPaso(row));
            }
        }

        foreach (var ticket in tickets)
        {
            ticket.Pasos.Sort((a, b) => a.Orden.CompareTo(b.Orden));
        }

        return tickets;
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


    private static TicketSeguimientoPacienteDto MapSeguimientoTicket(DataRow row) => new()
    {
        TicketId              = row.Int64("TicketId"),
        NumeroTicket          = row.Str("NumeroTicket"),
        Estado                = row.Str("Estado"),
        Prioridad             = row.Str("Prioridad"),
        EsEspecial            = row.Table.HasColumn("EsEspecial") && row.Bool("EsEspecial"),
        CitaId                = row.Table.HasColumn("CitaId") ? row.Int64Null("CitaId") : null,
        CitaEstado            = row.Table.HasColumn("CitaEstado") ? row.StrNull("CitaEstado") : null,
        FechaCita             = row.Table.HasColumn("FechaCita") ? row.DateTimeNull("FechaCita") : null,
        PacienteId            = row.Int64("PacienteId"),
        PacienteNombre        = row.Str("PacienteNombre"),
        NumeroExpediente      = row.Table.HasColumn("NumeroExpediente") ? row.StrNull("NumeroExpediente") : null,
        SedeId                = row.Int32("SedeId"),
        SedeNombre            = row.Str("SedeNombre"),
        ServicioId            = row.Int32("ServicioId"),
        ServicioNombre        = row.Str("ServicioNombre"),
        EspecialidadNombre    = row.Table.HasColumn("EspecialidadNombre") ? row.StrNull("EspecialidadNombre") : null,
        VentanillaNombre      = row.Table.HasColumn("VentanillaNombre") ? row.StrNull("VentanillaNombre") : null,
        MedicoId              = row.Table.HasColumn("MedicoId") ? row.Int32Null("MedicoId") : null,
        MedicoNombre          = row.Table.HasColumn("MedicoNombre") ? row.StrNull("MedicoNombre") : null,
        ConsultorioId         = row.Table.HasColumn("ConsultorioId") ? row.Int32Null("ConsultorioId") : null,
        ConsultorioNombre     = row.Table.HasColumn("ConsultorioNombre") ? row.StrNull("ConsultorioNombre") : null,
        DestinoTipo           = row.Table.HasColumn("DestinoTipo") ? row.StrNull("DestinoTipo") : null,
        DestinoActual         = row.Table.HasColumn("DestinoActual") ? row.StrNull("DestinoActual") : null,
        EtapaActual           = row.Table.HasColumn("EtapaActual") ? row.Str("EtapaActual") : string.Empty,
        FechaGeneracion       = row.DateTime("FechaGeneracion"),
        FechaLlamado          = row.DateTimeNull("FechaLlamado"),
        FechaInicioAtencion   = row.DateTimeNull("FechaInicioAtencion"),
        FechaFinAtencion      = row.DateTimeNull("FechaFinAtencion"),
        ContadorLlamados      = row.Int32("ContadorLlamados"),
    };

    private static TicketSeguimientoPasoDto MapSeguimientoPaso(DataRow row) => new()
    {
        TicketId     = row.Int64("TicketId"),
        Orden        = row.Int32("Orden"),
        Codigo       = row.Str("Codigo"),
        Titulo       = row.Str("Titulo"),
        Descripcion  = row.Str("Descripcion"),
        Estado       = row.Str("Estado"),
        Fecha        = row.Table.HasColumn("Fecha") ? row.DateTimeNull("Fecha") : null,
        Lugar        = row.Table.HasColumn("Lugar") ? row.StrNull("Lugar") : null,
        Responsable  = row.Table.HasColumn("Responsable") ? row.StrNull("Responsable") : null,
        Ayuda        = row.Table.HasColumn("Ayuda") ? row.StrNull("Ayuda") : null,
    };

    private static TicketDto MapTicketFromDataSet(DataSet ds, string spName)
    {
        DataTable? statusTable = null;
        foreach (DataTable table in ds.Tables)
        {
            if (table.Rows.Count > 0 && (table.HasColumn("HttpStatus") || table.HasColumn("StatusCode")))
            {
                statusTable = table;
                break;
            }
        }

        if (statusTable is not null)
        {
            var statusRow = statusTable.Rows[0];
            var statusColumn = statusTable.HasColumn("HttpStatus") ? "HttpStatus" : "StatusCode";
            int httpStatus = statusRow.Int32(statusColumn);
            string code = statusTable.HasColumn("Code")
                ? statusRow.Str("Code")
                : statusTable.HasColumn("Codigo")
                    ? statusRow.Str("Codigo")
                    : "OPERACION_TICKET";
            string message = statusTable.HasColumn("Message")
                ? statusRow.Str("Message")
                : statusTable.HasColumn("Mensaje")
                    ? statusRow.Str("Mensaje")
                    : "La operacion de ticket no pudo completarse.";

            if (httpStatus == 409)
                throw new ConflictException(message, code);
            if (httpStatus is >= 400 and < 500)
                throw new BusinessException(message, code);
            if (httpStatus >= 500)
                throw new InvalidOperationException(message);
        }

        DataTable? ticketTable = null;
        foreach (DataTable table in ds.Tables)
        {
            if (table.HasColumn("TicketId") || table.HasColumn("ticketId"))
            {
                ticketTable = table;
                break;
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
        EsEspecial            = row.Table.HasColumn("EsEspecial") && row.Bool("EsEspecial"),
        MotivoEspecial        = row.Table.HasColumn("MotivoEspecial") ? row.StrNull("MotivoEspecial") : null,
        CitaId                = row.Table.HasColumn("CitaId") ? row.Int64Null("CitaId") : null,
        CitaEstado            = row.Table.HasColumn("CitaEstado") ? row.StrNull("CitaEstado") : null,
        PacienteId            = row.Int64("PacienteId"),
        PacienteNombre        = row.Str("PacienteNombre"),
        NumeroExpediente      = row.Table.HasColumn("NumeroExpediente") ? row.StrNull("NumeroExpediente") : null,
        PacienteDocumento     = row.Table.HasColumn("PacienteDocumento") ? row.StrNull("PacienteDocumento") : null,
        SedeId                = row.Int32("SedeId"),
        SedeNombre            = row.Str("SedeNombre"),
        ServicioId            = row.Int32("ServicioId"),
        ServicioNombre        = row.Str("ServicioNombre"),
        EspecialidadNombre    = row.Table.HasColumn("EspecialidadNombre") ? row.StrNull("EspecialidadNombre") : null,
        VentanillaNumero      = row.Table.HasColumn("VentanillaNumero") ? row.Int32Null("VentanillaNumero") : null,
        VentanillaNombre      = row.Table.HasColumn("VentanillaNombre") ? row.StrNull("VentanillaNombre") : null,
        MedicoId              = row.Table.HasColumn("MedicoId") ? row.Int32Null("MedicoId") : null,
        MedicoNombre          = row.Table.HasColumn("MedicoNombre") ? row.StrNull("MedicoNombre") : null,
        ConsultorioId         = row.Table.HasColumn("ConsultorioId") ? row.Int32Null("ConsultorioId") : null,
        ConsultorioNombre     = row.Table.HasColumn("ConsultorioNombre") ? row.StrNull("ConsultorioNombre") : null,
        AutorizadoPorId       = row.Table.HasColumn("AutorizadoPorId") ? row.Int32Null("AutorizadoPorId") : null,
        AutorizadoPorNombre   = row.Table.HasColumn("AutorizadoPorNombre") ? row.StrNull("AutorizadoPorNombre") : null,
        FechaGeneracion       = row.DateTime("FechaGeneracion"),
        FechaLlamado          = row.DateTimeNull("FechaLlamado"),
        FechaInicioAtencion   = row.DateTimeNull("FechaInicioAtencion"),
        FechaFinAtencion      = row.DateTimeNull("FechaFinAtencion"),
        ContadorLlamados      = row.Int32("ContadorLlamados"),
    };
}