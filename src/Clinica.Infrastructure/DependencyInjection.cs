using Clinica.Application.Contracts;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.HostedServices;
using Clinica.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;



namespace Clinica.Infrastructure;

// -----------------------------------------------------------------------------
// Registro centralizado de infraestructura.
// Llama AddInfrastructure() desde Program.cs
// -----------------------------------------------------------------------------
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<DatabaseConnection>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITicketQueueService, TicketQueueService>();

        // Worker ligero para suplir la ausencia de SQL Agent en Railway/Somee.
        services.AddHostedService<TicketQueueMaintenanceWorker>();
         // ── Módulo 5 · Farmacia ───────────────────────────────────────────────
        services.AddScoped<IFarmaciaService, FarmaciaService>();

        // ── Módulo 5 · Inventario ─────────────────────────────────────────────
        services.AddScoped<IInventarioService, InventarioService>();

        // ── Módulo 5 · Compras ────────────────────────────────────────────────
        services.AddScoped<IProveedoresService, ProveedoresService>();
        services.AddScoped<IOrdenesCompraService, OrdenesCompraService>();

        // ── Módulo 5 · Cobros ─────────────────────────────────────────────────
        services.AddScoped<ICuentasService, CuentasService>();
        services.AddScoped<IPagosService, PagosService>();

        // ── Módulo 5 · Notificaciones ─────────────────────────────────────────
        services.AddScoped<IPlantillasNotificacionService, PlantillasNotificacionService>();
        services.AddScoped<IColaNotificacionesService, ColaNotificacionesService>();

        // ── Módulo 5 · Telemedicina ───────────────────────────────────────────
        services.AddScoped<ISesionesTelemedicinaService, SesionesTelemedicinaService>();

        // ── Módulo 5 · Reportes / ETL ─────────────────────────────────────────
        services.AddScoped<IReportesEtlService, ReportesEtlService>();

        return services;
    }
}