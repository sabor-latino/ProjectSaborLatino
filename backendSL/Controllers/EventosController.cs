using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSaborLatino.DTOs.Eventos;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;

namespace ProjectSaborLatino.Controllers;

// Eventos contratados: creación, equipo, trabajadores, estados y disponibilidad
[ApiController]
[Route("api/eventos")]
public class EventosController : ControllerBase
{
    private const string RolesAdmin = Roles.Administrador + "," + Roles.Superadministrador;

    private readonly IEventoService _service;

    public EventosController(IEventoService service)
    {
        _service = service;
    }

    // POST api/eventos → crear el evento desde una cotización aceptada (solo admin)
    [HttpPost]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<EventoDto>> Crear(EventoCrearDto dto)
    {
        try
        {
            var creado = await _service.CrearAsync(dto, UsuarioId()!);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // GET api/eventos?desde=&hasta=&estado=&clienteId=&paqueteId= → todos, por fecha de inicio (solo admin)
    [HttpGet]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<List<EventoDto>>> ObtenerTodos(
        DateTime? desde = null, DateTime? hasta = null, string? estado = null,
        string? clienteId = null, int? paqueteId = null)
    {
        try
        {
            return await _service.ObtenerTodosAsync(desde, hasta, estado, clienteId, paqueteId);
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // GET api/eventos/5 → admin: cualquiera; Trabajador: si está asignado; Cliente: si es suyo (si no, 404)
    [HttpGet("{id:int}")]
    [Authorize(Roles = RolesAdmin + "," + Roles.Trabajador + "," + Roles.Cliente)]
    public async Task<ActionResult<EventoDto>> ObtenerPorId(int id)
    {
        string? soloDelCliente = null;
        string? soloDelTrabajador = null;

        if (!EsAdmin())
        {
            if (User.IsInRole(Roles.Trabajador))
            {
                soloDelTrabajador = UsuarioId();
            }
            else
            {
                soloDelCliente = UsuarioId();
            }
        }

        var dto = await _service.ObtenerPorIdAsync(id, soloDelCliente, soloDelTrabajador);
        return dto is null ? NotFound() : dto;
    }

    // PUT api/eventos/5 → modificar fechas, lugar y notas del evento (solo admin)
    [HttpPut("{id:int}")]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<EventoDto>> Modificar(int id, EventoModificarDto dto)
    {
        try
        {
            var actualizado = await _service.ModificarAsync(id, dto, UsuarioId()!);
            return actualizado is null ? NotFound() : actualizado;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // PUT api/eventos/5/equipos → reemplaza todo el equipo reservado (solo admin)
    [HttpPut("{id:int}/equipos")]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<EventoDto>> AsignarEquipos(int id, List<EventoEquipoAsignarDto> equipos)
    {
        try
        {
            var dto = await _service.AsignarEquiposAsync(id, equipos);
            return dto is null ? NotFound() : dto;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // PUT api/eventos/5/trabajadores → reemplaza todos los trabajadores asignados (solo admin)
    [HttpPut("{id:int}/trabajadores")]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<EventoDto>> AsignarTrabajadores(int id, List<EventoTrabajadorAsignarDto> trabajadores)
    {
        try
        {
            var dto = await _service.AsignarTrabajadoresAsync(id, trabajadores);
            return dto is null ? NotFound() : dto;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // PATCH api/eventos/5/estado → avanzar al siguiente estado (admin o Trabajador asignado)
    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = RolesAdmin + "," + Roles.Trabajador)]
    public async Task<ActionResult<EventoDto>> CambiarEstado(int id, EventoCambiarEstadoDto dto)
    {
        try
        {
            var actualizado = await _service.CambiarEstadoAsync(id, dto, UsuarioId()!, EsAdmin() ? null : UsuarioId());
            return actualizado is null ? NotFound() : actualizado;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // PATCH api/eventos/5/cancelar → cancelar con motivo (solo admin)
    [HttpPatch("{id:int}/cancelar")]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<EventoDto>> Cancelar(int id, EventoCancelarDto dto)
    {
        try
        {
            var cancelado = await _service.CancelarAsync(id, dto.Motivo, UsuarioId()!);
            return cancelado is null ? NotFound() : cancelado;
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // GET api/equipos/disponibilidad?desde=&hasta= (la "/" inicial ignora el prefijo api/eventos)
    [HttpGet("/api/equipos/disponibilidad")]
    [Authorize(Roles = RolesAdmin)]
    public async Task<ActionResult<List<DisponibilidadEquipoDto>>> Disponibilidad(DateTime desde, DateTime hasta)
    {
        try
        {
            return await _service.ObtenerDisponibilidadAsync(desde, hasta);
        }
        catch (DatosInvalidosException ex)
        {
            return DatosInvalidos(ex);
        }
    }

    // GET api/mis-eventos → Cliente: los de sus solicitudes; Trabajador: donde está asignado
    [HttpGet("/api/mis-eventos")]
    [Authorize(Roles = Roles.Cliente + "," + Roles.Trabajador)]
    public async Task<ActionResult<List<EventoDto>>> MisEventos()
    {
        return User.IsInRole(Roles.Trabajador)
            ? await _service.ObtenerDeTrabajadorAsync(UsuarioId()!)
            : await _service.ObtenerDeClienteAsync(UsuarioId()!);
    }

    private string? UsuarioId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool EsAdmin() =>
        User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Superadministrador);

    private ActionResult DatosInvalidos(DatosInvalidosException ex)
    {
        ModelState.AddModelError("Evento", ex.Message);
        return ValidationProblem(ModelState);
    }
}
