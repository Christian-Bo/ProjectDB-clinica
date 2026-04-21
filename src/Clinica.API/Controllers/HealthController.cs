using Clinica.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/health")]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    private readonly IDatabaseHealthService _databaseHealthService;

    public HealthController(IDatabaseHealthService databaseHealthService)
    {
        _databaseHealthService = databaseHealthService;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            ok = true,
            message = "API operativa"
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        var result = await _databaseHealthService.CheckAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                ok = false,
                message = "No fue posible abrir conexion con SQL Server.",
                error = result.ErrorMessage
            });
        }

        return Ok(new
        {
            ok = true,
            message = "Conexion a SQL Server exitosa.",
            database = result.DatabaseName,
            serverUtcNow = result.ServerUtcNow,
            environment = result.EnvironmentName,
            dataSource = result.DataSource
        });
    }
}
