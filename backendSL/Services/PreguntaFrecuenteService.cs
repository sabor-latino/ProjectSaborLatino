using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface IPreguntaFrecuenteService
{
    Task<List<PreguntaFrecuenteDto>> ObtenerTodosAsync(bool soloActivos);
    Task<PreguntaFrecuenteDto?> ObtenerPorIdAsync(int id, bool soloActivos);
    Task<PreguntaFrecuenteDto> CrearAsync(PreguntaFrecuenteGuardarDto dto);
    Task<PreguntaFrecuenteDto?> ActualizarAsync(int id, PreguntaFrecuenteGuardarDto dto);
    Task<bool> DesactivarAsync(int id);
}

public class PreguntaFrecuenteService : IPreguntaFrecuenteService
{
    private readonly ApplicationDbContext _db;

    public PreguntaFrecuenteService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<PreguntaFrecuenteDto>> ObtenerTodosAsync(bool soloActivos)
    {
        var lista = await _db.PreguntasFrecuentes
            .Where(x => !soloActivos || x.Activo)
            .OrderBy(x => x.Orden)
            .ToListAsync();

        return lista.Select(ADto).ToList();
    }

    public async Task<PreguntaFrecuenteDto?> ObtenerPorIdAsync(int id, bool soloActivos)
    {
        var entidad = await _db.PreguntasFrecuentes
            .FirstOrDefaultAsync(x => x.Id == id && (!soloActivos || x.Activo));

        return entidad is null ? null : ADto(entidad);
    }

    public async Task<PreguntaFrecuenteDto> CrearAsync(PreguntaFrecuenteGuardarDto dto)
    {
        var entidad = new PreguntaFrecuente();
        CopiarDatos(dto, entidad);

        _db.PreguntasFrecuentes.Add(entidad);
        await _db.SaveChangesAsync();

        return ADto(entidad);
    }

    public async Task<PreguntaFrecuenteDto?> ActualizarAsync(int id, PreguntaFrecuenteGuardarDto dto)
    {
        var entidad = await _db.PreguntasFrecuentes.FindAsync(id);
        if (entidad is null)
        {
            return null;
        }

        CopiarDatos(dto, entidad);
        await _db.SaveChangesAsync();

        return ADto(entidad);
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var entidad = await _db.PreguntasFrecuentes.FindAsync(id);
        if (entidad is null)
        {
            return false;
        }

        entidad.Activo = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static void CopiarDatos(PreguntaFrecuenteGuardarDto dto, PreguntaFrecuente entidad)
    {
        entidad.Pregunta = dto.Pregunta;
        entidad.PreguntaEn = dto.PreguntaEn;
        entidad.Respuesta = dto.Respuesta;
        entidad.RespuestaEn = dto.RespuestaEn;
        entidad.Orden = dto.Orden;
        entidad.Activo = dto.Activo;
    }

    private static PreguntaFrecuenteDto ADto(PreguntaFrecuente x) => new()
    {
        Id = x.Id,
        Pregunta = x.Pregunta,
        PreguntaEn = x.PreguntaEn,
        Respuesta = x.Respuesta,
        RespuestaEn = x.RespuestaEn,
        Orden = x.Orden,
        Activo = x.Activo
    };
}
