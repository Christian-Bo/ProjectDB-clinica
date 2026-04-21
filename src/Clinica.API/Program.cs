using Clinica.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(int.Parse(port)));

builder.Services.AddInfrastructure();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clinica API",
        Version = "v1",
        Description = "API base de Clinica conectada a SQL Server mediante Stored Procedures"
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
        ? ["http://localhost:3000"]
        : csvOrigins
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ClinicaPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
