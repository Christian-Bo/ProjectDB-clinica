using Clinica.Application.Contracts;
using Clinica.Infrastructure.Database;
using Clinica.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clinica.Infrastructure.HostedServices;

// -----------------------------------------------------------------------------
// Worker del modulo 3.
//
// Mejora importante:
// - Si la cadena de conexion aun no esta configurada, no lanza una excepcion que
//   ensucie el arranque de la API. Solo registra advertencia y vuelve a intentar
//   en el siguiente ciclo.
// -----------------------------------------------------------------------------
public sealed class TicketQueueMaintenanceWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TicketQueueMaintenanceWorker> _logger;
    private readonly TicketQueueWorkerOptions _options;
    private bool _missingConnectionStringWarningShown;

    public TicketQueueMaintenanceWorker(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        IOptions<TicketQueueWorkerOptions> options,
        ILogger<TicketQueueMaintenanceWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ProcessNoShowEnabled)
        {
            _logger.LogInformation("TicketQueueMaintenanceWorker deshabilitado por configuracion.");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Max(15, _options.NoShowIntervalSeconds));
        _logger.LogInformation("TicketQueueMaintenanceWorker iniciado. Intervalo NO_SHOW: {DelaySeconds}s", delay.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!DatabaseConnection.TryResolveConnectionString(_configuration, out _))
                {
                    if (!_missingConnectionStringWarningShown)
                    {
                        _logger.LogWarning(
                            "TicketQueueMaintenanceWorker omitido porque 'DefaultConnection' no esta configurada todavia. " +
                            "Revisa appsettings, .env o variables de entorno.");
                        _missingConnectionStringWarningShown = true;
                    }
                }
                else
                {
                    _missingConnectionStringWarningShown = false;

                    using var scope = _serviceScopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITicketQueueService>();
                    var result = await service.ProcessNoShowAsync(stoppingToken);

                    if (result.Success && result.Data is not null && result.Data.RegistrosProcesados > 0)
                    {
                        _logger.LogInformation("Worker NO_SHOW proceso {Count} ticket(s).", result.Data.RegistrosProcesados);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en TicketQueueMaintenanceWorker al procesar NO_SHOW.");
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
