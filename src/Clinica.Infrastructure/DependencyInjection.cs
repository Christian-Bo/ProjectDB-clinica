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

        // Seguridad
        services.AddScoped<PasswordHasher>();
        services.AddScoped<JwtTokenGenerator>();

        // Repositorios
        services.AddScoped<AuthRepository>();

        // Servicios
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITicketQueueService, TicketQueueService>();

        // Worker
        services.AddHostedService<TicketQueueMaintenanceWorker>();

        return services;
    }
}