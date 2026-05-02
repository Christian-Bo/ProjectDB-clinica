using Clinica.Application.Contracts;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.Repositories;
using Clinica.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Clinica.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra todos los servicios de infraestructura.
    /// Llamar desde Program.cs: builder.Services.AddInfrastructure();
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Core de acceso a datos.
        services.AddSingleton<DatabaseConnection>();
        services.AddSingleton<SqlExecutor>();

        // Repositorios.
        services.AddScoped<TicketsRepository>();
        services.AddScoped<PantallaRepository>();
        services.AddScoped<CatalogosRepository>();
        services.AddScoped<CitasRepository>();
        services.AddScoped<PacientesRepository>();

        // Servicios de aplicación.
        services.AddScoped<IAuthService,                 AuthService>();
        services.AddScoped<IDatabaseHealthService,       DatabaseHealthService>();
        services.AddScoped<ITicketQueueService,          TicketQueueService>();
        services.AddScoped<ITicketsService,              TicketsService>();
        services.AddScoped<IPantallaService,             PantallaService>();
        services.AddScoped<ICatalogosRecepcionService,   CatalogosRecepcionService>();
        services.AddScoped<ICitasService,                CitasService>();
        services.AddScoped<IPacientesService,            PacientesService>();
        services.AddScoped<IConsultasService,            ConsultasService>();
        services.AddScoped<IOrdenesService,              OrdenesService>();
        services.AddScoped<IRecetasService,              RecetasService>();

        return services;
    }
}
