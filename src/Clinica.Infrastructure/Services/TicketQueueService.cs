using System.Data;
using System.Text.Json;
using Clinica.Application.Contracts;
using Clinica.Application.Models.Common;
using Clinica.Application.Models.Tickets;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clinica.Infrastructure.Services;

// -----------------------------------------------------------------------------
// Servicio principal del modulo 3.
// Responsabilidades:
// 1) Consumir los Stored Procedures de tickets/cola.
// 2) Enriquecer la informacion para que el frontend vea nombres utiles.
// 3) Devolver respuestas coherentes y faciles de consumir en Next.js.
// 4) Mantener la logica del backend separada del acceso a SQL.
// -----------------------------------------------------------------------------
public sealed class TicketQueueService : ITicketQueueService
{
    private readonly DatabaseConnection _databaseConnection;
    private readonly ILogger<TicketQueueService> _logger;

    public TicketQueueService(DatabaseConnection databaseConnection, ILogger<TicketQueueService> logger)
    {
        _databaseConnection = databaseConnection;
        _logger = logger;
    }

    public async Task<ServiceOperationResult<TicketDetailDto>> GenerateTicketAsync(
        GenerateTicketRequestDto request,
        Guid? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateStoredProcedureCommand(connection, "dbo.sp_GenerarTicket");
        AddParameter(command, "@CitaId", request.CitaId);
        AddParameter(command, "@PacienteId", request.PacienteId);
        AddParameter(command, "@SedeId", request.SedeId);
        AddParameter(command, "@ServicioId", request.ServicioId);
        AddParameter(command, "@MedicoId", request.MedicoId);
        AddParameter(command, "@PrioridadSolicitada", request.PrioridadSolicitada);
        AddParameter(command, "@MotivoEspecial", request.MotivoEspecial);
        AddParameter(command, "@UsuarioId", request.UsuarioId);
        AddParameter(command, "@IdempotencyKey", idempotencyKey);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);
        TicketDetailDto? detail = null;

        if (envelope.HttpStatus is >= 200 and < 300)
        {
            var ticketId = envelope.TicketId ?? TryExtractTicketIdFromJson(envelope.Message);
            if (ticketId.HasValue)
            {
                detail = await LoadTicketDetailByIdAsync(connection, ticketId.Value, cancellationToken);
            }
        }

