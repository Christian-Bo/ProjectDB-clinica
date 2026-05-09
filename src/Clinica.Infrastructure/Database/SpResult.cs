namespace Clinica.Infrastructure.Database;

/// <summary>
/// Resultado estándar devuelto por los Stored Procedures de escritura.
/// Espera columnas como: HttpStatus, Codigo, Mensaje y opcionalmente EntityId/CitaId/PacienteId/Id.
/// </summary>
public sealed class SpResult
{
    public int HttpStatus { get; init; } = 200;
    public string Codigo { get; init; } = "OK";
    public string Mensaje { get; init; } = "Operacion ejecutada correctamente.";
    public long? EntityId { get; init; }

    public bool Success => HttpStatus >= 200 && HttpStatus < 300;
}
