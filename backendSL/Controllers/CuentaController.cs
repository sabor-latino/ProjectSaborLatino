using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectSaborLatino.DTOs.Cuenta;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Controllers;

[ApiController]
[Route("api/cuenta")]
public class CuentaController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CuentaController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // POST api/cuenta/registro → crea una cuenta con rol Cliente
    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<ActionResult<PerfilDto>> Registrar(RegistroDto dto)
    {
        var usuario = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            NombreCompleto = dto.NombreCompleto,
            PhoneNumber = dto.Telefono
        };

        var resultado = await _userManager.CreateAsync(usuario, dto.Password);
        if (!resultado.Succeeded)
        {
            return ErroresDeIdentity(resultado);
        }

        await _userManager.AddToRoleAsync(usuario, Roles.Cliente);

        return CreatedAtAction(nameof(ObtenerPerfil), await CrearPerfilAsync(usuario));
    }

    // GET api/cuenta/yo → datos y rol del usuario que inició sesión
    [HttpGet("yo")]
    [Authorize]
    public async Task<ActionResult<PerfilDto>> ObtenerPerfil()
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Unauthorized();
        }

        return await CrearPerfilAsync(usuario);
    }

    // PUT api/cuenta/yo → editar nombre y teléfono propios
    [HttpPut("yo")]
    [Authorize]
    public async Task<ActionResult<PerfilDto>> ActualizarPerfil(ActualizarPerfilDto dto)
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return Unauthorized();
        }

        usuario.NombreCompleto = dto.NombreCompleto;
        usuario.PhoneNumber = dto.Telefono;

        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            return ErroresDeIdentity(resultado);
        }

        return await CrearPerfilAsync(usuario);
    }

    private async Task<PerfilDto> CrearPerfilAsync(ApplicationUser usuario)
    {
        return new PerfilDto
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email ?? string.Empty,
            Telefono = usuario.PhoneNumber,
            Roles = await _userManager.GetRolesAsync(usuario),
            FechaRegistro = usuario.FechaRegistro
        };
    }

    private ActionResult ErroresDeIdentity(IdentityResult resultado)
    {
        foreach (var error in resultado.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(ModelState);
    }
}
