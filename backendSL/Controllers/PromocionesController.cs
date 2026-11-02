using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;

namespace ProjectSaborLatino.Controllers;

// Promociones de temporada del catálogo
[ApiController]
[Route("api/promociones")]
[Authorize(Roles = Roles.Administrador + "," + Roles.Superadministrador)]
public class PromocionesController : ControllerBase
{
    private readonly IPromocionTemporadaService _service;

    public PromocionesController(IPromocionTemporadaService service)
    {
        _service = service;
    }

    // GET api/promociones → lista pública de promociones activas y vigentes
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<PromocionTemporadaDto>>> ObtenerTodas(bool incluirInactivas = false)
    {
        return await _service.ObtenerTodasAsync(SoloActivasYVigentes(incluirInactivas));
    }

    // GET api/promociones/5
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<PromocionTemporadaDto>> ObtenerPorId(int id, bool incluirInactivas = false)
    {
        var dto = await _service.ObtenerPorIdAsync(id, SoloActivasYVigentes(incluirInactivas));
        return dto is null ? NotFound() : dto;
    }

    // POST api/promociones → crear (solo Administrador y Superadministrador)
    [HttpPost]
    public async Task<ActionResult<PromocionTemporadaDto>> Crear(PromocionTemporadaGuardarDto dto)
    {
        try
        {
            var creada = await _service.CrearAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creada.Id }, creada);
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // PUT api/promociones/5 → editar
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PromocionTemporadaDto>> Actualizar(int id, PromocionTemporadaGuardarDto dto)
    {
        try
        {
            var actualizada = await _service.ActualizarAsync(id, dto);
            return actualizada is null ? NotFound() : actualizada;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // DELETE api/promociones/5 → desactivar (Activo = false)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        return await _service.DesactivarAsync(id) ? NoContent() : NotFound();
    }

    private bool SoloActivasYVigentes(bool incluirInactivas)
    {
        var esAdmin = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Superadministrador);
        return !(incluirInactivas && esAdmin);
    }

    private ActionResult DatosInvalidos(DatosInvalidosException ex)
    {
        ModelState.AddModelError("Promocion", ex.Message);
        return ValidationProblem(ModelState);
    }
}
