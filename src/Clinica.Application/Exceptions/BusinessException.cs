namespace Clinica.Application.Exceptions;

public sealed class BusinessException : Exception
{
    public string? Code { get; }

    public BusinessException(string message, string? code = null)
        : base(message)
    {
        Code = code;
    }
}
