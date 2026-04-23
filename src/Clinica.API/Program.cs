using System.Text.Json.Serialization;
using Clinica.API.Configuration;
using Clinica.Infrastructure;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

EnvironmentBootstrapper.LoadFromDotEnv(builder.Environment.ContentRootPath);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var portRaw = Environment.GetEnvironmentVariable("PORT") ?? "8080";
if (!int.TryParse(portRaw, out var parsedPort) || parsedPort <= 0 || parsedPort > 65535)
{
    parsedPort = 8080;
    Console.WriteLine($">> ADVERTENCIA: PORT '{portRaw}' invalido. Se usara {parsedPort}.");
}

builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(parsedPort));

builder.Services
    .AddOptions<TicketQueueWorkerOptions>()
    .Bind(builder.Configuration.GetSection(TicketQueueWorkerOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

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
        Description = "API .NET 8 para ClinicaDB con el modulo 3 de recepcion, tickets y pantalla publica."
    });

    options.AddSecurityDefinition("Idempotency-Key", new OpenApiSecurityScheme
    {
        Name = "Idempotency-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "GUID opcional para evitar duplicados en operaciones criticas."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Idempotency-Key"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

var connectionStringLoaded = DatabaseConnection.TryResolveConnectionString(builder.Configuration, out _);
Console.WriteLine(connectionStringLoaded
    ? ">> ConnectionString 'DefaultConnection' detectada correctamente."
    : ">> ADVERTENCIA: No se detecto 'DefaultConnection'. Revisa appsettings, .env o variables de entorno.");

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ClinicaPolicy");
app.UseAuthorization();

app.MapControllers();

app.Run();