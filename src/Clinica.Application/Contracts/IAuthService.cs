namespace Clinica.Application.Contracts;

/// <summary>
/// Contrato mínimo del servicio de autenticación.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Intenta autenticar un usuario y devuelve un token si aplica.
    /// </summary>
    Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida si un token es reconocido como válido.
    /// </summary>
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta el cierre de sesión lógico si aplica.
    /// </summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);
}