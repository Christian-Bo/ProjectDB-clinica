using System.ComponentModel.DataAnnotations;

namespace Clinica.Infrastructure.Options;

// -----------------------------------------------------------------------------
// Opciones del worker del modulo 3.
// Estas opciones se pueden ajustar desde appsettings o variables de entorno.
// -----------------------------------------------------------------------------
public sealed class TicketQueueWorkerOptions
{
    public const string SectionName = "TicketQueueWorker";

    public bool ProcessNoShowEnabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int NoShowIntervalSeconds { get; set; } = 60;
}