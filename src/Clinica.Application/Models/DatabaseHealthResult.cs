namespace Clinica.Application.Models;

public sealed class DatabaseHealthResult
{
    public bool IsSuccess { get; init; }
    public string? DatabaseName { get; init; }
    public DateTime? ServerUtcNow { get; init; }
    public string? EnvironmentName { get; init; }
    public string? DataSource { get; init; }
    public string? ErrorMessage { get; init; }
}
