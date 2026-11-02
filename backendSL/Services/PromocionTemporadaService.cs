using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface IPromocionTemporadaService
{
    Task<List<PromocionTemporadaDto>> ObtenerTodasAsync(bool soloActivasYVigentes);
    Task<PromocionTemporadaDto?> ObtenerPorIdAsync(int id, bool soloActivasYVigentes);
    Task<PromocionTemporadaDto> CrearAsync(PromocionTemporadaGuardarDto dto);
    Task<PromocionTemporadaDto?> ActualizarAsync(int id, PromocionTemporadaGuardarDto dto);
    Task<bool> DesactivarAsync(int id);
}

public class PromocionTemporadaService : IPromocionTemporadaService
{
    private readonly ApplicationDbContext _db;

    public PromocionTemporadaService(ApplicationDbContext db)
    {
        _db = db;
    }

    private IQueryable<PromocionTemporada> ConDetalle() => _db.PromocionesTemporada
        .Include(p => p.Paquetes)
            .ThenInclude(pp => pp.Paquete)
                .ThenInclude(paq => paq!.Servicio);

    public async Task<List<PromocionTemporadaDto>> ObtenerTodasAsync(bool soloActivasYVigentes)
    {
        var corteVigencia = DateTime.UtcNow.AddDays(-1);
        var promociones = await ConDetalle()
            .Where(p => !soloActivasYVigentes || (p.Activo && p.FechaFin >= corteVigencia && p.FechaInicio <= DateTime.UtcNow.AddDays(1)))
            .OrderByDescending(p => p.DestacadaEnInicio)
            .ThenByDescending(p => p.PorcentajeDescuento)
            .ThenBy(p => p.FechaFin)
            .ToListAsync();

        return promociones.Select(p => ADto(p, soloActivasYVigentes)).ToList();
    }

    public async Task<PromocionTemporadaDto?> ObtenerPorIdAsync(int id, bool soloActivasYVigentes)
    {
        var corteVigencia = DateTime.UtcNow.AddDays(-1);
        var promocion = await ConDetalle()
            .FirstOrDefaultAsync(p => p.Id == id && (!soloActivasYVigentes || (p.Activo && p.FechaFin >= corteVigencia && p.FechaInicio <= DateTime.UtcNow.AddDays(1))));

        return promocion is null ? null : ADto(promocion, soloActivasYVigentes);
    }

    public async Task<PromocionTemporadaDto> CrearAsync(PromocionTemporadaGuardarDto dto)
    {
        await ValidarAsync(dto, exigirPaquetesActivos: true);

        var promocion = new PromocionTemporada();
        CopiarDatos(dto, promocion);

        _db.PromocionesTemporada.Add(promocion);
        await _db.SaveChangesAsync();

        return (await ObtenerPorIdAsync(promocion.Id, soloActivasYVigentes: false))!;
    }

    public async Task<PromocionTemporadaDto?> ActualizarAsync(int id, PromocionTemporadaGuardarDto dto)
    {
        var promocion = await _db.PromocionesTemporada
            .Include(p => p.Paquetes)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (promocion is null)
        {
            return null;
        }

        await ValidarAsync(dto, exigirPaquetesActivos: dto.Activo);

        CopiarDatos(dto, promocion);
        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(id, soloActivasYVigentes: false);
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var promocion = await _db.PromocionesTemporada.FindAsync(id);
        if (promocion is null)
        {
            return false;
        }

        promocion.Activo = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task ValidarAsync(PromocionTemporadaGuardarDto dto, bool exigirPaquetesActivos)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new DatosInvalidosException("El nombre de la promoción es obligatorio.");
        }

        if (dto.PorcentajeDescuento <= 0m || dto.PorcentajeDescuento > 90m)
        {
            throw new DatosInvalidosException("El porcentaje de descuento debe ser mayor a 0 y menor o igual a 90.");
        }

        if (dto.FechaFin <= dto.FechaInicio)
        {
            throw new DatosInvalidosException("La fecha de fin debe ser posterior a la fecha de inicio.");
        }

