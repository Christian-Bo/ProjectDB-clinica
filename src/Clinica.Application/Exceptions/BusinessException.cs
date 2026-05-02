namespace Clinica.Application.Exceptions;

// Se lanza cuando un SP devuelve una regla de negocio violada (422)
public sealed class BusinessException : Exception
{
    public string Codigo { get; }

    public BusinessException(string codigo, string mensaje) : base(mensaje)
    {
        Codigo = codigo;
    }
}

// Se lanza cuando hay un conflicto de concurrencia o duplicado (409)
public sealed class ConflictException : Exception
{
    public string Codigo { get; }

    public ConflictException(string codigo, string mensaje) : base(mensaje)
    {
        Codigo = codigo;
    }
}

// Se lanza cuando no se encuentra un recurso (404)
public sealed class NotFoundException : Exception
{
    public NotFoundException(string mensaje) : base(mensaje) { }
}