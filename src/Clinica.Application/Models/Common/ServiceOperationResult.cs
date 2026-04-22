namespace Clinica.Application.Models.Common;

// -----------------------------------------------------------------------------
// Envoltorio simple para que servicios y controladores hablen el mismo idioma.
// De esta forma la API siempre devuelve: ok, code, message y data.
// -----------------------------------------------------------------------------
public sealed class ServiceOperationResult<T>
{
    public int HttpStatus { get; init; } = 200;
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public bool Success => HttpStatus >= 200 && HttpStatus < 300;
}
