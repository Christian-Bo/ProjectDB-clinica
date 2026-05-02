namespace Clinica.Application.Exceptions;

/// <summary>
/// Excepcion para reglas de negocio violadas. El middleware la traduce a HTTP 422.
/// Convencion del constructor: primero el mensaje visible para el cliente y luego el codigo interno opcional.
/// </summary>
public sealed class BusinessException : Exception
{
    public string? Code { get; }

    public BusinessException(string message, string? code = null) : base(message)
    {
        Code = code;
    }
}

/// <summary>
/// Excepcion para recursos inexistentes. El middleware la traduce a HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>
/// Excepcion para conflictos de concurrencia, duplicados o estados invalidos. El middleware la traduce a HTTP 409.
/// Convencion del constructor: primero el mensaje visible para el cliente y luego el codigo interno opcional.
/// </summary>
public sealed class ConflictException : Exception
{
    public string? Code { get; }

    public ConflictException(string message, string? code = null) : base(message)
    {
        Code = code;
    }
}
