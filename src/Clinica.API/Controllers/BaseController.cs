using Microsoft.AspNetCore.Mvc;

namespace Clinica.API.Controllers;

// Controlador base — todos los controladores deben heredar de este
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    // TODO: agregar helpers compartidos entre controladores
}