        return new ServiceOperationResult<TicketDetailDto>
        {
            HttpStatus = envelope.HttpStatus,
            Code = envelope.Code,
            Message = NormalizeMessage(envelope),
            Data = detail
        };
    }

    public async Task<ServiceOperationResult<TicketDetailDto>> CallNextAsync(
        CallNextTicketRequestDto request,
        Guid? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateStoredProcedureCommand(connection, "dbo.sp_LlamarSiguienteTicket");
        AddParameter(command, "@SedeId", request.SedeId);
        AddParameter(command, "@ServicioId", request.ServicioId);
        AddParameter(command, "@EstacionId", request.EstacionId);
        AddParameter(command, "@UsuarioId", request.UsuarioId);
        AddParameter(command, "@IdempotencyKey", idempotencyKey);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);
        TicketDetailDto? detail = null;

        if (envelope.HttpStatus is >= 200 and < 300)
        {
            var ticketId = envelope.TicketId ?? TryExtractTicketIdFromJson(envelope.Message);
            if (ticketId.HasValue)
            {
                detail = await LoadTicketDetailByIdAsync(connection, ticketId.Value, cancellationToken);
            }
        }

        return new ServiceOperationResult<TicketDetailDto>
        {
            HttpStatus = envelope.HttpStatus,
            Code = envelope.Code,
            Message = NormalizeMessage(envelope),
            Data = detail
        };
    }

    public async Task<ServiceOperationResult<TicketDetailDto>> MarkInAttentionAsync(long ticketId, CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateStoredProcedureCommand(connection, "dbo.sp_MarcarTicketEnAtencion");
        AddParameter(command, "@TicketId", ticketId);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);
        var detail = envelope.HttpStatus is >= 200 and < 300
            ? await LoadTicketDetailByIdAsync(connection, ticketId, cancellationToken)
            : null;

        return new ServiceOperationResult<TicketDetailDto>
        {
            HttpStatus = envelope.HttpStatus,
            Code = envelope.Code,
            Message = NormalizeMessage(envelope),
            Data = detail
        };
    }

    public async Task<ServiceOperationResult<TicketDetailDto>> FinishAsync(long ticketId, FinalizeTicketRequestDto request, CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateStoredProcedureCommand(connection, "dbo.sp_FinalizarTicket");
        AddParameter(command, "@TicketId", ticketId);
        AddParameter(command, "@Motivo", request.Motivo);

        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);
        var detail = envelope.HttpStatus is >= 200 and < 300
            ? await LoadTicketDetailByIdAsync(connection, ticketId, cancellationToken)
            : null;

        return new ServiceOperationResult<TicketDetailDto>
        {
            HttpStatus = envelope.HttpStatus,
            Code = envelope.Code,
            Message = NormalizeMessage(envelope),
            Data = detail
        };
    }

    public async Task<ServiceOperationResult<NoShowProcessResponseDto>> ProcessNoShowAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateStoredProcedureCommand(connection, "dbo.sp_MarcarTicketsNoShow");
        var envelope = await ExecuteEnvelopeAsync(command, cancellationToken);

        return new ServiceOperationResult<NoShowProcessResponseDto>
        {
            HttpStatus = envelope.HttpStatus,
            Code = envelope.Code,
            Message = NormalizeMessage(envelope),
            Data = new NoShowProcessResponseDto
            {
                RegistrosProcesados = envelope.Registros ?? 0
            }
        };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<TicketDetailDto>>> ListAsync(TicketListFiltersDto filters, CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var ticketIds = new List<long>();

        await using (var command = CreateStoredProcedureCommand(connection, "dbo.sp_Ticket_Listar"))
        {
            AddParameter(command, "@SedeId", filters.SedeId);
            AddParameter(command, "@ServicioId", filters.ServicioId);
            AddParameter(command, "@Estado", filters.Estado);
            AddParameter(command, "@Prioridad", filters.Prioridad);
            AddParameter(command, "@FechaDesde", filters.FechaDesde);
            AddParameter(command, "@FechaHasta", filters.FechaHasta);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ticketIds.Add(reader.GetInt64OrDefault("TicketId"));
            }
        }

        IReadOnlyList<TicketDetailDto> data = ticketIds.Count == 0
            ? Array.Empty<TicketDetailDto>()
            : await LoadTicketDetailsByIdsAsync(connection, ticketIds, cancellationToken);

        return new ServiceOperationResult<IReadOnlyList<TicketDetailDto>>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "TICKETS_LISTADOS",
            Message = data.Count == 0
                ? "No se encontraron tickets con los filtros enviados."
                : "Tickets obtenidos correctamente.",
            Data = data
        };
    }

    public async Task<ServiceOperationResult<ReceptionOperationalSummaryDto>> GetOperationalSummaryAsync(
        int? sedeId,
        int? servicioId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandTimeout = 60;
        command.CommandText = @"
SELECT
    SUM(CASE WHEN t.Estado = N'ESPERA' THEN 1 ELSE 0 END) AS TicketsEnEspera,
    SUM(CASE WHEN t.Estado = N'LLAMADO' THEN 1 ELSE 0 END) AS TicketsLlamados,
    SUM(CASE WHEN t.Estado = N'EN_ATENCION' THEN 1 ELSE 0 END) AS TicketsEnAtencion,
    SUM(CASE WHEN t.Estado = N'FINALIZADO' THEN 1 ELSE 0 END) AS TicketsFinalizados,
    SUM(CASE WHEN t.Estado = N'NO_SHOW' THEN 1 ELSE 0 END) AS TicketsNoShow,
    SUM(CASE WHEN t.Prioridad = N'ESPECIAL' THEN 1 ELSE 0 END) AS TicketsEspecialesHoy
FROM dbo.Tickets t
WHERE CAST(t.FechaGeneracion AS date) = CAST(SYSUTCDATETIME() AS date)
  AND (@SedeId IS NULL OR t.SedeId = @SedeId)
  AND (@ServicioId IS NULL OR t.ServicioId = @ServicioId);

SELECT TOP (1)
    t.NumeroTicket,
    s.Nombre AS SedeNombre,
    sv.Nombre AS ServicioNombre
FROM dbo.Tickets t
INNER JOIN dbo.Sedes s ON s.SedeId = t.SedeId
INNER JOIN dbo.Servicios sv ON sv.ServicioId = t.ServicioId
WHERE CAST(t.FechaGeneracion AS date) = CAST(SYSUTCDATETIME() AS date)
  AND (@SedeId IS NULL OR t.SedeId = @SedeId)
  AND (@ServicioId IS NULL OR t.ServicioId = @ServicioId)
  AND t.Estado IN (N'LLAMADO', N'EN_ATENCION')
ORDER BY ISNULL(t.FechaLlamado, t.FechaInicioAtencion) DESC, t.TicketId DESC;";
        AddParameter(command, "@SedeId", sedeId);
        AddParameter(command, "@ServicioId", servicioId);

        var summary = new ReceptionOperationalSummaryDto
        {
            SedeId = sedeId,
            ServicioId = servicioId,
            ConsultadoEnUtc = DateTime.UtcNow
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            summary = new ReceptionOperationalSummaryDto
            {
                SedeId = sedeId,
                ServicioId = servicioId,
                TicketsEnEspera = reader.GetInt32OrDefault("TicketsEnEspera"),
                TicketsLlamados = reader.GetInt32OrDefault("TicketsLlamados"),
                TicketsEnAtencion = reader.GetInt32OrDefault("TicketsEnAtencion"),
                TicketsFinalizados = reader.GetInt32OrDefault("TicketsFinalizados"),
                TicketsNoShow = reader.GetInt32OrDefault("TicketsNoShow"),
                TicketsEspecialesHoy = reader.GetInt32OrDefault("TicketsEspecialesHoy"),
                ConsultadoEnUtc = DateTime.UtcNow
            };
        }

        if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
        {
            summary = new ReceptionOperationalSummaryDto
            {
                SedeId = summary.SedeId,
                ServicioId = summary.ServicioId,
                TicketsEnEspera = summary.TicketsEnEspera,
                TicketsLlamados = summary.TicketsLlamados,
                TicketsEnAtencion = summary.TicketsEnAtencion,
                TicketsFinalizados = summary.TicketsFinalizados,
                TicketsNoShow = summary.TicketsNoShow,
                TicketsEspecialesHoy = summary.TicketsEspecialesHoy,
                UltimoTicketLlamado = reader.GetNullableString("NumeroTicket"),
                SedeNombre = reader.GetNullableString("SedeNombre"),
                ServicioNombre = reader.GetNullableString("ServicioNombre"),
                ConsultadoEnUtc = summary.ConsultadoEnUtc
            };
        }

        if (string.IsNullOrWhiteSpace(summary.SedeNombre) || string.IsNullOrWhiteSpace(summary.ServicioNombre))
        {
            var contexto = await LoadQueueContextAsync(connection, sedeId, servicioId, cancellationToken);
            summary = new ReceptionOperationalSummaryDto
            {
                SedeId = summary.SedeId,
                ServicioId = summary.ServicioId,
                TicketsEnEspera = summary.TicketsEnEspera,
                TicketsLlamados = summary.TicketsLlamados,
                TicketsEnAtencion = summary.TicketsEnAtencion,
                TicketsFinalizados = summary.TicketsFinalizados,
                TicketsNoShow = summary.TicketsNoShow,
                TicketsEspecialesHoy = summary.TicketsEspecialesHoy,
                UltimoTicketLlamado = summary.UltimoTicketLlamado,
                SedeNombre = string.IsNullOrWhiteSpace(summary.SedeNombre) ? contexto.SedeNombre : summary.SedeNombre,
                ServicioNombre = string.IsNullOrWhiteSpace(summary.ServicioNombre) ? contexto.ServicioNombre : summary.ServicioNombre,
                ConsultadoEnUtc = summary.ConsultadoEnUtc
            };
        }

        return new ServiceOperationResult<ReceptionOperationalSummaryDto>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "RESUMEN_OPERATIVO_OK",
            Message = "Resumen operativo obtenido correctamente.",
            Data = summary
        };
    }

    public async Task<ServiceOperationResult<TicketDetailDto>> GetByIdAsync(long ticketId, CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        long? foundId = null;
        await using (var command = CreateStoredProcedureCommand(connection, "dbo.sp_Ticket_Obtener"))
        {
            AddParameter(command, "@TicketId", ticketId);
            AddParameter(command, "@NumeroTicket", null);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                foundId = reader.GetNullableInt64("TicketId");
            }
        }

        if (!foundId.HasValue)
        {
            return new ServiceOperationResult<TicketDetailDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "TICKET_NO_ENCONTRADO",
                Message = "No se encontro el ticket solicitado."
            };
        }

        var detail = await LoadTicketDetailByIdAsync(connection, foundId.Value, cancellationToken);

        return new ServiceOperationResult<TicketDetailDto>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "TICKET_OBTENIDO",
            Message = "Ticket obtenido correctamente.",
            Data = detail
        };
    }

    public async Task<ServiceOperationResult<TicketDetailDto>> GetByNumberAsync(string numeroTicket, CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        long? foundId = null;
        await using (var command = CreateStoredProcedureCommand(connection, "dbo.sp_Ticket_Obtener"))
        {
            AddParameter(command, "@TicketId", null);
            AddParameter(command, "@NumeroTicket", numeroTicket);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                foundId = reader.GetNullableInt64("TicketId");
            }
        }

        if (!foundId.HasValue)
        {
            return new ServiceOperationResult<TicketDetailDto>
            {
                HttpStatus = StatusCodes.Status404NotFound,
                Code = "TICKET_NO_ENCONTRADO",
                Message = "No se encontro un ticket con el numero indicado."
            };
        }

        var detail = await LoadTicketDetailByIdAsync(connection, foundId.Value, cancellationToken);

        return new ServiceOperationResult<TicketDetailDto>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "TICKET_OBTENIDO",
            Message = "Ticket obtenido correctamente.",
            Data = detail
        };
    }

    public async Task<ServiceOperationResult<QueueDisplayResponseDto>> GetQueueDisplayAsync(int sedeId, int servicioId, CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        QueueTicketPreviewDto? actual = null;
        var proximos = new List<QueueTicketPreviewDto>();

        await using (var command = CreateStoredProcedureCommand(connection, "dbo.sp_ObtenerPantallaCola"))
        {
            AddParameter(command, "@SedeId", sedeId);
            AddParameter(command, "@ServicioId", servicioId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                actual = new QueueTicketPreviewDto
                {
                    TicketId = reader.GetInt64OrDefault("TicketId"),
                    NumeroTicket = reader.GetNullableString("NumeroTicket") ?? string.Empty,
                    Prioridad = reader.GetNullableString("Prioridad") ?? string.Empty,
                    Estado = reader.GetNullableString("Estado") ?? string.Empty,
                    FechaReferencia = reader.GetNullableDateTime("FechaLlamado"),
                    ConsultorioId = reader.GetNullableInt32("ConsultorioId")
                };
            }

            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    proximos.Add(new QueueTicketPreviewDto
                    {
                        TicketId = reader.GetInt64OrDefault("TicketId"),
                        NumeroTicket = reader.GetNullableString("NumeroTicket") ?? string.Empty,
                        Prioridad = reader.GetNullableString("Prioridad") ?? string.Empty,
                        Estado = reader.GetNullableString("Estado") ?? string.Empty,
                        FechaReferencia = reader.GetNullableDateTime("FechaGeneracion")
                    });
                }
            }
        }

        var contexto = await LoadQueueContextAsync(connection, sedeId, servicioId, cancellationToken);

        if (actual?.ConsultorioId is int consultorioId)
        {
            actual.ConsultorioNombre = await LoadConsultorioNameAsync(connection, consultorioId, cancellationToken);
        }

        return new ServiceOperationResult<QueueDisplayResponseDto>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "PANTALLA_COLA_OK",
            Message = "Cola publica obtenida correctamente.",
            Data = new QueueDisplayResponseDto
            {
                SedeId = sedeId,
                SedeNombre = contexto.SedeNombre,
                ServicioId = servicioId,
                ServicioNombre = contexto.ServicioNombre,
                Actual = actual,
                Proximos = proximos,
                ConsultadoEnUtc = DateTime.UtcNow
            }
        };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetSedesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var data = new List<SelectionOptionDto>();
        await using var command = CreateStoredProcedureCommand(connection, "dbo.sp_Sede_Listar");
        AddParameter(command, "@Estado", "ACTIVA");
        AddParameter(command, "@MunicipioId", null);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32OrDefault("SedeId");
            var nombre = reader.GetNullableString("Nombre") ?? $"Sede #{id}";
            var codigo = reader.GetNullableString("Codigo");
            data.Add(new SelectionOptionDto
            {
                Id = id,
                Nombre = nombre,
                Codigo = codigo,
                Descripcion = reader.GetNullableString("Direccion"),
                Label = string.IsNullOrWhiteSpace(codigo) ? nombre : $"{nombre} ({codigo})",
                Activo = true
            });
        }

        return new ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "SEDES_LISTADAS",
            Message = "Sedes activas obtenidas correctamente.",
            Data = data
        };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetServiciosAsync(int? sedeId, CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var data = new List<SelectionOptionDto>();
        await using var command = CreateStoredProcedureCommand(connection, "dbo.sp_Servicio_Listar");
        AddParameter(command, "@SedeId", sedeId);
        AddParameter(command, "@EspecialidadId", null);
        AddParameter(command, "@Activo", true);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32OrDefault("ServicioId");
            var nombre = reader.GetNullableString("Nombre") ?? $"Servicio #{id}";
            var especialidad = reader.GetNullableString("EspecialidadNombre");
            var sedeNombre = reader.GetNullableString("SedeNombre");
            data.Add(new SelectionOptionDto
            {
                Id = id,
                Nombre = nombre,
                Codigo = null,
                Descripcion = especialidad,
                Label = string.IsNullOrWhiteSpace(especialidad)
                    ? nombre
                    : $"{nombre} - {especialidad}" + (string.IsNullOrWhiteSpace(sedeNombre) ? string.Empty : $" ({sedeNombre})"),
                Activo = true
            });
        }

        return new ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "SERVICIOS_LISTADOS",
            Message = "Servicios obtenidos correctamente.",
            Data = data
        };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetStationsAsync(int? sedeId, string? tipoEstacion, CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var data = new List<SelectionOptionDto>();
        await using var command = CreateStoredProcedureCommand(connection, "dbo.sp_EstacionAtencion_Listar");
        AddParameter(command, "@SedeId", sedeId);
        AddParameter(command, "@TipoEstacion", string.IsNullOrWhiteSpace(tipoEstacion) ? "RECEPCION" : tipoEstacion);
        AddParameter(command, "@Activo", true);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32OrDefault("EstacionId");
            var nombre = reader.GetNullableString("Nombre") ?? $"Estacion #{id}";
            var tipo = reader.GetNullableString("TipoEstacion");
            var sedeNombre = reader.GetNullableString("SedeNombre");
            data.Add(new SelectionOptionDto
            {
                Id = id,
                Nombre = nombre,
                Codigo = tipo,
                Descripcion = sedeNombre,
                Label = string.IsNullOrWhiteSpace(sedeNombre) ? nombre : $"{nombre} - {sedeNombre}",
                Activo = true
            });
        }

        return new ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "ESTACIONES_LISTADAS",
            Message = "Estaciones obtenidas correctamente.",
            Data = data
        };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<PatientSelectionDto>>> GetPatientsAsync(string? texto, CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var data = new List<PatientSelectionDto>();
        await using var command = CreateStoredProcedureCommand(connection, "dbo.sp_Paciente_Listar");
        AddParameter(command, "@Estado", "ACTIVO");
        AddParameter(command, "@Texto", string.IsNullOrWhiteSpace(texto) ? null : texto);
        AddParameter(command, "@MunicipioId", null);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var pacienteId = reader.GetInt32OrDefault("PacienteId");
            var nombre = BuildFullName(
                reader.GetNullableString("Nombres"),
                reader.GetNullableString("Apellidos"),
                $"Paciente #{pacienteId}");

            var expediente = reader.GetNullableString("NumeroExpediente");
            var documento = BuildDocumentLabel(reader.GetNullableString("TipoDocumento"), reader.GetNullableString("NumeroDocumento"));
            var telefono = reader.GetNullableString("Telefono");
            var correo = reader.GetNullableString("CorreoElectronico");

            var labelParts = new List<string> { nombre };
            if (!string.IsNullOrWhiteSpace(expediente)) labelParts.Add(expediente);
            if (!string.IsNullOrWhiteSpace(documento)) labelParts.Add(documento);
            if (!string.IsNullOrWhiteSpace(telefono)) labelParts.Add(telefono);

            data.Add(new PatientSelectionDto
            {
                PacienteId = pacienteId,
                Label = string.Join(" | ", labelParts),
                NumeroExpediente = expediente,
                Documento = documento,
                Telefono = telefono,
                CorreoElectronico = correo,
                EsDiscapacitado = reader.GetBooleanOrDefault("EsDiscapacitado")
            });
        }

        return new ServiceOperationResult<IReadOnlyList<PatientSelectionDto>>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "PACIENTES_LISTADOS",
            Message = "Pacientes obtenidos correctamente.",
            Data = data
        };
    }

    public async Task<ServiceOperationResult<IReadOnlyList<AppointmentSelectionDto>>> GetConfirmedAppointmentsAsync(
        int? sedeId,
        int? servicioId,
        string? texto,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _databaseConnection.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var citaIds = new List<long>();
        await using (var command = CreateStoredProcedureCommand(connection, "dbo.sp_Cita_Listar"))
        {
            AddParameter(command, "@PacienteId", null);
            AddParameter(command, "@MedicoId", null);
            AddParameter(command, "@SedeId", sedeId);
            AddParameter(command, "@ServicioId", servicioId);
            AddParameter(command, "@Estado", "CONFIRMADA");
            AddParameter(command, "@FechaDesde", DateTime.UtcNow.Date);
            AddParameter(command, "@FechaHasta", null);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                citaIds.Add(reader.GetInt64OrDefault("CitaId"));
            }
        }

        IReadOnlyList<AppointmentSelectionDto> data = citaIds.Count == 0
            ? Array.Empty<AppointmentSelectionDto>()
            : await LoadAppointmentsByIdsAsync(connection, citaIds, cancellationToken);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            data = data
                .Where(x => x.Label.Contains(texto, StringComparison.OrdinalIgnoreCase)
                         || x.PacienteNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
                         || x.ServicioNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
                         || x.SedeNombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
                         || (x.MedicoNombre?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        return new ServiceOperationResult<IReadOnlyList<AppointmentSelectionDto>>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "CITAS_CONFIRMADAS_LISTADAS",
            Message = "Citas confirmadas obtenidas correctamente.",
            Data = data
        };
    }

    public Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetTicketPrioritiesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SelectionOptionDto> data = new List<SelectionOptionDto>
        {
            new() { Id = 1, Nombre = "NORMAL", Label = "Normal", Codigo = "NORMAL", Descripcion = "Atencion estandar" },
            new() { Id = 2, Nombre = "ANCIANO", Label = "Adulto mayor", Codigo = "ANCIANO", Descripcion = "Prioridad para pacientes de 65 anos o mas" },
            new() { Id = 3, Nombre = "DISCAPACIDAD", Label = "Discapacidad", Codigo = "DISCAPACIDAD", Descripcion = "Prioridad para paciente registrado como discapacitado" },
            new() { Id = 4, Nombre = "EMBARAZO", Label = "Embarazo", Codigo = "EMBARAZO", Descripcion = "Prioridad para pacientes embarazadas" },
            new() { Id = 5, Nombre = "ESPECIAL", Label = "Especial (Supervisor)", Codigo = "ESPECIAL", Descripcion = "Siempre pasa primero, requiere autorizacion de Supervisor" }
        };

        return Task.FromResult(new ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "PRIORIDADES_TICKET_LISTADAS",
            Message = "Prioridades de ticket obtenidas correctamente.",
            Data = data
        });
    }

    public Task<ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>> GetTicketStatesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SelectionOptionDto> data = new List<SelectionOptionDto>
        {
            new() { Id = 1, Nombre = "ESPERA", Label = "En espera", Codigo = "ESPERA", Descripcion = "Ticket generado pendiente de ser llamado" },
            new() { Id = 2, Nombre = "LLAMADO", Label = "Llamado", Codigo = "LLAMADO", Descripcion = "Ticket llamado a ventanilla o consultorio" },
            new() { Id = 3, Nombre = "EN_ATENCION", Label = "En atencion", Codigo = "EN_ATENCION", Descripcion = "Paciente siendo atendido" },
            new() { Id = 4, Nombre = "FINALIZADO", Label = "Finalizado", Codigo = "FINALIZADO", Descripcion = "Ticket concluido correctamente" },
            new() { Id = 5, Nombre = "NO_SHOW", Label = "No show", Codigo = "NO_SHOW", Descripcion = "Paciente no se presento a tiempo" }
        };

        return Task.FromResult(new ServiceOperationResult<IReadOnlyList<SelectionOptionDto>>
        {
            HttpStatus = StatusCodes.Status200OK,
            Code = "ESTADOS_TICKET_LISTADOS",
            Message = "Estados de ticket obtenidos correctamente.",
            Data = data
        });
    }

    private async Task<StoredProcedureEnvelope> ExecuteEnvelopeAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new StoredProcedureEnvelope
                {
                    HttpStatus = StatusCodes.Status500InternalServerError,
                    Code = "SP_SIN_RESPUESTA",
                    Message = "El stored procedure no devolvio una fila de resultado."
                };
            }

            return new StoredProcedureEnvelope
            {
                HttpStatus = reader.GetInt32OrDefault("HttpStatus", StatusCodes.Status500InternalServerError),
                Code = reader.GetNullableString("Codigo") ?? "SP_SIN_CODIGO",
                Message = reader.GetNullableString("Mensaje") ?? "Operacion ejecutada.",
                TicketId = reader.GetNullableInt64("TicketId"),
                NumeroTicket = reader.GetNullableString("NumeroTicket"),
                Registros = reader.GetNullableInt32("Registros")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando stored procedure {StoredProcedure}", command.CommandText);
            return new StoredProcedureEnvelope
            {
                HttpStatus = StatusCodes.Status500InternalServerError,
                Code = "ERROR_INFRAESTRUCTURA",
                Message = ex.Message
            };
        }
    }

    private async Task<TicketDetailDto?> LoadTicketDetailByIdAsync(SqlConnection connection, long ticketId, CancellationToken cancellationToken)
    {
        var list = await LoadTicketDetailsByIdsAsync(connection, new[] { ticketId }, cancellationToken);
        return list.FirstOrDefault();
    }

    private async Task<IReadOnlyList<TicketDetailDto>> LoadTicketDetailsByIdsAsync(SqlConnection connection, IReadOnlyCollection<long> ticketIds, CancellationToken cancellationToken)
    {
        if (ticketIds.Count == 0)
        {
            return Array.Empty<TicketDetailDto>();
        }

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandTimeout = 60;

        var parameterNames = new List<string>();
        var index = 0;
        foreach (var ticketId in ticketIds.Distinct())
        {
            var parameterName = $"@Id{index++}";
            parameterNames.Add(parameterName);
            command.Parameters.Add(new SqlParameter(parameterName, SqlDbType.BigInt) { Value = ticketId });
        }

        command.CommandText = $@"
SELECT
    t.TicketId,
    t.NumeroTicket,
    t.Estado,
    t.Prioridad,
    t.EsEspecial,
    t.MotivoEspecial,
    t.CitaId,
    c.Estado AS CitaEstado,
    t.PacienteId,
    COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(up.Nombres, N''), N' ', ISNULL(up.Apellidos, N'')))), N''), CONCAT(N'Paciente #', t.PacienteId)) AS PacienteNombre,
    p.NumeroExpediente,
    CONCAT(p.TipoDocumento, N': ', p.NumeroDocumento) AS PacienteDocumento,
    t.SedeId,
    s.Nombre AS SedeNombre,
    t.ServicioId,
    sv.Nombre AS ServicioNombre,
    esp.Nombre AS EspecialidadNombre,
    t.MedicoId,
    CASE WHEN um.UsuarioId IS NULL THEN NULL ELSE LTRIM(RTRIM(CONCAT(ISNULL(um.Nombres, N''), N' ', ISNULL(um.Apellidos, N'')))) END AS MedicoNombre,
    t.ConsultorioId,
    ct.Nombre AS ConsultorioNombre,
    t.AutorizadoPor AS AutorizadoPorId,
    CASE WHEN ua.UsuarioId IS NULL THEN NULL ELSE LTRIM(RTRIM(CONCAT(ISNULL(ua.Nombres, N''), N' ', ISNULL(ua.Apellidos, N'')))) END AS AutorizadoPorNombre,
    t.FechaGeneracion,
    t.FechaLlamado,
    t.FechaInicioAtencion,
    t.FechaFinAtencion,
    t.ContadorLlamados
