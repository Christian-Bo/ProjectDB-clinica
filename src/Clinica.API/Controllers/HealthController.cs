using Clinica.Application.DTOs.Common;
using Clinica.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Clinica.API.Controllers;

[ApiController]
[Route("api/health")]
[Produces("application/json")]
public sealed class HealthController(SqlExecutor db, IConfiguration config) : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() =>
        Ok(ApiResponse<object>.Success(new { message = "pong", utc = DateTime.UtcNow }));

    [HttpGet("db")]
    public async Task<IActionResult> Db(CancellationToken ct)
    {
        try
        {
            var dt = await db.ExecuteSpFirstTableAsync("dbo.sp_Sede_Listar", null, ct);
            return Ok(ApiResponse<object>.Success(new
            {
                message    = "Conexión OK",
                sedesFound = dt.Rows.Count,
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(503, ApiResponse<object>.Fail($"BD no alcanzable: {ex.Message}", "DB_ERROR"));
        }
    }
}
