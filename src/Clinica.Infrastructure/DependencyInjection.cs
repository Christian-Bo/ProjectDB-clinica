using Microsoft.Extensions.DependencyInjection;

namespace Clinica.Infrastructure;

// Registra todos los servicios y repositorios de infraestructura
// Llama AddInfrastructure() desde Program.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // TODO: registrar servicios y repositorios
        return services;
    }
}
