using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;

namespace ProjectSaborLatino.Controllers;

// Administración de usuarios. El Superadministrador ve a todos;
// el Administrador solo a Clientes y Trabajadores (lo demás da 404)
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Roles.Administrador + "," + Roles.Superadministrador)]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsuariosController(IUsuarioService service)
    {
        _service = service;
    }

    // GET api/usuarios?rol=Trabajador&incluirInactivos=true
    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> ObtenerTodos(string? rol = null, bool incluirInactivos = false)
    {
        try
        {
            return await _service.ObtenerTodosAsync(rol, incluirInactivos, EsSuper());
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // GET api/usuarios/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioDto>> ObtenerPorId(string id)
    {
        var dto = await _service.ObtenerPorIdAsync(id, EsSuper());
        return dto is null ? NotFound() : dto;
    }

    // POST api/usuarios → el Administrador solo puede crear Trabajadores
    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(UsuarioCrearDto dto)
    {
        try
        {
            var creado = await _service.CrearAsync(dto, UsuarioId(), EsSuper());
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // PUT api/usuarios/{id} o PUT api/usuarios/{id}/perfil
    [HttpPut("{id}")]
    [HttpPut("{id}/perfil")]
    public async Task<ActionResult<UsuarioDto>> ModificarPerfil(string id, UsuarioModificarPerfilDto dto)
    {
        try
        {
            var actualizado = await _service.ModificarPerfilAsync(id, dto, UsuarioId(), EsSuper());
            return actualizado is null ? NotFound() : actualizado;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // PATCH api/usuarios/{id}/rol
    [HttpPatch("{id}/rol")]
    public async Task<ActionResult<UsuarioDto>> CambiarRol(string id, UsuarioCambiarRolDto dto)
    {
        try
        {
            var actualizado = await _service.CambiarRolAsync(id, dto.Rol, UsuarioId(), EsSuper());
            return actualizado is null ? NotFound() : actualizado;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // PATCH api/usuarios/{id}/activo → desactivar también impide iniciar sesión
    [HttpPatch("{id}/activo")]
    public async Task<ActionResult<UsuarioDto>> CambiarActivo(string id, UsuarioCambiarActivoDto dto)
    {
        try
        {
            var actualizado = await _service.CambiarActivoAsync(id, dto.Activo!.Value, UsuarioId(), EsSuper());
            return actualizado is null ? NotFound() : actualizado;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    private string UsuarioId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private bool EsSuper() => User.IsInRole(Roles.Superadministrador);

    private ActionResult DatosInvalidos(DatosInvalidosException ex)
    {
        ModelState.AddModelError("Usuario", ex.Message);
        return ValidationProblem(ModelState);
    }
}
