using System.Text.Json;
using Clinica.Application.Exceptions;

namespace Clinica.API.Middlewares;

/// <summary>
/// Captura todas las excepciones no controladas y las traduce a respuestas
/// HTTP con el formato ApiResponse estándar. Nunca expone stack traces en producción.
/// </summary>
public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, code, message) = ex switch
        {
            BusinessException be   => (422, be.Code ?? "REGLA_NEGOCIO",       be.Message),
            ConflictException  ce  => (409, ce.Code ?? "CONFLICTO",           ce.Message),
            NotFoundException  _   => (404, "NO_ENCONTRADO",                  ex.Message),
            ArgumentException  _   => (400, "PARAMETRO_INVALIDO",             ex.Message),
            _                      => (500, "ERROR_INTERNO",                  "Ocurrió un error interno.")
        };

        if (status == 500)
            logger.LogError(ex, "Error no controlado: {Message}", ex.Message);
        else
            logger.LogWarning("Error controlado [{Code}]: {Message}", code, ex.Message);

        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            ok      = false,
            code,
            message,
            data    = (object?)null,
        }, JsonOptions);

        await context.Response.WriteAsync(body);
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionMiddleware>();
}
