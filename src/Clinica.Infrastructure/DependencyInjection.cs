using Clinica.Application.Contracts;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.Repositories;
using Clinica.Infrastructure.Security;
using Clinica.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Clinica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Core de acceso a datos
        services.AddSingleton<DatabaseConnection>();
        services.AddSingleton<SqlExecutor>();

        // Seguridad — Dev1
        services.AddScoped<PasswordHasher>();
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<AuthRepository>();

        // Repositorios
        services.AddScoped<TicketsRepository>();
        services.AddScoped<PantallaRepository>();
        services.AddScoped<CatalogosRepository>();
        services.AddScoped<CitasRepository>();
        services.AddScoped<PacientesRepository>();

        // Servicios
        services.AddScoped<IAuthService,               AuthService>();
        services.AddScoped<IDatabaseHealthService,     DatabaseHealthService>();
        services.AddScoped<ITicketQueueService,        TicketQueueService>();
        services.AddScoped<ITicketsService,            TicketsService>();
        services.AddScoped<IPantallaService,           PantallaService>();
        services.AddScoped<ICatalogosRecepcionService, CatalogosRecepcionService>();
        services.AddScoped<ICitasService,              CitasService>();
        services.AddScoped<IPacientesService,          PacientesService>();
        services.AddScoped<IConsultasService,          ConsultasService>();
        services.AddScoped<IOrdenesService,            OrdenesService>();
        services.AddScoped<IRecetasService,            RecetasService>();

        return services;
    }
}