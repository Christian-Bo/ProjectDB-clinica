using Clinica.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[Route("api/health")]
public sealed class HealthController : BaseController
{
    private readonly IDatabaseHealthService _databaseHealthService;

    public HealthController(IDatabaseHealthService databaseHealthService)
    {
        _databaseHealthService = databaseHealthService;
    }

    [AllowAnonymous]
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            ok = true,
            message = "API operativa"
        });
    }

    [AllowAnonymous]
    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        var result = await _databaseHealthService.CheckAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ok = false,
                message = "No fue posible verificar conectividad con la base de datos."
            });
        }

        return Ok(new
        {
            ok = true,
            message = "Conexion a SQL Server exitosa."
        });
    }
}