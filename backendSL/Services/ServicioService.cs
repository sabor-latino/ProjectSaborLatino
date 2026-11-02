using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

// Contrato: qué se puede hacer con los servicios
public interface IServicioService
{
    Task<List<ServicioDto>> ObtenerTodosAsync(bool soloActivos);
    Task<ServicioDto?> ObtenerPorIdAsync(int id, bool soloActivos);
    Task<ServicioDto> CrearAsync(ServicioGuardarDto dto);
    Task<ServicioDto?> ActualizarAsync(int id, ServicioGuardarDto dto);
    Task<bool> DesactivarAsync(int id);
}

// Implementación: cómo se hace, usando la base de datos
public class ServicioService : IServicioService
{
    private readonly ApplicationDbContext _db;

    public ServicioService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ServicioDto>> ObtenerTodosAsync(bool soloActivos)
    {
        var servicios = await _db.Servicios
            .Where(s => !soloActivos || s.Activo)
            .OrderBy(s => s.Nombre)
            .ToListAsync();

        return servicios.Select(ADto).ToList();
    }

    public async Task<ServicioDto?> ObtenerPorIdAsync(int id, bool soloActivos)
    {
        var servicio = await _db.Servicios
            .FirstOrDefaultAsync(s => s.Id == id && (!soloActivos || s.Activo));

        return servicio is null ? null : ADto(servicio);
    }

    public async Task<ServicioDto> CrearAsync(ServicioGuardarDto dto)
    {
        var servicio = new Servicio();
        CopiarDatos(dto, servicio);

        _db.Servicios.Add(servicio);
        await _db.SaveChangesAsync();

        return ADto(servicio);
    }

    public async Task<ServicioDto?> ActualizarAsync(int id, ServicioGuardarDto dto)
    {
        var servicio = await _db.Servicios.FindAsync(id);
        if (servicio is null)
        {
            return null;
        }

        CopiarDatos(dto, servicio);
        await _db.SaveChangesAsync();

        return ADto(servicio);
    }

    // "Borrado lógico": no se elimina la fila, solo se marca como inactiva
    public async Task<bool> DesactivarAsync(int id)
    {
        var servicio = await _db.Servicios.FindAsync(id);
        if (servicio is null)
        {
            return false;
        }

        servicio.Activo = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static void CopiarDatos(ServicioGuardarDto dto, Servicio servicio)
    {
        servicio.Nombre = dto.Nombre;
        servicio.NombreEn = dto.NombreEn;
        servicio.Descripcion = dto.Descripcion;
        servicio.DescripcionEn = dto.DescripcionEn;
        servicio.ImagenUrl = dto.ImagenUrl;
        servicio.Activo = dto.Activo;
    }

    private static ServicioDto ADto(Servicio s) => new()
    {
        Id = s.Id,
        Nombre = s.Nombre,
        NombreEn = s.NombreEn,
        Descripcion = s.Descripcion,
        DescripcionEn = s.DescripcionEn,
        ImagenUrl = s.ImagenUrl,
        Activo = s.Activo
    };
}
