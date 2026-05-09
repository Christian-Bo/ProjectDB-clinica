using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Clinica.Infrastructure.Database;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/admin/usuarios")]
[Authorize(Roles = "Administrador,Supervisor")]
public sealed class UsuariosController : ControllerBase
{
    private readonly SqlExecutor _sql;

    public UsuariosController(SqlExecutor sql)
    {
        _sql = sql;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estado = null,
        [FromQuery] string? rol = null)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_Usuario_Listar",
            new[]
            {
                new SqlParameter("@Estado",    (object?)estado ?? DBNull.Value),
                new SqlParameter("@RolNombre", (object?)rol    ?? DBNull.Value),
            },
            r => new
            {
                usuarioId         = r.GetInt32("UsuarioId"),
                nombreUsuario     = r.GetString("NombreUsuario"),
                correoElectronico = r.GetString("CorreoElectronico"),
                nombres           = r.GetString("Nombres"),
                apellidos         = r.GetString("Apellidos"),
                telefono          = r.IsDBNull("Telefono") ? null : r.GetString("Telefono"),
                estado            = r.GetString("Estado"),
                rolesActivos      = r.IsDBNull("RolesActivos") ? null : r.GetString("RolesActivos"),
                fechaCreacion     = r.GetDateTime("FechaCreacion"),
            });

        return Ok(new { success = true, data = rows });
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoRequest req)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_Usuario_CambiarEstado",
            new[]
            {
                new SqlParameter("@UsuarioId", id),
                new SqlParameter("@Estado",    req.Estado),
            },
            r => new
            {
                httpStatus = r.GetInt32("HttpStatus"),
                codigo     = r.GetString("Codigo"),
                mensaje    = r.GetString("Mensaje"),
            });

        var result = rows.FirstOrDefault();
        if (result?.httpStatus == 404)
            return NotFound(new { success = false, message = result.mensaje });

        return Ok(new { success = true, message = result?.mensaje });
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AsignarRol(int id, [FromBody] RolRequest req)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_Usuario_AsignarRol",
            new[]
            {
                new SqlParameter("@UsuarioId",   id),
                new SqlParameter("@RolNombre",   req.Rol),
                new SqlParameter("@AsignadoPor", DBNull.Value),
            },
            r => new { codigo = r.GetString("Codigo"), mensaje = r.GetString("Mensaje") });

        var result = rows.FirstOrDefault();
        return Ok(new { success = true, message = result?.mensaje });
    }

    [HttpDelete("{id}/roles/{rol}")]
    public async Task<IActionResult> RevocarRol(int id, string rol)
    {
        var rows = await _sql.QueryAsync(
            "dbo.sp_Usuario_RevocarRol",
            new[]
            {
                new SqlParameter("@UsuarioId",  id),
                new SqlParameter("@RolNombre",  rol),
                new SqlParameter("@RevocadoPor", DBNull.Value),
            },
            r => new { codigo = r.GetString("Codigo"), mensaje = r.GetString("Mensaje") });

        var result = rows.FirstOrDefault();
        return Ok(new { success = true, message = result?.mensaje });
    }
}

public sealed record CambiarEstadoRequest(string Estado);
public sealed record RolRequest(string Rol);