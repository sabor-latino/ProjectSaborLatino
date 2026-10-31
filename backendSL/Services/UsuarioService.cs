using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface IUsuarioService
{
    Task<List<UsuarioDto>> ObtenerTodosAsync(string? rol, bool incluirInactivos, bool actorEsSuper);
    Task<UsuarioDto?> ObtenerPorIdAsync(string id, bool actorEsSuper);
    Task<UsuarioDto> CrearAsync(UsuarioCrearDto dto, string actorId, bool actorEsSuper);
    Task<UsuarioDto?> CambiarRolAsync(string id, string rol, string actorId, bool actorEsSuper);
    Task<UsuarioDto?> CambiarActivoAsync(string id, bool activo, string actorId, bool actorEsSuper);
}

// Reglas de alcance:
// - El Superadministrador ve y administra a todos.
// - El Administrador solo ve y administra Clientes y Trabajadores.
// Lo que queda fuera del alcance se trata como si no existiera (null → 404).
public class UsuarioService : IUsuarioService
{
    private static readonly string[] RolesDelAdministrador = [Roles.Cliente, Roles.Trabajador];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IBitacoraService _bitacora;

    public UsuarioService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IBitacoraService bitacora)
    {
        _db = db;
        _userManager = userManager;
        _bitacora = bitacora;
    }

    public async Task<List<UsuarioDto>> ObtenerTodosAsync(string? rol, bool incluirInactivos, bool actorEsSuper)
    {
        var consulta = UsuariosConRol();

        if (!string.IsNullOrWhiteSpace(rol))
        {
            var filtro = NormalizarRol(rol);
            consulta = consulta.Where(x => x.Rol == filtro);
        }

        if (!actorEsSuper)
        {
            consulta = consulta.Where(x => x.Rol == Roles.Cliente || x.Rol == Roles.Trabajador);
        }

        if (!incluirInactivos)
        {
            consulta = consulta.Where(x => x.Usuario.Activo);
        }

        var lista = await consulta
            .OrderBy(x => x.Usuario.NombreCompleto)
            .ToListAsync();

        return lista.Select(x => ADto(x.Usuario, x.Rol)).ToList();
    }

    public async Task<UsuarioDto?> ObtenerPorIdAsync(string id, bool actorEsSuper)
    {
        var (usuario, rol) = await BuscarEnAlcanceAsync(id, actorEsSuper);
        return usuario is null ? null : ADto(usuario, rol);
    }

    public async Task<UsuarioDto> CrearAsync(UsuarioCrearDto dto, string actorId, bool actorEsSuper)
    {
        var rol = NormalizarRol(dto.Rol);

        if (!actorEsSuper && rol != Roles.Trabajador)
        {
            throw new DatosInvalidosException("Un Administrador solo puede crear usuarios con rol Trabajador.");
        }

        var usuario = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true,
            NombreCompleto = dto.NombreCompleto.Trim(),
            PhoneNumber = dto.Telefono
        };

        var resultado = await _userManager.CreateAsync(usuario, dto.Password);
        if (!resultado.Succeeded)
        {
            throw new DatosInvalidosException(string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(usuario, rol);
        await _bitacora.RegistrarAsync(actorId, "Crear", "Usuario", usuario.Id, $"{usuario.Email} con rol {rol}");

        return ADto(usuario, rol);
    }

    public async Task<UsuarioDto?> CambiarRolAsync(string id, string rol, string actorId, bool actorEsSuper)
    {
        var (usuario, rolActual) = await BuscarEnAlcanceAsync(id, actorEsSuper);
        if (usuario is null)
        {
            return null;
        }

        if (usuario.Id == actorId)
        {
            throw new DatosInvalidosException("No puede cambiar su propio rol.");
        }

        var nuevo = NormalizarRol(rol);

        if (!actorEsSuper && !RolesDelAdministrador.Contains(nuevo))
        {
            throw new DatosInvalidosException("Un Administrador solo puede asignar los roles Cliente o Trabajador.");
        }

        if (nuevo == rolActual)
        {
            return ADto(usuario, rolActual);
        }

        if (rolActual == Roles.Superadministrador && usuario.Activo && !await HayOtroSuperActivoAsync(usuario.Id))
        {
            throw new DatosInvalidosException("No se puede quitar el rol al último Superadministrador activo.");
        }

        if (nuevo != Roles.Trabajador && await _db.Empleados.AnyAsync(e => e.UsuarioId == usuario.Id))
        {
            throw new DatosInvalidosException("No se puede cambiar el rol fuera de Trabajador mientras el usuario esté enlazado a un empleado.");
        }

        if (!string.IsNullOrEmpty(rolActual))
        {
            await _userManager.RemoveFromRoleAsync(usuario, rolActual);
        }

        await _userManager.AddToRoleAsync(usuario, nuevo);
        await _bitacora.RegistrarAsync(actorId, "CambiarRol", "Usuario", usuario.Id, $"{rolActual} → {nuevo}");

        return ADto(usuario, nuevo);
    }

    // Desactivar también bloquea el inicio de sesión (lockout permanente); activar lo revierte
    public async Task<UsuarioDto?> CambiarActivoAsync(string id, bool activo, string actorId, bool actorEsSuper)
    {
        var (usuario, rol) = await BuscarEnAlcanceAsync(id, actorEsSuper);
        if (usuario is null)
        {
            return null;
        }

        if (usuario.Id == actorId)
        {
            throw new DatosInvalidosException("No puede activarse ni desactivarse a sí mismo.");
        }

        if (usuario.Activo == activo)
        {
            return ADto(usuario, rol);
        }

        if (!activo && rol == Roles.Superadministrador && !await HayOtroSuperActivoAsync(usuario.Id))
        {
            throw new DatosInvalidosException("No se puede desactivar al último Superadministrador activo.");
        }

        usuario.Activo = activo;
        usuario.LockoutEnabled = true;
        usuario.LockoutEnd = activo ? null : DateTimeOffset.MaxValue;

        var resultado = await _userManager.UpdateSecurityStampAsync(usuario);
        if (!resultado.Succeeded)
        {
            throw new DatosInvalidosException(string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        await _bitacora.RegistrarAsync(actorId, activo ? "Activar" : "Desactivar", "Usuario", usuario.Id, usuario.Email);

        return ADto(usuario, rol);
    }

    // Cada usuario con el nombre de su rol (un usuario tiene un solo rol en este sistema)
    private IQueryable<UsuarioConRol> UsuariosConRol() =>
        from u in _db.Users
        let rol = (from ur in _db.UserRoles
                   join r in _db.Roles on ur.RoleId equals r.Id
                   where ur.UserId == u.Id
                   select r.Name).FirstOrDefault()
        select new UsuarioConRol { Usuario = u, Rol = rol ?? string.Empty };

    private async Task<(ApplicationUser? Usuario, string Rol)> BuscarEnAlcanceAsync(string id, bool actorEsSuper)
    {
        var encontrado = await UsuariosConRol().FirstOrDefaultAsync(x => x.Usuario.Id == id);

        if (encontrado is null || (!actorEsSuper && !RolesDelAdministrador.Contains(encontrado.Rol)))
        {
            return (null, string.Empty);
        }

        return (encontrado.Usuario, encontrado.Rol);
    }

    private async Task<bool> HayOtroSuperActivoAsync(string excluirUsuarioId)
    {
        var supers = await _userManager.GetUsersInRoleAsync(Roles.Superadministrador);
        return supers.Any(u => u.Id != excluirUsuarioId && u.Activo);
    }

    // Acepta "trabajador" o "TRABAJADOR" y devuelve el nombre exacto del rol
    private static string NormalizarRol(string rol)
    {
        var encontrado = Roles.Todos.FirstOrDefault(r => string.Equals(r, rol.Trim(), StringComparison.OrdinalIgnoreCase));
        return encontrado ?? throw new DatosInvalidosException($"Rol no válido: {rol}.");
    }

    private static UsuarioDto ADto(ApplicationUser u, string rol) => new()
    {
        Id = u.Id,
        Email = u.Email ?? string.Empty,
        NombreCompleto = u.NombreCompleto,
        Telefono = u.PhoneNumber,
        Rol = rol,
        Activo = u.Activo,
        FechaRegistro = u.FechaRegistro
    };

    private class UsuarioConRol
    {
        public ApplicationUser Usuario { get; set; } = null!;
        public string Rol { get; set; } = string.Empty;
    }
}