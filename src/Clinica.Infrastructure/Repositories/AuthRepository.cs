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
        cmd.Parameters.AddWithValue("@UsuarioId",          DBNull.Value);
        cmd.Parameters.AddWithValue("@NombreUsuario",      username);
        cmd.Parameters.AddWithValue("@CorreoElectronico",  DBNull.Value);

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
}