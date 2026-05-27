using System.Security.Claims;
using Clinica.Application.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
        var ruta = ResolverRuta(roles);
        var permisos = ResolverPermisos(roles);

        return Ok(ApiResponse<object>.Success(new
        {
            usuarioId = User.FindFirstValue("usuarioId"),
            username = User.FindFirstValue("username"),
            nombreCompleto = User.FindFirstValue("nombreCompleto"),
            roles,
            rutaPrincipal = ruta,
            permisos
        }));
    }

    private static string ResolverRuta(IReadOnlyCollection<string> roles)
    {
        if (Tiene(roles, "Administrador") || Tiene(roles, "Supervisor")) return "/admin";
        if (Tiene(roles, "Secretaria")) return "/secretaria";
        if (Tiene(roles, "Recepcion")) return "/recepcion";
        if (Tiene(roles, "Medico")) return "/medico";
        if (Tiene(roles, "Farmacia") || Tiene(roles, "Bodega") || Tiene(roles, "Inventario")) return "/farmacia";
        if (Tiene(roles, "Paciente")) return "/paciente";
        if (Tiene(roles, "Auditor")) return "/admin/etl";
        if (Tiene(roles, "Tecnico")) return "/medico/ordenes";
        return "/login";
    }

    private static object ResolverPermisos(IReadOnlyCollection<string> roles)
    {
        var admin = Tiene(roles, "Administrador") || Tiene(roles, "Supervisor");
        return new
        {
            admin = admin || Tiene(roles, "Auditor"),
            usuarios = admin,
            notificaciones = admin,
            recepcion = admin || Tiene(roles, "Recepcion"),
            secretaria = admin || Tiene(roles, "Secretaria"),
            medico = admin || Tiene(roles, "Medico"),
            farmacia = admin || Tiene(roles, "Farmacia") || Tiene(roles, "Bodega") || Tiene(roles, "Inventario"),
            bi = admin || Tiene(roles, "Auditor")
        };
    }

    private static bool Tiene(IEnumerable<string> roles, string rol)
        => roles.Any(r => string.Equals(Normalizar(r), Normalizar(rol), StringComparison.OrdinalIgnoreCase));

    private static string Normalizar(string value)
        => value.Trim()
            .ToLowerInvariant()
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ü", "u");
}
