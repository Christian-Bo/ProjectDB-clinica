namespace Clinica.Application.DTOs.Common;

/// <summary>Envelope estándar para todas las respuestas de la API.</summary>
public sealed record ApiResponse<T>
{
    public bool   Ok      { get; init; }
    public string Code    { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public T?     Data    { get; init; }

    public static ApiResponse<T> Success(T data, string message = "OK") =>
        new() { Ok = true, Code = "OK", Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, string code = "ERROR") =>
        new() { Ok = false, Code = code, Message = message, Data = default };
}

/// <summary>Respuesta de lista genérica.</summary>
public sealed record ApiListResponse<T>
{
    public bool     Ok      { get; init; }
    public string   Code    { get; init; } = string.Empty;
    public string   Message { get; init; } = string.Empty;
    public List<T>  Data    { get; init; } = [];
    public int      Total   { get; init; }
}
