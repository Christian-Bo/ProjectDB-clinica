using Clinica.Application.Contracts;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.HostedServices;
using Clinica.Infrastructure.Repositories;
using Clinica.Infrastructure.Security;
using Clinica.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Clinica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Base de datos
        services.AddScoped<DatabaseConnection>();
        services.AddScoped<SqlExecutor>();

        // Seguridad — Dev1
        services.AddScoped<PasswordHasher>();
        services.AddScoped<JwtTokenGenerator>();

        // Repositorios — Dev1
        services.AddScoped<AuthRepository>();

        // Repositorios — Dev2
        services.AddScoped<PacientesRepository>();
        services.AddScoped<CitasRepository>();

        // Servicios — Dev2
        services.AddScoped<IPacientesService, PacientesService>();
        services.AddScoped<ICitasService, CitasService>();

        // Servicios base
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<IAuthService, AuthService>();

        // Modulo 3 — Recepcion / Tickets / Cola
        services.AddScoped<ITicketQueueService, TicketQueueService>();

        // Modulo 4 — Consulta Medica, Historia Clinica, Recetas y Ordenes
        services.AddScoped<IConsultasService, ConsultasService>();
        services.AddScoped<IRecetasService, RecetasService>();
        services.AddScoped<IOrdenesService, OrdenesService>();

        // Worker
        services.AddHostedService<TicketQueueMaintenanceWorker>();

        return services;
    }
}