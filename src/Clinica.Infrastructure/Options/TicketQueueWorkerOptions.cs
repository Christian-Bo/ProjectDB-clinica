using System.ComponentModel.DataAnnotations;

namespace Clinica.Infrastructure.Options;

public sealed class TicketQueueWorkerOptions
{
    public const string SectionName = "TicketQueueWorker";

    public bool ProcessNoShowEnabled { get; set; } = true;

    private int _noShowIntervalSeconds = 60;

    [Range(1, int.MaxValue)]
    public int NoShowIntervalSeconds
    {
        get => _noShowIntervalSeconds;
        set => _noShowIntervalSeconds = value > 0 ? value : 60;
    }
}