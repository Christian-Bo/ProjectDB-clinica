using Clinica.Application.Models.Common;
using Clinica.Application.Models.Recetas;

namespace Clinica.Application.Contracts;

// -----------------------------------------------------------------------------
// Contrato del servicio de Recetas.
// Crea recetas desde una consulta cerrada y consulta recetas existentes.
// Los errores de alergia o principio activo duplicado vienen del SP y
// se exponen tal como los devuelve SQL Server.
// -----------------------------------------------------------------------------
public interface IRecetasService
{
    Task<ServiceOperationResult<RecetaResponseDto>> CrearAsync(
        CrearRecetaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceOperationResult<RecetaResponseDto>> ObtenerAsync(
        long recetaId,
        CancellationToken cancellationToken = default);
}
