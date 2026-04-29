using Clinica.Application.DTOs.Pacientes;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;

namespace Clinica.Infrastructure.Repositories;

public sealed class PacientesRepository
{
    private readonly SqlExecutor _executor;

    public PacientesRepository(SqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<PacienteResponseDto?> ObtenerAsync(int pacienteId)
    {
        return await _executor.QuerySingleAsync(
            "dbo.sp_Paciente_Obtener",
            [new SqlParameter("@PacienteId", pacienteId)],
            reader => new PacienteResponseDto
            {
                PacienteId                 = reader.GetInt32OrDefault("PacienteId"),
                UsuarioId                  = reader.GetNullableInt32("UsuarioId"),
                NumeroExpediente           = reader.GetNullableString("NumeroExpediente") ?? string.Empty,
                TipoDocumento              = reader.GetNullableString("TipoDocumento") ?? string.Empty,
                NumeroDocumento            = reader.GetNullableString("NumeroDocumento") ?? string.Empty,
                FechaNacimiento            = reader.GetDateTimeOrDefault("FechaNacimiento"),
                Genero                     = reader.GetNullableString("Genero") ?? string.Empty,
                Ocupacion                  = reader.GetNullableString("Ocupacion"),
                Nacionalidad               = reader.GetNullableString("Nacionalidad") ?? string.Empty,
                DireccionResidencia        = reader.GetNullableString("DireccionResidencia"),
                MunicipioId                = reader.GetNullableInt32("MunicipioId"),
                TipoSangre                 = reader.GetNullableString("TipoSangre"),
                NotasMedicas               = reader.GetNullableString("NotasMedicas"),
                EsDiscapacitado            = reader.GetBooleanOrDefault("EsDiscapacitado"),
                ContactoEmergenciaNombre   = reader.GetNullableString("ContactoEmergenciaNombre"),
                ContactoEmergenciaTelefono = reader.GetNullableString("ContactoEmergenciaTelefono"),
                ContactoEmergenciaRelacion = reader.GetNullableString("ContactoEmergenciaRelacion"),
                Estado                     = reader.GetNullableString("Estado") ?? string.Empty,
                FechaRegistro              = reader.GetDateTimeOrDefault("FechaRegistro"),
                FechaModificacion          = reader.GetNullableDateTime("FechaModificacion")
            });
    }

    public async Task<SpResult> UpsertAsync(PacienteUpsertDto dto)
    {
        return await _executor.ExecuteAsync(
            "dbo.sp_Paciente_Upsert",
            [
                new SqlParameter("@PacienteId",                  (object?)dto.PacienteId ?? DBNull.Value),
                new SqlParameter("@UsuarioId",                   (object?)dto.UsuarioId ?? DBNull.Value),
                new SqlParameter("@TipoDocumento",               dto.TipoDocumento),
                new SqlParameter("@NumeroDocumento",             dto.NumeroDocumento),
                new SqlParameter("@FechaNacimiento",             dto.FechaNacimiento),
                new SqlParameter("@Genero",                      dto.Genero),
                new SqlParameter("@Ocupacion",                   (object?)dto.Ocupacion ?? DBNull.Value),
                new SqlParameter("@Nacionalidad",                dto.Nacionalidad),
                new SqlParameter("@DireccionResidencia",         (object?)dto.DireccionResidencia ?? DBNull.Value),
                new SqlParameter("@MunicipioId",                 (object?)dto.MunicipioId ?? DBNull.Value),
                new SqlParameter("@ContactoEmergenciaNombre",    (object?)dto.ContactoEmergenciaNombre ?? DBNull.Value),
                new SqlParameter("@ContactoEmergenciaTelefono",  (object?)dto.ContactoEmergenciaTelefono ?? DBNull.Value),
                new SqlParameter("@ContactoEmergenciaRelacion",  (object?)dto.ContactoEmergenciaRelacion ?? DBNull.Value),
                new SqlParameter("@TipoSangre",                  (object?)dto.TipoSangre ?? DBNull.Value),
                new SqlParameter("@NotasMedicas",                (object?)dto.NotasMedicas ?? DBNull.Value),
                new SqlParameter("@EsDiscapacitado",             dto.EsDiscapacitado)
            ]);
    }

    public async Task<List<AlergiaResponseDto>> ListarAlergiasAsync(int pacienteId)
    {
        return await _executor.QueryAsync(
            "dbo.sp_Paciente_ListarAlergias",
            [new SqlParameter("@PacienteId", pacienteId)],
            reader => new AlergiaResponseDto
            {
                PacienteAlergiaId = reader.GetInt32OrDefault("PacienteAlergiaId"),
                PacienteId        = reader.GetInt32OrDefault("PacienteId"),
                AlergiaId         = reader.GetInt32OrDefault("AlergiaId"),
                Severidad         = reader.GetNullableString("Severidad") ?? string.Empty,
                Descripcion       = reader.GetNullableString("Descripcion"),
                FechaDeteccion    = reader.GetNullableDateTime("FechaDeteccion"),
                FechaRegistro     = reader.GetDateTimeOrDefault("FechaRegistro")
            });
    }

    public async Task<SpResult> AgregarAlergiaAsync(int pacienteId, AlergiaRequestDto dto)
    {
        return await _executor.ExecuteAsync(
            "dbo.sp_Paciente_AgregarAlergia",
            [
                new SqlParameter("@PacienteId",     pacienteId),
                new SqlParameter("@AlergiaId",      dto.AlergiaId),
                new SqlParameter("@Severidad",      dto.Severidad),
                new SqlParameter("@Descripcion",    (object?)dto.Descripcion ?? DBNull.Value),
                new SqlParameter("@FechaDeteccion", (object?)dto.FechaDeteccion ?? DBNull.Value)
            ]);
    }

    public async Task<SpResult> QuitarAlergiaAsync(int pacienteId, int alergiaId)
    {
        return await _executor.ExecuteAsync(
            "dbo.sp_Paciente_QuitarAlergia",
            [
                new SqlParameter("@PacienteId", pacienteId),
                new SqlParameter("@AlergiaId",  alergiaId)
            ]);
    }
}