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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
        services.AddScoped<ITicketQueueService, TicketQueueService>();
        services.AddScoped<ITicketsService, TicketsService>();
        services.AddScoped<IPantallaService, PantallaService>();
        services.AddScoped<ICatalogosRecepcionService, CatalogosRecepcionService>();
        services.AddScoped<ICitasService, CitasService>();
        services.AddScoped<IPacientesService, PacientesService>();
        services.AddScoped<IConsultasService, ConsultasService>();
        services.AddScoped<IOrdenesService, OrdenesService>();
        services.AddScoped<IRecetasService, RecetasService>();

        // ── Flujo BD2: ventanillas, secretarias, cola médica y configuración ─────
        services.AddScoped<IOperativoCatalogosService, OperativoCatalogosService>();
        services.AddScoped<ISecretariaService, SecretariaService>();
        services.AddScoped<IMedicoColaService, MedicoColaService>();
        services.AddScoped<INotificacionConfiguracionService, NotificacionConfiguracionService>();

        return services;
    }
}