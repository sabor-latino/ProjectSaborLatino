using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface IImagenGaleriaService
{
    Task<List<ImagenGaleriaDto>> ObtenerTodosAsync(bool soloActivos);
    Task<ImagenGaleriaDto?> ObtenerPorIdAsync(int id, bool soloActivos);
    Task<ImagenGaleriaDto> CrearAsync(ImagenGaleriaGuardarDto dto);
    Task<ImagenGaleriaDto?> ActualizarAsync(int id, ImagenGaleriaGuardarDto dto);
    Task<bool> DesactivarAsync(int id);
}

public class ImagenGaleriaService : IImagenGaleriaService
{
    private readonly ApplicationDbContext _db;

    public ImagenGaleriaService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ImagenGaleriaDto>> ObtenerTodosAsync(bool soloActivos)
    {
        var lista = await _db.ImagenesGaleria
            .Where(x => !soloActivos || x.Activo)
            .OrderBy(x => x.Orden)
            .ToListAsync();

        return lista.Select(ADto).ToList();
    }

    public async Task<ImagenGaleriaDto?> ObtenerPorIdAsync(int id, bool soloActivos)
    {
        var entidad = await _db.ImagenesGaleria
            .FirstOrDefaultAsync(x => x.Id == id && (!soloActivos || x.Activo));

        return entidad is null ? null : ADto(entidad);
    }

    public async Task<ImagenGaleriaDto> CrearAsync(ImagenGaleriaGuardarDto dto)
    {
        var entidad = new ImagenGaleria();
        CopiarDatos(dto, entidad);

        _db.ImagenesGaleria.Add(entidad);
        await _db.SaveChangesAsync();

        return ADto(entidad);
    }

    public async Task<ImagenGaleriaDto?> ActualizarAsync(int id, ImagenGaleriaGuardarDto dto)
    {
        var entidad = await _db.ImagenesGaleria.FindAsync(id);
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
        var entidad = await _db.ImagenesGaleria.FindAsync(id);
        if (entidad is null)
        {
            return false;
        }

        entidad.Activo = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static void CopiarDatos(ImagenGaleriaGuardarDto dto, ImagenGaleria entidad)
    {
        entidad.Titulo = dto.Titulo;
        entidad.TituloEn = string.IsNullOrWhiteSpace(dto.TituloEn) ? null : dto.TituloEn.Trim();
        entidad.ImagenUrl = dto.ImagenUrl;
        entidad.Orden = dto.Orden;
        entidad.Activo = dto.Activo;
    }

    private static ImagenGaleriaDto ADto(ImagenGaleria x) => new()
    {
        Id = x.Id,
        Titulo = x.Titulo,
        TituloEn = x.TituloEn,
        ImagenUrl = x.ImagenUrl,
        Orden = x.Orden,
        Activo = x.Activo
    };
}