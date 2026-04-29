using System.Text.Json.Serialization;
using Clinica.API.Configuration;
using Clinica.API.Middlewares;
using Clinica.Infrastructure;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1) Carga de configuracion robusta.
//    - appsettings.json
//    - appsettings.{Environment}.json
//    - .env y .env.local (desarrollo local)
//    - variables de entorno reales (Railway, Windows, Linux)
// -----------------------------------------------------------------------------
EnvironmentBootstrapper.LoadFromDotEnv(builder.Environment.ContentRootPath);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(int.Parse(port)));

builder.Services.Configure<TicketQueueWorkerOptions>(
    builder.Configuration.GetSection(TicketQueueWorkerOptions.SectionName));

builder.Services.AddInfrastructure();

builder.Services.AddAuthorization();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clinica API",
        Version = "v1",
        Description = "API .NET 8 para ClinicaDB con enfoque BD-first y modulo 3 de Recepcion/Tickets/Pantalla publica"
    });

    options.AddSecurityDefinition("Idempotency-Key", new OpenApiSecurityScheme
    {
        Name = "Idempotency-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "GUID opcional para evitar duplicados en operaciones criticas como generar ticket y llamar siguiente."
    });
});

// -----------------------------------------------------------------------------
// 2) CORS flexible.
//    Primero intenta leer el arreglo tradicional Cors:AllowedOrigins.
//    Si no existe, usa una variable CSV llamada CORS_ALLOWED_ORIGINS.
// -----------------------------------------------------------------------------
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    var csvOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"]
        ?? builder.Configuration["Cors:AllowedOriginsCsv"];

    allowedOrigins = string.IsNullOrWhiteSpace(csvOrigins)
        ? new[] { "http://localhost:3000" }
        : csvOrigins.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClinicaPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// -----------------------------------------------------------------------------
// 3) Mensaje diagnostico de arranque.
//    Esto ayuda a detectar inmediatamente si la ConnectionString se cargo o no.
// -----------------------------------------------------------------------------
var connectionStringLoaded = DatabaseConnection.TryResolveConnectionString(builder.Configuration, out _);
Console.WriteLine(connectionStringLoaded
    ? ">> ConnectionString 'DefaultConnection' detectada correctamente."
    : ">> ADVERTENCIA: No se detecto 'DefaultConnection'. Revisa appsettings, .env o variables de entorno.");

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("ClinicaPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