        var paqueteIds = (dto.PaqueteIds ?? []).Distinct().ToList();
        if (paqueteIds.Count > 0)
        {
            var existentes = await _db.Paquetes
                .CountAsync(p => paqueteIds.Contains(p.Id) && (!exigirPaquetesActivos || (p.Activo && p.Servicio!.Activo)));
            if (existentes != paqueteIds.Count)
            {
                throw new DatosInvalidosException("Uno o más paquetes indicados no existen o están inactivos.");
            }
        }
    }

    private static void CopiarDatos(PromocionTemporadaGuardarDto dto, PromocionTemporada promocion)
    {
        promocion.Nombre = dto.Nombre.Trim();
        promocion.NombreEn = string.IsNullOrWhiteSpace(dto.NombreEn) ? null : dto.NombreEn.Trim();
        promocion.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        promocion.DescripcionEn = string.IsNullOrWhiteSpace(dto.DescripcionEn) ? null : dto.DescripcionEn.Trim();
        promocion.EtiquetaTemporada = string.IsNullOrWhiteSpace(dto.EtiquetaTemporada) ? null : dto.EtiquetaTemporada.Trim();
        promocion.EtiquetaTemporadaEn = string.IsNullOrWhiteSpace(dto.EtiquetaTemporadaEn) ? null : dto.EtiquetaTemporadaEn.Trim();
        promocion.PorcentajeDescuento = dto.PorcentajeDescuento;
        promocion.CodigoPromocional = string.IsNullOrWhiteSpace(dto.CodigoPromocional) ? null : dto.CodigoPromocional.Trim();
        promocion.FechaInicio = dto.FechaInicio;
        promocion.FechaFin = dto.FechaFin;
        promocion.ImagenUrl = string.IsNullOrWhiteSpace(dto.ImagenUrl) ? null : dto.ImagenUrl.Trim();
        promocion.DestacadaEnInicio = dto.DestacadaEnInicio;
        promocion.Activo = dto.Activo;

        var idsDeseados = (dto.PaqueteIds ?? []).Distinct().ToList();
        promocion.Paquetes.RemoveAll(pp => !idsDeseados.Contains(pp.PaqueteId));

        foreach (var paqueteId in idsDeseados)
        {
            if (!promocion.Paquetes.Any(pp => pp.PaqueteId == paqueteId))
            {
                promocion.Paquetes.Add(new PromocionTemporadaPaquete { PaqueteId = paqueteId });
            }
        }
    }

    private static PromocionTemporadaDto ADto(PromocionTemporada p, bool soloActivasYVigentes)
    {
        var ahoraUtc = DateTime.UtcNow;
        var ahoraLocal = DateTime.Now;
        var vigente = p.Activo &&
            ((p.FechaInicio <= ahoraUtc && p.FechaFin >= ahoraUtc) ||
             (p.FechaInicio <= ahoraLocal && p.FechaFin >= ahoraLocal));

        var paquetesFiltrados = p.Paquetes
            .Where(pp => pp.Paquete is not null &&
                         (!soloActivasYVigentes || (pp.Paquete.Activo && (pp.Paquete.Servicio?.Activo ?? true))))
            .OrderBy(pp => pp.Paquete!.PrecioBase)
            .Select(pp =>
            {
                var precioBase = pp.Paquete!.PrecioBase;
                return new PromocionPaqueteItemDto
                {
                    PaqueteId = pp.PaqueteId,
                    PaqueteNombre = pp.Paquete.Nombre,
                    PaqueteNombreEn = pp.Paquete.NombreEn,
                    PrecioBase = precioBase,
                    PrecioPromocional = Math.Round(precioBase * (1m - p.PorcentajeDescuento / 100m), 2)
                };
            })
            .ToList();

        return new PromocionTemporadaDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            NombreEn = p.NombreEn,
            Descripcion = p.Descripcion,
            DescripcionEn = p.DescripcionEn,
            EtiquetaTemporada = p.EtiquetaTemporada,
            EtiquetaTemporadaEn = p.EtiquetaTemporadaEn,
            PorcentajeDescuento = p.PorcentajeDescuento,
            CodigoPromocional = p.CodigoPromocional,
            FechaInicio = p.FechaInicio,
            FechaFin = p.FechaFin,
            ImagenUrl = p.ImagenUrl,
            DestacadaEnInicio = p.DestacadaEnInicio,
            Activo = p.Activo,
            Vigente = vigente,
            Paquetes = paquetesFiltrados
        };
    }
}
