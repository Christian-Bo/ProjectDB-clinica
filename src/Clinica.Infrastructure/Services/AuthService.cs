using Clinica.Application.Contracts;

namespace Clinica.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    public Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}