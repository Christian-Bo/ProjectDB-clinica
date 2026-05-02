using System.Data;
using Clinica.Domain.Models;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;

namespace Clinica.Infrastructure.Repositories;

public sealed class AuthRepository
{
    private readonly DatabaseConnection _db;

    public AuthRepository(DatabaseConnection db)
    {
        _db = db;
    }

    public async Task<UsuarioAuthModel?> ObtenerPorUsernameAsync(string username)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_Usuario_Obtener";

        // Si contiene @ es correo, si no es username
        var esCorreo = username.Contains('@');
        cmd.Parameters.AddWithValue("@UsuarioId", DBNull.Value);
        cmd.Parameters.AddWithValue("@NombreUsuario", esCorreo ? DBNull.Value : (object)username);
        cmd.Parameters.AddWithValue("@CorreoElectronico", esCorreo ? (object)username : DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new UsuarioAuthModel
        {
            UsuarioId              = reader.GetInt32("UsuarioId"),
            NombreUsuario          = reader.GetString("NombreUsuario"),
            CorreoElectronico      = reader.GetString("CorreoElectronico"),
            PasswordHash           = reader.GetString("PasswordHash"),
            Nombres                = reader.GetString("Nombres"),
            Apellidos              = reader.GetString("Apellidos"),
            Estado                 = reader.GetString("Estado"),
            RequiereCambioPassword = reader.GetBoolean("RequiereCambioPassword"),
            RolesActivos           = reader.IsDBNull("RolesActivos")
                                    ? null : reader.GetString("RolesActivos")
        };
    }
    public async Task RegistrarSesionAsync(int usuarioId, string token, int minutesExpiry)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = @"
            INSERT INTO dbo.SesionesUsuario
                (UsuarioId, Token, FechaExpiracion, Estado)
            VALUES
                (@uid, @tok,
                 DATEADD(MINUTE, @min, SYSUTCDATETIME()),
                 'ACTIVA')";
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        cmd.Parameters.AddWithValue("@tok", token);
        cmd.Parameters.AddWithValue("@min", minutesExpiry);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RegistrarIntentoFallidoAsync(string correo, string? ip)
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = @"
            INSERT INTO dbo.IntentosAcceso
                (CorreoElectronico, IPOrigen, Exitoso, MotivoFallo)
            VALUES
                (@correo, @ip, 0, 'CREDENCIALES_INVALIDAS')";
        cmd.Parameters.AddWithValue("@correo", correo);
        cmd.Parameters.AddWithValue("@ip", (object?)ip ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(bool Success, string ErrorCode, string Message, int UsuarioId, int PacienteId)>
        RegistrarPacienteAsync(
            string nombres, string apellidos, string correo,
            string passwordHash, string salt, string? telefono,
            string tipoDocumento, string numeroDocumento,
            DateTime fechaNacimiento, string genero,
            string nacionalidad, string? tipoSangre)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand("dbo.sp_Registro_Paciente", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Nombres", nombres);
        command.Parameters.AddWithValue("@Apellidos", apellidos);
        command.Parameters.AddWithValue("@CorreoElectronico", correo);
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        command.Parameters.AddWithValue("@Salt", salt);
        command.Parameters.AddWithValue("@Telefono", (object?)telefono ?? DBNull.Value);
        command.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);
        command.Parameters.AddWithValue("@NumeroDocumento", numeroDocumento);
        command.Parameters.AddWithValue("@FechaNacimiento", fechaNacimiento);
        command.Parameters.AddWithValue("@Genero", genero);
        command.Parameters.AddWithValue("@Nacionalidad", nacionalidad);
        command.Parameters.AddWithValue("@TipoSangre", (object?)tipoSangre ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var httpStatus = reader.GetInt32(reader.GetOrdinal("HttpStatus"));
            var codigo = reader.GetString(reader.GetOrdinal("Codigo"));
            var mensaje = reader.GetString(reader.GetOrdinal("Mensaje"));

            if (httpStatus == 201)
            {
                var usuarioId = reader.GetInt32(reader.GetOrdinal("UsuarioId"));
                var pacienteId = reader.GetInt32(reader.GetOrdinal("PacienteId"));
                return (true, codigo, mensaje, usuarioId, pacienteId);
            }

            return (false, codigo, mensaje, 0, 0);
        }

        return (false, "ERROR", "No se obtuvo respuesta del SP.", 0, 0);
    }
}