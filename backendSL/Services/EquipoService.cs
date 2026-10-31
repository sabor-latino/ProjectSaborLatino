using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface IEquipoService
{
    Task<List<EquipoDto>> ObtenerTodosAsync(bool soloActivos);
    Task<EquipoDto?> ObtenerPorIdAsync(int id, bool soloActivos);
    Task<EquipoDto> CrearAsync(EquipoGuardarDto dto);
    Task<EquipoDto?> ActualizarAsync(int id, EquipoGuardarDto dto);
    Task<bool> DesactivarAsync(int id);
}

public class EquipoService : IEquipoService
{
    private readonly ApplicationDbContext _db;

    public EquipoService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<EquipoDto>> ObtenerTodosAsync(bool soloActivos)
    {
        var lista = await _db.Equipos
            .Where(x => !soloActivos || x.Activo)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        return lista.Select(ADto).ToList();
    }

    public async Task<EquipoDto?> ObtenerPorIdAsync(int id, bool soloActivos)
    {
        var entidad = await _db.Equipos
            .FirstOrDefaultAsync(x => x.Id == id && (!soloActivos || x.Activo));

        return entidad is null ? null : ADto(entidad);
    }

    public async Task<EquipoDto> CrearAsync(EquipoGuardarDto dto)
    {
        var entidad = new Equipo();
        CopiarDatos(dto, entidad);

        _db.Equipos.Add(entidad);
        await _db.SaveChangesAsync();

        return ADto(entidad);
    }

    public async Task<EquipoDto?> ActualizarAsync(int id, EquipoGuardarDto dto)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await ReservaLocks.TomarAsync(_db, [$"equipo:{id}"]);
        var entidad = await _db.Equipos.FindAsync(id);
        if (entidad is null)
        {
            return null;
        }

        // Las fechas de eventos se guardan como hora local sin zona; solo compromisos
        // cuyo fin aún no pasó limitan el inventario actual.
        var maximoReservado = await EventoService.MaximoReservadoAsync(
            _db, id, DateTime.Now, DateTime.MaxValue, null);
        if (dto.CantidadTotal < maximoReservado)
        {
            throw new DatosInvalidosException(
                $"La cantidad total no puede ser menor que {maximoReservado} unidades ya reservadas simultáneamente.");
        }

        CopiarDatos(dto, entidad);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return ADto(entidad);
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await ReservaLocks.TomarAsync(_db, [$"equipo:{id}"]);
        var entidad = await _db.Equipos.FindAsync(id);
        if (entidad is null)
        {
            return false;
        }

        // Desactivar impide nuevas reservas; las reservas históricas y futuras
        // existentes permanecen en la base y siguen contando para el inventario.
        entidad.Activo = false;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    private static void CopiarDatos(EquipoGuardarDto dto, Equipo entidad)
    {
        entidad.Nombre = dto.Nombre;
        entidad.Tipo = dto.Tipo;
        entidad.CantidadTotal = dto.CantidadTotal;
        entidad.Activo = dto.Activo;
    }

    private static EquipoDto ADto(Equipo x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Tipo = x.Tipo,
        CantidadTotal = x.CantidadTotal,
        Activo = x.Activo
    };
}
