using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface IPaqueteService
{
    Task<List<PaqueteDto>> ObtenerTodosAsync(bool soloActivos, int? servicioId);
    Task<PaqueteDto?> ObtenerPorIdAsync(int id, bool soloActivos);
    Task<PaqueteDto> CrearAsync(PaqueteGuardarDto dto);
    Task<PaqueteDto?> ActualizarAsync(int id, PaqueteGuardarDto dto);
    Task<bool> DesactivarAsync(int id);
}

public class PaqueteService : IPaqueteService
{
    private readonly ApplicationDbContext _db;

    public PaqueteService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Consulta base: trae el servicio y el equipo de cada paquete
    private IQueryable<Paquete> ConDetalle() => _db.Paquetes
        .Include(p => p.Servicio)
        .Include(p => p.Equipos)
            .ThenInclude(pe => pe.Equipo);

    public async Task<List<PaqueteDto>> ObtenerTodosAsync(bool soloActivos, int? servicioId)
    {
        var paquetes = await ConDetalle()
            .Where(p => !soloActivos || (p.Activo && p.Servicio!.Activo))
            .Where(p => servicioId == null || p.ServicioId == servicioId)
            .OrderBy(p => p.PrecioBase)
            .ToListAsync();

        return paquetes.Select(ADto).ToList();
    }

    public async Task<PaqueteDto?> ObtenerPorIdAsync(int id, bool soloActivos)
    {
        var paquete = await ConDetalle()
            .FirstOrDefaultAsync(p => p.Id == id && (!soloActivos || (p.Activo && p.Servicio!.Activo)));

        return paquete is null ? null : ADto(paquete);
    }

    public async Task<PaqueteDto> CrearAsync(PaqueteGuardarDto dto)
    {
        await ValidarAsync(dto);

        var paquete = new Paquete();
        CopiarDatos(dto, paquete);

        _db.Paquetes.Add(paquete);
        await _db.SaveChangesAsync();

        // Se vuelve a leer para devolverlo con nombres de servicio y equipo
        return (await ObtenerPorIdAsync(paquete.Id, soloActivos: false))!;
    }

    public async Task<PaqueteDto?> ActualizarAsync(int id, PaqueteGuardarDto dto)
    {
        var paquete = await _db.Paquetes
            .Include(p => p.Equipos)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (paquete is null)
        {
            return null;
        }

        await ValidarAsync(dto);

        CopiarDatos(dto, paquete);
        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(id, soloActivos: false);
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var paquete = await _db.Paquetes.FindAsync(id);
        if (paquete is null)
        {
            return false;
        }

        paquete.Activo = false;
        await _db.SaveChangesAsync();
        return true;
    }

    // Reglas que los atributos del DTO no pueden revisar porque necesitan la base de datos
    private async Task ValidarAsync(PaqueteGuardarDto dto)
    {
        if (!await _db.Servicios.AnyAsync(s => s.Id == dto.ServicioId))
        {
            throw new DatosInvalidosException($"No existe el servicio con Id {dto.ServicioId}.");
        }

        var equipoIds = dto.Equipos.Select(e => e.EquipoId).ToList();

        if (equipoIds.Count != equipoIds.Distinct().Count())
        {
            throw new DatosInvalidosException("Un mismo equipo aparece repetido en el paquete.");
        }

        var existentes = await _db.Equipos.CountAsync(e => equipoIds.Contains(e.Id));
        if (existentes != equipoIds.Count)
        {
            throw new DatosInvalidosException("Uno o más equipos indicados no existen.");
        }
    }

    private static void CopiarDatos(PaqueteGuardarDto dto, Paquete paquete)
    {
        paquete.ServicioId = dto.ServicioId;
        paquete.Nombre = dto.Nombre;
        paquete.NombreEn = dto.NombreEn;
        paquete.Descripcion = dto.Descripcion;
        paquete.DescripcionEn = dto.DescripcionEn;
        paquete.PrecioBase = dto.PrecioBase;
        paquete.ImagenUrl = dto.ImagenUrl;
        paquete.PotenciaSonido = dto.PotenciaSonido;
        paquete.CoberturaAforo = dto.CoberturaAforo;
        paquete.DuracionMontaje = dto.DuracionMontaje;
        paquete.RequerimientosElectricos = dto.RequerimientosElectricos;
        paquete.EspacioMinimo = dto.EspacioMinimo;
        paquete.FichaTecnica = dto.FichaTecnica;
        paquete.FichaTecnicaEn = dto.FichaTecnicaEn;
        paquete.Activo = dto.Activo;

        // Deja la lista de equipo igual a la del DTO:
        // 1) quita los que ya no vienen
        paquete.Equipos.RemoveAll(pe => !dto.Equipos.Any(e => e.EquipoId == pe.EquipoId));

        foreach (var e in dto.Equipos)
        {
            var existente = paquete.Equipos.FirstOrDefault(pe => pe.EquipoId == e.EquipoId);
            if (existente is null)
            {
                // 2) agrega los nuevos
                paquete.Equipos.Add(new PaqueteEquipo { EquipoId = e.EquipoId, Cantidad = e.Cantidad });
            }
            else
            {
                // 3) actualiza la cantidad de los que ya estaban
                existente.Cantidad = e.Cantidad;
            }
        }
    }

    private static PaqueteDto ADto(Paquete p) => new()
    {
        Id = p.Id,
        ServicioId = p.ServicioId,
        ServicioNombre = p.Servicio?.Nombre ?? string.Empty,
        ServicioNombreEn = p.Servicio?.NombreEn,
        Nombre = p.Nombre,
        NombreEn = p.NombreEn,
        Descripcion = p.Descripcion,
        DescripcionEn = p.DescripcionEn,
        PrecioBase = p.PrecioBase,
        ImagenUrl = p.ImagenUrl,
        PotenciaSonido = p.PotenciaSonido,
        CoberturaAforo = p.CoberturaAforo,
        DuracionMontaje = p.DuracionMontaje,
        RequerimientosElectricos = p.RequerimientosElectricos,
        EspacioMinimo = p.EspacioMinimo,
        FichaTecnica = p.FichaTecnica,
        FichaTecnicaEn = p.FichaTecnicaEn,
        Activo = p.Activo,
        Equipos = p.Equipos
            .OrderBy(pe => pe.Equipo?.Nombre)
            .Select(pe => new PaqueteEquipoDto
            {
                EquipoId = pe.EquipoId,
                EquipoNombre = pe.Equipo?.Nombre ?? string.Empty,
                EquipoNombreEn = pe.Equipo?.NombreEn,
                EspecificacionTecnica = pe.Equipo?.EspecificacionTecnica,
                EspecificacionTecnicaEn = pe.Equipo?.EspecificacionTecnicaEn,
                Cantidad = pe.Cantidad
            })
            .ToList()
    };
}
