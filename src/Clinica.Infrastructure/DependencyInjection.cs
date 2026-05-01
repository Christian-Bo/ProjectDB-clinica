using Clinica.Application.Contracts;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.HostedServices;
using Clinica.Infrastructure.Repositories;
using Clinica.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Clinica.Infrastructure;

// -----------------------------------------------------------------------------
// Registro centralizado de infraestructura.
// Llama AddInfrastructure() desde Program.cs.
// -----------------------------------------------------------------------------
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Base de datos
        services.AddScoped<DatabaseConnection>();
        services.AddScoped<SqlExecutor>();

        // Repositorios — Dev2
        services.AddScoped<PacientesRepository>();
        services.AddScoped<CitasRepository>();

        // Servicios — Dev2
        services.AddScoped<IPacientesService, PacientesService>();
        services.AddScoped<ICitasService, CitasService>();

        // Servicios existentes
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<IAuthService, AuthService>();

        // Módulo 3 — Recepción / Tickets / Cola
        services.AddScoped<ITicketQueueService, TicketQueueService>();

        // Módulo 4 — Consulta Médica, Historia Clínica, Recetas y Órdenes
        services.AddScoped<IConsultasService, ConsultasService>();
        services.AddScoped<IRecetasService, RecetasService>();
        services.AddScoped<IOrdenesService, OrdenesService>();

        // Worker
        services.AddHostedService<TicketQueueMaintenanceWorker>();

        return services;
    }
}