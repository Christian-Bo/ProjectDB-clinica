using Clinica.Application.Contracts;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.HostedServices;
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
        services.AddScoped<DatabaseConnection>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<IAuthService, AuthService>();

        // Módulo 3 — Recepción / Tickets / Cola
        services.AddScoped<ITicketQueueService, TicketQueueService>();

        // Módulo 4 — Consulta Médica, Historia Clínica, Recetas y Órdenes
        services.AddScoped<IConsultasService, ConsultasService>();
        services.AddScoped<IRecetasService, RecetasService>();
        services.AddScoped<IOrdenesService, OrdenesService>();

        // Worker ligero para suplir la ausencia de SQL Agent en Railway/Somee.
        services.AddHostedService<TicketQueueMaintenanceWorker>();

        return services;
    }
}
