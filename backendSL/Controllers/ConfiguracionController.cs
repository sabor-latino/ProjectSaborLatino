using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;

namespace ProjectSaborLatino.Controllers;

// Parámetros del sistema (Administrador y Superadministrador)
[ApiController]
[Route("api/configuracion")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Superadministrador}")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionService _service;

    public ConfiguracionController(IConfiguracionService service)
    {
        _service = service;
    }

    // GET api/configuracion
    [HttpGet]
    public async Task<ActionResult<List<ConfiguracionDto>>> ObtenerTodas()
    {
        return await _service.ObtenerTodasAsync();
    }

    // PUT api/configuracion/Fidelidad.EventosMinimos  { "valor": "5" }
    [HttpPut("{clave}")]
    public async Task<ActionResult<ConfiguracionDto>> Actualizar(string clave, ConfiguracionGuardarDto dto)
    {
        try
        {
            var actualizada = await _service.ActualizarAsync(clave, dto.Valor);
            return actualizada is null ? NotFound() : actualizada;
        }
        catch (DatosInvalidosException ex)
        {
            ModelState.AddModelError("Configuracion", ex.Message);
            return ValidationProblem(ModelState);
        }
    }
}