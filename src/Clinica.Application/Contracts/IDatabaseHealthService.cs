using Clinica.Application.Models;

namespace Clinica.Application.Contracts;

public interface IDatabaseHealthService
{
    Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken = default);
}
