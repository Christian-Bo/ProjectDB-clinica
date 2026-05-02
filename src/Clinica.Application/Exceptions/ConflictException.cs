namespace Clinica.Application.Exceptions;

public sealed class ConflictException : Exception
{
    public string? Code { get; }

    public ConflictException(string message, string? code = null)
        : base(message)
    {
        Code = code;
    }
}
