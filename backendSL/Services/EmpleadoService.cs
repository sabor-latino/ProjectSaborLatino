using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Eventos;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface IEmpleadoService
{
    Task<List<EmpleadoDto>> ObtenerTodosAsync(bool soloActivos);
    Task<EmpleadoDto?> ObtenerPorIdAsync(int id);
    Task<EmpleadoDto> CrearAsync(EmpleadoGuardarDto dto);
    Task<EmpleadoDto?> ActualizarAsync(int id, EmpleadoGuardarDto dto);
    Task<bool> DesactivarAsync(int id);
}

public class EmpleadoService : IEmpleadoService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmpleadoService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<List<EmpleadoDto>> ObtenerTodosAsync(bool soloActivos)
    {
        var empleados = await _db.Empleados
            .Where(e => !soloActivos || e.Activo)
            .OrderBy(e => e.NombreCompleto)
            .ToListAsync();

        return empleados.Select(ADto).ToList();
    }

    public async Task<EmpleadoDto?> ObtenerPorIdAsync(int id)
    {
        var empleado = await _db.Empleados.FindAsync(id);
        return empleado is null ? null : ADto(empleado);
    }

    public async Task<EmpleadoDto> CrearAsync(EmpleadoGuardarDto dto)
    {
        await ValidarAsync(dto, idActual: null);

        var empleado = new Empleado();
        CopiarDatos(dto, empleado);

        _db.Empleados.Add(empleado);
        await _db.SaveChangesAsync();

        return ADto(empleado);
    }

    public async Task<EmpleadoDto?> ActualizarAsync(int id, EmpleadoGuardarDto dto)
    {
        var empleado = await _db.Empleados.FindAsync(id);
        if (empleado is null)
        {
            return null;
        }

        await ValidarAsync(dto, idActual: id);

        CopiarDatos(dto, empleado);
        await _db.SaveChangesAsync();

        return ADto(empleado);
    }

    // "Borrado lógico": el empleado queda inactivo
    public async Task<bool> DesactivarAsync(int id)
    {
        var empleado = await _db.Empleados.FindAsync(id);
        if (empleado is null)
        {
            return false;
        }

        empleado.Activo = false;
        await _db.SaveChangesAsync();
        return true;
    }

    // idActual es el Id del empleado que se edita (null al crear), para no chocar consigo mismo
    private async Task ValidarAsync(EmpleadoGuardarDto dto, int? idActual)
    {
        var cedula = dto.Cedula.Trim();
        if (await _db.Empleados.AnyAsync(e => e.Cedula == cedula && e.Id != idActual))
        {
            throw new DatosInvalidosException($"Ya existe un empleado con la cédula {cedula}.");
        }

        if (string.IsNullOrWhiteSpace(dto.UsuarioId))
        {
            return;
        }

        var usuario = await _userManager.FindByIdAsync(dto.UsuarioId);
        if (usuario is null)
        {
            throw new DatosInvalidosException("No existe el usuario indicado.");
        }

        if (!usuario.Activo)
        {
            throw new DatosInvalidosException("El usuario indicado está inactivo.");
        }

        if (!await _userManager.IsInRoleAsync(usuario, Roles.Trabajador))
        {
            throw new DatosInvalidosException("El usuario indicado no tiene el rol Trabajador.");
        }

        if (await _db.Empleados.AnyAsync(e => e.UsuarioId == dto.UsuarioId && e.Id != idActual))
        {
            throw new DatosInvalidosException("Ese usuario ya está enlazado a otro empleado.");
        }
    }

    private static void CopiarDatos(EmpleadoGuardarDto dto, Empleado empleado)
    {
        empleado.Cedula = dto.Cedula.Trim();
        empleado.NombreCompleto = dto.NombreCompleto.Trim();
        empleado.Puesto = dto.Puesto.Trim();
        empleado.Telefono = dto.Telefono;
        empleado.Activo = dto.Activo;
        empleado.UsuarioId = string.IsNullOrWhiteSpace(dto.UsuarioId) ? null : dto.UsuarioId;
    }

    private static EmpleadoDto ADto(Empleado e) => new()
    {
        Id = e.Id,
        Cedula = e.Cedula,
        NombreCompleto = e.NombreCompleto,
        Puesto = e.Puesto,
        Telefono = e.Telefono,
        Activo = e.Activo,
        UsuarioId = e.UsuarioId
    };
}