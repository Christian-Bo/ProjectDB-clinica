using Clinica.API.Middlewares;
using Clinica.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Servicios ────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title   = "Clínica Integral API — Módulo 3",
        Version = "v1",
        Description = "Recepción, Tickets y Pantalla Pública",
    });
});

// Módulo 3: inyección de infraestructura (repositorios y servicios)
builder.Services.AddInfrastructure();

// CORS — permite el origen del frontend (configurado en variable de entorno)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["*"];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Idempotency-Key");
    });
});

var app = builder.Build();

// ── Middlewares ──────────────────────────────────────────────────────────────
app.UseGlobalExceptionHandler(); // siempre primero

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

app.Run();
