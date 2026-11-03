using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;

namespace ProjectSaborLatino.Controllers;

// Solicitudes de cotización que envían visitantes y clientes
[ApiController]
[Route("api/solicitudes")]
public class SolicitudesController : ControllerBase
{
    private const string RolesAdmin = Roles.Administrador + "," + Roles.Superadministrador;

    private readonly ISolicitudService _solicitudes;
    private readonly ICotizacionService _cotizaciones;

    public SolicitudesController(ISolicitudService solicitudes, ICotizacionService cotizaciones)
    {
        _solicitudes = solicitudes;
        _cotizaciones = cotizaciones;
    }

    // POST api/solicitudes → cualquiera (visitante o cliente) pide una cotización
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<SolicitudDto>> Crear(SolicitudCrearDto dto)
    {
        // Si quien la manda es un Cliente con sesión iniciada, queda ligada a su cuenta
        var clienteId = User.IsInRole(Roles.Cliente) ? UsuarioId() : null;

        try
        {
            var creada = await _solicitudes.CrearAsync(dto, clienteId, UsuarioId());
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creada.Id }, creada);
        }
        catch (ConflictoIdempotenciaException ex)
        {
            return Conflict(new ProblemDetails { Status = 409, Title = ex.Message });
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // Confirmación manual de recepción; nunca cambia el estado comercial.
    [HttpPost("{id:int}/whatsapp-recibido")]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<SolicitudDto>> MarcarWhatsAppRecibido(int id)
    {
        var solicitud = await _solicitudes.MarcarWhatsAppRecibidoAsync(id, UsuarioId()!);
        return solicitud is null ? NotFound() : Ok(solicitud);
    }

    // GET api/solicitudes?estado=Pendiente → todas, más recientes primero (solo admin)
    [HttpGet]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<List<SolicitudDto>>> ObtenerTodas(string? estado = null)
    {
        try
        {
            return await _solicitudes.ObtenerTodasAsync(estado);
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // GET api/solicitudes/mias → las del cliente que inició sesión, con sus cotizaciones
    [HttpGet("mias")]
    [Authorize(Roles = Roles.Cliente)]
    public async Task<ActionResult<List<SolicitudDto>>> ObtenerMias()
    {
        return await _solicitudes.ObtenerDeClienteAsync(UsuarioId()!);
    }

    // GET api/solicitudes/5 → el admin ve cualquiera; el cliente solo las suyas (si no, 404)
    [HttpGet("{id:int}")]
    [Authorize(Roles = RolesAdmin + "," + Roles.Cliente)]
    public async Task<ActionResult<SolicitudDto>> ObtenerPorId(int id)
    {
        var dto = await _solicitudes.ObtenerPorIdAsync(id, EsAdmin() ? null : UsuarioId());
        return dto is null ? NotFound() : dto;
    }

    // PATCH api/solicitudes/5/estado → rechazar o cancelar (solo admin)
    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<SolicitudDto>> CambiarEstado(int id, SolicitudCambiarEstadoDto dto)
    {
        try
        {
            var actualizada = await _solicitudes.CambiarEstadoAsync(id, dto.Estado);
            return actualizada is null ? NotFound() : actualizada;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // POST api/solicitudes/5/cotizaciones → el admin responde con una cotización
    [HttpPost("{id:int}/cotizaciones")]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<CotizacionDto>> CrearCotizacion(int id, CotizacionCrearDto dto)
    {
        try
        {
            var creada = await _cotizaciones.CrearAsync(id, dto, UsuarioId()!);
            return creada is null
                ? NotFound()
                : CreatedAtAction(nameof(ObtenerPorId), new { id }, creada);
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // Id del usuario que inició sesión (viene dentro del token)
    private string? UsuarioId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool EsAdmin() =>
        User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Superadministrador);

    // Convierte el error del Service en un 400 con el mensaje
    private ActionResult DatosInvalidos(DatosInvalidosException ex)
    {
        ModelState.AddModelError("Solicitud", ex.Message);
        return ValidationProblem(ModelState);
    }
}
