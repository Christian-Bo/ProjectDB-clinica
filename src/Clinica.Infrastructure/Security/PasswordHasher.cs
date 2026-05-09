using BCrypt.Net;

namespace Clinica.Infrastructure.Security;

public sealed class PasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    public string GenerateSalt()
        => BCrypt.Net.BCrypt.GenerateSalt(WorkFactor);
}