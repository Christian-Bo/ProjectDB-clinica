using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/admin/usuarios")]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador,Supervisor")]
public sealed class UsuariosController : ControllerBase
{
    private readonly SqlExecutor _sql;
    private readonly PasswordHasher _hasher;

    public UsuariosController(SqlExecutor sql, PasswordHasher hasher)
    {
        _sql = sql;
        _hasher = hasher;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estado = null,
        [FromQuery] string? rol = null,
        CancellationToken ct = default)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_Usuario_Listar",
            new[]
            {
                new SqlParameter("@Estado", SqlDbType.NVarChar, 30) { Value = ToDbNull(estado) },
                new SqlParameter("@RolNombre", SqlDbType.NVarChar, 50) { Value = ToDbNull(rol) },
            },
            r =>
            {
                var usuarioId = ReadInt32(r, "UsuarioId");
                var nombreUsuario = ReadString(r, "NombreUsuario");
                var correo = ReadString(r, "CorreoElectronico");
                var nombres = ReadString(r, "Nombres");
                var apellidos = ReadString(r, "Apellidos");
                var nombreCompleto = $"{nombres} {apellidos}".Trim();
                var roles = ReadNullableString(r, "RolesActivos") ?? string.Empty;
                var estadoUsuario = ReadString(r, "Estado");

                return new
                {
                    usuarioId,

                    // Nombres reales de la BD / backend
                    nombreUsuario,
                    correoElectronico = correo,
                    nombres,
                    apellidos,
                    telefono = ReadNullableString(r, "Telefono"),
                    estado = estadoUsuario,
                    rolesActivos = roles,
                    fechaCreacion = ReadDateTime(r, "FechaCreacion"),
                    fechaUltimoAcceso = ReadNullableDateTime(r, "FechaUltimoAcceso"),

                    // Alias usados por la vista actual del frontend admin
                    username = nombreUsuario,
                    nombreCompleto,
                    email = correo,
                    roles
                };
            },
            ct);

        return Ok(new { ok = true, success = true, code = "OK", message = "Usuarios cargados correctamente.", data = rows });
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoRequest req, CancellationToken ct = default)
    {
        var estadoNormalizado = ResolveEstado(req);

        if (string.IsNullOrWhiteSpace(estadoNormalizado))
            return BadRequest(new { ok = false, success = false, code = "ESTADO_REQUERIDO", message = "Debe enviar 'estado' o 'activo'." });

        var rows = await _sql.QueryAsync(
            "dbo.sp_Usuario_CambiarEstado",
            new[]
            {
                new SqlParameter("@UsuarioId", SqlDbType.Int) { Value = id },
                new SqlParameter("@Estado", SqlDbType.NVarChar, 30) { Value = estadoNormalizado },
                new SqlParameter("@ModificadoPor", SqlDbType.Int) { Value = DBNull.Value },
            },
            r => new SpResponse(
                ReadInt32(r, "HttpStatus", 200),
                ReadString(r, "Codigo", "OK"),
                ReadString(r, "Mensaje", "Operación ejecutada correctamente.")
            ),
            ct);

        return FromSpResponse(rows.FirstOrDefault());
    }

    [HttpPatch("{id:int}/password")]
    public async Task<IActionResult> RestablecerPassword(int id, [FromBody] RestablecerPasswordRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.NuevaPassword) || req.NuevaPassword.Length < 8)
            return BadRequest(new { ok = false, success = false, code = "PASSWORD_INVALIDA", message = "La nueva contraseña debe tener al menos 8 caracteres." });

        var passwordHash = _hasher.Hash(req.NuevaPassword);
        var salt = _hasher.GenerateSalt();

        var rows = await _sql.QueryAsync(
            "dbo.sp_Usuario_RestablecerPassword",
            new[]
            {
                new SqlParameter("@UsuarioId", SqlDbType.Int) { Value = id },
                new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500) { Value = passwordHash },
                new SqlParameter("@Salt", SqlDbType.NVarChar, 100) { Value = salt },
                new SqlParameter("@RequiereCambioPassword", SqlDbType.Bit) { Value = req.RequiereCambio },
                new SqlParameter("@ModificadoPor", SqlDbType.Int) { Value = DBNull.Value },
            },
            r => new SpResponse(
                ReadInt32(r, "HttpStatus", 200),
                ReadString(r, "Codigo", "OK"),
                ReadString(r, "Mensaje", "Operación ejecutada correctamente.")
            ),
            ct);

        return FromSpResponse(rows.FirstOrDefault());
    }

    [HttpPost("{id:int}/roles")]
    public async Task<IActionResult> AsignarRol(int id, [FromBody] RolRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Rol))
            return BadRequest(new { ok = false, success = false, code = "ROL_REQUERIDO", message = "El rol es obligatorio." });

        var rows = await _sql.QueryAsync(
            "dbo.sp_Usuario_AsignarRol",
            new[]
            {
                new SqlParameter("@UsuarioId", SqlDbType.Int) { Value = id },
                new SqlParameter("@RolNombre", SqlDbType.NVarChar, 50) { Value = req.Rol.Trim() },
                new SqlParameter("@AsignadoPor", SqlDbType.Int) { Value = DBNull.Value },
            },
            r => new SpResponse(
                ReadInt32(r, "HttpStatus", 200),
                ReadString(r, "Codigo", "OK"),
                ReadString(r, "Mensaje", "Rol asignado correctamente.")
            ),
            ct);

        return FromSpResponse(rows.FirstOrDefault());
    }

    [HttpDelete("{id:int}/roles/{rol}")]
    public async Task<IActionResult> RevocarRol(int id, string rol, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rol))
            return BadRequest(new { ok = false, success = false, code = "ROL_REQUERIDO", message = "El rol es obligatorio." });

        var rows = await _sql.QueryAsync(
            "dbo.sp_Usuario_RevocarRol",
            new[]
            {
                new SqlParameter("@UsuarioId", SqlDbType.Int) { Value = id },
                new SqlParameter("@RolNombre", SqlDbType.NVarChar, 50) { Value = rol.Trim() },
                new SqlParameter("@RevocadoPor", SqlDbType.Int) { Value = DBNull.Value },
            },
            r => new SpResponse(
                ReadInt32(r, "HttpStatus", 200),
                ReadString(r, "Codigo", "OK"),
                ReadString(r, "Mensaje", "Rol revocado correctamente.")
            ),
            ct);

        return FromSpResponse(rows.FirstOrDefault());
    }

    private static string? ResolveEstado(CambiarEstadoRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.Estado))
            return req.Estado.Trim().ToUpperInvariant();

        if (req.Activo.HasValue)
            return req.Activo.Value ? "ACTIVO" : "INACTIVO";

        return null;
    }

    private IActionResult FromSpResponse(SpResponse? result)
    {
        if (result is null)
            return StatusCode(500, new { ok = false, success = false, code = "SIN_RESPUESTA", message = "El procedimiento no devolvió respuesta." });

        var body = new { ok = result.HttpStatus < 400, success = result.HttpStatus < 400, code = result.Codigo, message = result.Mensaje };

        return result.HttpStatus switch
        {
            200 => Ok(body),
            201 => StatusCode(201, body),
            400 => BadRequest(body),
            401 => Unauthorized(body),
            403 => StatusCode(403, body),
            404 => NotFound(body),
            409 => Conflict(body),
            422 => UnprocessableEntity(body),
            _ when result.HttpStatus >= 400 => StatusCode(result.HttpStatus, body),
            _ => Ok(body)
        };
    }

    private static object ToDbNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static int Ordinal(SqlDataReader reader, string columnName)
        => reader.GetOrdinal(columnName);

    private static string ReadString(SqlDataReader reader, string columnName, string defaultValue = "")
    {
        if (!HasColumn(reader, columnName))
            return defaultValue;

        var ordinal = Ordinal(reader, columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : Convert.ToString(reader.GetValue(ordinal)) ?? defaultValue;
    }

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        if (!HasColumn(reader, columnName))
            return null;

        var ordinal = Ordinal(reader, columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal));
    }

    private static int ReadInt32(SqlDataReader reader, string columnName, int defaultValue = 0)
    {
        if (!HasColumn(reader, columnName))
            return defaultValue;

        var ordinal = Ordinal(reader, columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static DateTime ReadDateTime(SqlDataReader reader, string columnName)
    {
        if (!HasColumn(reader, columnName))
            return default;

        var ordinal = Ordinal(reader, columnName);
        return reader.IsDBNull(ordinal) ? default : Convert.ToDateTime(reader.GetValue(ordinal));
    }

    private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
    {
        if (!HasColumn(reader, columnName))
            return null;

        var ordinal = Ordinal(reader, columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));
    }

    private sealed record SpResponse(int HttpStatus, string Codigo, string Mensaje);
}

public sealed class CambiarEstadoRequest
{
    public string? Estado { get; init; }
    public bool? Activo { get; init; }
}

public sealed record RolRequest(string Rol);
public sealed record RestablecerPasswordRequest(string NuevaPassword, bool RequiereCambio = true);
