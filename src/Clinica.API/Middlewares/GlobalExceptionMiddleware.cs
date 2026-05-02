using System.Text.Json;
using Clinica.Application.Exceptions;

namespace Clinica.API.Middlewares;

// -----------------------------------------------------------------------------
// Atrapa todas las excepciones no controladas y las convierte en JSON uniforme.
// Nunca debe llegar un error tecnico crudo al frontend.
// -----------------------------------------------------------------------------
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, codigo, mensaje) = exception switch
        {
            BusinessException bex  => (422, bex.Codigo, bex.Message),
            ConflictException cex  => (409, cex.Codigo, cex.Message),
            NotFoundException nex  => (404, "NOT_FOUND", nex.Message),
            UnauthorizedAccessException => (401, "NO_AUTORIZADO", "No autorizado."),
            _ => (500, "ERROR_INTERNO", "Ocurrio un error interno.")
        };

        if (statusCode == 500)
            _logger.LogError(exception, "Error no controlado");

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            success = false,
            errorCode = codigo,
            message = mensaje,
            meta = new
            {
                requestId = context.TraceIdentifier,
                timestamp = DateTime.UtcNow
            }
        });

        await context.Response.WriteAsync(body);
    }
}