FROM dbo.Tickets t
INNER JOIN dbo.Pacientes p ON p.PacienteId = t.PacienteId
LEFT JOIN dbo.Usuarios up ON up.UsuarioId = p.UsuarioId
INNER JOIN dbo.Sedes s ON s.SedeId = t.SedeId
INNER JOIN dbo.Servicios sv ON sv.ServicioId = t.ServicioId
LEFT JOIN dbo.Especialidades esp ON esp.EspecialidadId = sv.EspecialidadId
LEFT JOIN dbo.Medicos m ON m.MedicoId = t.MedicoId
LEFT JOIN dbo.Usuarios um ON um.UsuarioId = m.UsuarioId
LEFT JOIN dbo.Consultorios ct ON ct.ConsultorioId = t.ConsultorioId
LEFT JOIN dbo.Usuarios ua ON ua.UsuarioId = t.AutorizadoPor
LEFT JOIN dbo.Citas c ON c.CitaId = t.CitaId
WHERE t.TicketId IN ({string.Join(", ", parameterNames)});";

        var map = new Dictionary<long, TicketDetailDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var dto = new TicketDetailDto
            {
                TicketId = reader.GetInt64OrDefault("TicketId"),
                NumeroTicket = reader.GetNullableString("NumeroTicket") ?? string.Empty,
                Estado = reader.GetNullableString("Estado") ?? string.Empty,
                Prioridad = reader.GetNullableString("Prioridad") ?? string.Empty,
                EsEspecial = reader.GetBooleanOrDefault("EsEspecial"),
                MotivoEspecial = reader.GetNullableString("MotivoEspecial"),
                CitaId = reader.GetNullableInt64("CitaId"),
                CitaEstado = reader.GetNullableString("CitaEstado"),
                PacienteId = reader.GetInt32OrDefault("PacienteId"),
                PacienteNombre = reader.GetNullableString("PacienteNombre") ?? string.Empty,
                NumeroExpediente = reader.GetNullableString("NumeroExpediente"),
                PacienteDocumento = reader.GetNullableString("PacienteDocumento"),
                SedeId = reader.GetInt32OrDefault("SedeId"),
                SedeNombre = reader.GetNullableString("SedeNombre") ?? string.Empty,
                ServicioId = reader.GetInt32OrDefault("ServicioId"),
                ServicioNombre = reader.GetNullableString("ServicioNombre") ?? string.Empty,
                EspecialidadNombre = reader.GetNullableString("EspecialidadNombre"),
                MedicoId = reader.GetNullableInt32("MedicoId"),
                MedicoNombre = reader.GetNullableString("MedicoNombre"),
                ConsultorioId = reader.GetNullableInt32("ConsultorioId"),
                ConsultorioNombre = reader.GetNullableString("ConsultorioNombre"),
                AutorizadoPorId = reader.GetNullableInt32("AutorizadoPorId"),
                AutorizadoPorNombre = reader.GetNullableString("AutorizadoPorNombre"),
                FechaGeneracion = reader.GetDateTimeOrDefault("FechaGeneracion"),
                FechaLlamado = reader.GetNullableDateTime("FechaLlamado"),
                FechaInicioAtencion = reader.GetNullableDateTime("FechaInicioAtencion"),
                FechaFinAtencion = reader.GetNullableDateTime("FechaFinAtencion"),
                ContadorLlamados = reader.GetInt32OrDefault("ContadorLlamados")
            };

            map[dto.TicketId] = dto;
        }

        return ticketIds
            .Distinct()
            .Where(map.ContainsKey)
            .Select(id => map[id])
            .ToList();
    }

    private async Task<List<AppointmentSelectionDto>> LoadAppointmentsByIdsAsync(SqlConnection connection, IReadOnlyCollection<long> citaIds, CancellationToken cancellationToken)
    {
        if (citaIds.Count == 0)
        {
            return new List<AppointmentSelectionDto>();
        }

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandTimeout = 60;

        var parameterNames = new List<string>();
        var index = 0;
        foreach (var citaId in citaIds.Distinct())
        {
            var parameterName = $"@CitaId{index++}";
            parameterNames.Add(parameterName);
            command.Parameters.Add(new SqlParameter(parameterName, SqlDbType.BigInt) { Value = citaId });
        }

        command.CommandText = $@"
SELECT
    c.CitaId,
    c.PacienteId,
    c.FechaInicio,
    c.Estado,
    COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(up.Nombres, N''), N' ', ISNULL(up.Apellidos, N'')))), N''), CONCAT(N'Paciente #', c.PacienteId)) AS PacienteNombre,
    sv.Nombre AS ServicioNombre,
    s.Nombre AS SedeNombre,
    CASE WHEN um.UsuarioId IS NULL THEN NULL ELSE LTRIM(RTRIM(CONCAT(ISNULL(um.Nombres, N''), N' ', ISNULL(um.Apellidos, N'')))) END AS MedicoNombre,
    p.NumeroExpediente,
    p.NumeroDocumento
