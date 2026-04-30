using Clinica.Application.DTOs.Citas;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;

namespace Clinica.Infrastructure.Repositories;

public sealed class CitasRepository
{
    private readonly SqlExecutor _executor;

    public CitasRepository(SqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<SpResult> ReservarAsync(ReservarCitaRequestDto dto, Guid idempotencyKey, int usuarioId)
    {
        return await _executor.ExecuteAsync(
            "dbo.sp_ReservarCita",
            [
                new SqlParameter("@PacienteId",     dto.PacienteId),
                new SqlParameter("@SedeId",         dto.SedeId),
                new SqlParameter("@ServicioId",     dto.ServicioId),
                new SqlParameter("@MedicoId",       (object?)dto.MedicoId ?? DBNull.Value),
                new SqlParameter("@TipoConsultaId", dto.TipoConsultaId),
                new SqlParameter("@FechaInicio",    dto.FechaInicio),
                new SqlParameter("@Modalidad",      dto.Modalidad),
                new SqlParameter("@MotivoConsulta", (object?)dto.MotivoConsulta ?? DBNull.Value),
                new SqlParameter("@UsuarioId",      usuarioId),
                new SqlParameter("@IdempotencyKey", idempotencyKey)
            ]);
    }

    public async Task<SpResult> ConfirmarAsync(long citaId, int usuarioId, Guid idempotencyKey)
    {
        return await _executor.ExecuteAsync(
            "dbo.sp_ConfirmarCita",
            [
                new SqlParameter("@CitaId",         citaId),
                new SqlParameter("@UsuarioId",      usuarioId),
                new SqlParameter("@IdempotencyKey", idempotencyKey)
            ]);
    }

    public async Task<SpResult> CancelarAsync(long citaId, int usuarioId, string motivo)
    {
        return await _executor.ExecuteAsync(
            "dbo.sp_CancelarCita",
            [
                new SqlParameter("@CitaId",            citaId),
                new SqlParameter("@MotivoCancelacion", motivo),
                new SqlParameter("@UsuarioId",         usuarioId)
            ]);
    }

    public async Task<SpResult> ReprogramarAsync(long citaId, ReprogramarCitaRequestDto dto)
    {
        return await _executor.ExecuteAsync(
            "dbo.sp_ReprogramarCita",
            [
                new SqlParameter("@CitaId",           citaId),
                new SqlParameter("@NuevaFechaInicio", dto.NuevaFechaInicio),
                new SqlParameter("@UsuarioId",        dto.UsuarioId),
                new SqlParameter("@IdempotencyKey",   dto.IdempotencyKey)
            ]);
    }

    public async Task<CitaResponseDto?> ObtenerAsync(long citaId)
    {
        return await _executor.QuerySingleAsync(
            "dbo.sp_Cita_Obtener",
            [new SqlParameter("@CitaId", citaId)],
            reader => new CitaResponseDto
            {
                CitaId           = reader.GetInt64OrDefault("CitaId"),
                PacienteId       = reader.GetInt32OrDefault("PacienteId"),
                NumeroExpediente = string.Empty,
                SedeId           = reader.GetInt32OrDefault("SedeId"),
                NombreSede       = reader.GetNullableString("NombreSede") ?? string.Empty,
                ServicioId       = reader.GetInt32OrDefault("ServicioId"),
                NombreServicio   = reader.GetNullableString("NombreServicio") ?? string.Empty,
                MedicoId         = reader.GetNullableInt32("MedicoId"),
                NombreMedico     = null,
                FechaInicio      = reader.GetDateTimeOrDefault("FechaInicio"),
                FechaFin         = reader.GetDateTimeOrDefault("FechaFin"),
                Estado           = reader.GetNullableString("Estado") ?? string.Empty,
                Modalidad        = reader.GetNullableString("Modalidad") ?? string.Empty,
                MotivoConsulta   = reader.GetNullableString("MotivoConsulta"),
                FechaCreacion    = reader.GetDateTimeOrDefault("FechaCreacion")
            });
    }

    public async Task<List<CitaResponseDto>> ListarAsync(ListarCitasRequestDto filtros)
    {
        return await _executor.QueryAsync(
            "dbo.sp_Cita_Listar",
            [
                new SqlParameter("@PacienteId",  (object?)filtros.PacienteId ?? DBNull.Value),
                new SqlParameter("@MedicoId",    (object?)filtros.MedicoId ?? DBNull.Value),
                new SqlParameter("@SedeId",      (object?)filtros.SedeId ?? DBNull.Value),
                new SqlParameter("@ServicioId",  (object?)filtros.ServicioId ?? DBNull.Value),
                new SqlParameter("@Estado",      (object?)filtros.Estado ?? DBNull.Value),
                new SqlParameter("@FechaDesde",  (object?)filtros.FechaDesde ?? DBNull.Value),
                new SqlParameter("@FechaHasta",  (object?)filtros.FechaHasta ?? DBNull.Value)
            ],
            reader => new CitaResponseDto
            {
                CitaId           = reader.GetInt64OrDefault("CitaId"),
                PacienteId       = reader.GetInt32OrDefault("PacienteId"),
                NumeroExpediente = string.Empty,
                SedeId           = reader.GetInt32OrDefault("SedeId"),
                NombreSede       = string.Empty,
                ServicioId       = reader.GetInt32OrDefault("ServicioId"),
                NombreServicio   = string.Empty,
                MedicoId         = reader.GetNullableInt32("MedicoId"),
                NombreMedico     = null,
                FechaInicio      = reader.GetDateTimeOrDefault("FechaInicio"),
                FechaFin         = reader.GetDateTimeOrDefault("FechaFin"),
                Estado           = reader.GetNullableString("Estado") ?? string.Empty,
                Modalidad        = reader.GetNullableString("Modalidad") ?? string.Empty,
                MotivoConsulta   = reader.GetNullableString("MotivoConsulta"),
                FechaCreacion    = reader.GetDateTimeOrDefault("FechaCreacion")
            });
    }
}