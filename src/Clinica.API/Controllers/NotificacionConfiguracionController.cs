using Clinica.Application.Contracts;
using Clinica.Application.DTOs.Operativo;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[Route("api/configuracion/notificaciones")]
public sealed class NotificacionConfiguracionController(INotificacionConfiguracionService service) : BaseController
{
    [HttpGet]
    public async Task<ActionResult> Obtener(CancellationToken ct)
        => Ok(new { ok = true, code = "OK", message = "Configuracion de notificaciones obtenida.", data = await service.ObtenerAsync(ct) });

    [HttpPut]
    public async Task<ActionResult> Guardar([FromBody] GuardarNotificacionConfiguracionRequest request, CancellationToken ct)
        => ToActionResult(await service.GuardarAsync(request, ct));

    [HttpPost("probar")]
    public async Task<ActionResult> Probar([FromBody] ProbarCanalRequest request, CancellationToken ct)
        => ToActionResult(await service.ProbarAsync(request.Canal, ct));

    public sealed class ProbarCanalRequest
    {
        public string Canal { get; set; } = string.Empty;
    }
}