FROM dbo.Citas c
INNER JOIN dbo.Pacientes p ON p.PacienteId = c.PacienteId
LEFT JOIN dbo.Usuarios up ON up.UsuarioId = p.UsuarioId
INNER JOIN dbo.Sedes s ON s.SedeId = c.SedeId
INNER JOIN dbo.Servicios sv ON sv.ServicioId = c.ServicioId
LEFT JOIN dbo.Medicos m ON m.MedicoId = c.MedicoId
LEFT JOIN dbo.Usuarios um ON um.UsuarioId = m.UsuarioId
WHERE c.CitaId IN ({string.Join(", ", parameterNames)})
ORDER BY c.FechaInicio ASC, c.CitaId ASC;";

        var data = new List<AppointmentSelectionDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var citaId = reader.GetInt64OrDefault("CitaId");
            var pacienteNombre = reader.GetNullableString("PacienteNombre") ?? $"Paciente #{reader.GetInt32OrDefault("PacienteId")}";
            var servicioNombre = reader.GetNullableString("ServicioNombre") ?? string.Empty;
            var sedeNombre = reader.GetNullableString("SedeNombre") ?? string.Empty;
            var medicoNombre = reader.GetNullableString("MedicoNombre");
            var fechaInicio = reader.GetDateTimeOrDefault("FechaInicio");
            var expediente = reader.GetNullableString("NumeroExpediente");
            var documento = reader.GetNullableString("NumeroDocumento");

            var labelParts = new List<string>
            {
                pacienteNombre,
                fechaInicio.ToString("yyyy-MM-dd HH:mm"),
                servicioNombre,
                sedeNombre
            };

            if (!string.IsNullOrWhiteSpace(medicoNombre))
            {
                labelParts.Add($"Dr(a). {medicoNombre}");
            }

            if (!string.IsNullOrWhiteSpace(expediente))
            {
                labelParts.Add(expediente);
            }

            if (!string.IsNullOrWhiteSpace(documento))
            {
                labelParts.Add(documento);
            }

            data.Add(new AppointmentSelectionDto
            {
                CitaId = citaId,
                PacienteId = reader.GetInt32OrDefault("PacienteId"),
                FechaInicio = fechaInicio,
                Estado = reader.GetNullableString("Estado") ?? string.Empty,
                PacienteNombre = pacienteNombre,
                ServicioNombre = servicioNombre,
                SedeNombre = sedeNombre,
                MedicoNombre = medicoNombre,
                Label = string.Join(" | ", labelParts.Where(x => !string.IsNullOrWhiteSpace(x)))
            });
        }

        return data;
    }

    private async Task<(string SedeNombre, string ServicioNombre)> LoadQueueContextAsync(SqlConnection connection, int? sedeId, int? servicioId, CancellationToken cancellationToken)
    {
        if (!sedeId.HasValue || !servicioId.HasValue)
        {
            return (sedeId.HasValue ? $"Sede #{sedeId.Value}" : string.Empty, servicioId.HasValue ? $"Servicio #{servicioId.Value}" : string.Empty);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (1)
    s.Nombre AS SedeNombre,
    sv.Nombre AS ServicioNombre
FROM dbo.Sedes s
INNER JOIN dbo.Servicios sv ON sv.SedeId = s.SedeId
WHERE s.SedeId = @SedeId AND sv.ServicioId = @ServicioId;";
        command.Parameters.Add(new SqlParameter("@SedeId", SqlDbType.Int) { Value = sedeId.Value });
        command.Parameters.Add(new SqlParameter("@ServicioId", SqlDbType.Int) { Value = servicioId.Value });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return (reader.GetNullableString("SedeNombre") ?? $"Sede #{sedeId.Value}", reader.GetNullableString("ServicioNombre") ?? $"Servicio #{servicioId.Value}");
        }

        return ($"Sede #{sedeId.Value}", $"Servicio #{servicioId.Value}");
    }

    private async Task<string?> LoadConsultorioNameAsync(SqlConnection connection, int consultorioId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP (1) Nombre FROM dbo.Consultorios WHERE ConsultorioId = @ConsultorioId;";
        command.Parameters.Add(new SqlParameter("@ConsultorioId", SqlDbType.Int) { Value = consultorioId });

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar?.ToString();
    }

    private static SqlCommand CreateStoredProcedureCommand(SqlConnection connection, string procedureName)
    {
        var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedureName;
        command.CommandTimeout = 60;
        return command;
    }

    private static void AddParameter(SqlCommand command, string name, object? value)
    {
        command.Parameters.Add(new SqlParameter(name, value ?? DBNull.Value));
    }

    private static long? TryExtractTicketIdFromJson(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            if (document.RootElement.TryGetProperty("ticketId", out var ticketIdElement) && ticketIdElement.TryGetInt64(out var ticketId))
            {
                return ticketId;
            }
        }
        catch
        {
            // Si el mensaje no es JSON valido simplemente ignoramos.
        }

        return null;
    }

    private static string NormalizeMessage(StoredProcedureEnvelope envelope)
    {
        if (string.Equals(envelope.Code, "IDEMPOTENTE", StringComparison.OrdinalIgnoreCase))
        {
            return "La operacion ya habia sido ejecutada anteriormente; se devuelve el mismo resultado para evitar duplicados.";
        }

        return envelope.Message;
    }

    private static string BuildFullName(string? nombres, string? apellidos, string fallback)
    {
        var completeName = string.Join(" ", new[] { nombres, apellidos }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        return string.IsNullOrWhiteSpace(completeName) ? fallback : completeName;
    }

    private static string? BuildDocumentLabel(string? tipoDocumento, string? numeroDocumento)
    {
        if (string.IsNullOrWhiteSpace(tipoDocumento) && string.IsNullOrWhiteSpace(numeroDocumento))
        {
            return null;
        }

        return string.Join(": ", new[] { tipoDocumento, numeroDocumento }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private sealed class StoredProcedureEnvelope
    {
        public int HttpStatus { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public long? TicketId { get; init; }
        public string? NumeroTicket { get; init; }
        public int? Registros { get; init; }
    }
}
