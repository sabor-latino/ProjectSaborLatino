using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;

namespace ProjectSaborLatino.Controllers;

// Administración del contenido bilingüe de la página de inicio
[ApiController]
[Route("api/contenido-inicio")]
public class ContenidoInicioController : ControllerBase
{
    private readonly IContenidoInicioService _service;

    public ContenidoInicioController(IContenidoInicioService service)
    {
        _service = service;
    }

    // GET api/contenido-inicio → público para la portada
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ContenidoInicioDto>> Obtener()
    {
        return await _service.ObtenerAsync();
    }

    // PUT api/contenido-inicio → editable por Administrador y Superadministrador
    [HttpPut]
    [Authorize(Roles = Roles.Administrador + "," + Roles.Superadministrador)]
    public async Task<ActionResult<ContenidoInicioDto>> Actualizar(ContenidoInicioDto dto)
    {
        try
        {
            return await _service.ActualizarAsync(dto);
        }
        catch (DatosInvalidosException ex)
        {
            ModelState.AddModelError("ContenidoInicio", ex.Message);
            return ValidationProblem(ModelState);
        }
    }
}
