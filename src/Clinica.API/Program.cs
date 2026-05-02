using System.Text.Json.Serialization;
using Clinica.API.Configuration;
using Clinica.API.Middlewares;
using Clinica.Infrastructure;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.Options;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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

// JWT — Dev1
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

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
        Title       = "Clinica API",
        Version     = "v1",
        Description = "API .NET 8 para ClinicaDB con enfoque BD-first y modulo 3 de Recepcion/Tickets/Pantalla publica"
    });

    options.AddSecurityDefinition("Idempotency-Key", new OpenApiSecurityScheme
    {
        Name        = "Idempotency-Key",
        Type        = SecuritySchemeType.ApiKey,
        In          = ParameterLocation.Header,
        Description = "GUID opcional para evitar duplicados en operaciones criticas."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Ingresa el token JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
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
    : ">> ADVERTENCIA: No se detecto 'DefaultConnection'.");

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("ClinicaPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();