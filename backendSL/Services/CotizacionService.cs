using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface ICotizacionService
{
    Task<CotizacionDto?> CrearAsync(int solicitudId, CotizacionCrearDto dto, string creadaPorUsuarioId);
    Task<CotizacionDto?> AceptarAsync(int id);
    Task<CotizacionDto?> RechazarAsync(int id, string? soloDelClienteId);
}

public class CotizacionService : ICotizacionService
{
    private readonly ApplicationDbContext _db;

    public CotizacionService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Devuelve null si la solicitud no existe (→ 404)
    public async Task<CotizacionDto?> CrearAsync(int solicitudId, CotizacionCrearDto dto, string creadaPorUsuarioId)
    {
        await using var transaccion = await _db.Database.BeginTransactionAsync();
        var solicitud = await BloquearSolicitudAsync(solicitudId);
        if (solicitud is null)
        {
            return null;
        }

        if (solicitud.Estado is EstadoSolicitud.Aceptada or EstadoSolicitud.Rechazada or EstadoSolicitud.Cancelada)
        {
            throw new DatosInvalidosException($"No se puede cotizar una solicitud {solicitud.Estado}.");
        }

        if (dto.Descuento > dto.Monto)
        {
            throw new DatosInvalidosException("El descuento no puede ser mayor que el monto.");
        }

        var vigenteHasta = dto.VigenteHasta!.Value;
        if (vigenteHasta <= DateTime.Now)
        {
            throw new DatosInvalidosException("La fecha de vigencia debe ser en el futuro.");
        }

        var cotizacion = new Cotizacion
        {
            SolicitudId = solicitudId,
            Monto = dto.Monto,
            Descuento = dto.Descuento,
            Detalle = dto.Detalle,
            VigenteHasta = vigenteHasta,
            CreadaPorUsuarioId = creadaPorUsuarioId
        };

        _db.Cotizaciones.Add(cotizacion);
        solicitud.Estado = EstadoSolicitud.Cotizada;
        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return ADto(cotizacion);
    }

    // Aceptar = "contrato": esta queda Aceptada, las otras enviadas se rechazan y la solicitud pasa a Aceptada
    public async Task<CotizacionDto?> AceptarAsync(int id)
    {
        var solicitudId = await _db.Cotizaciones.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => (int?)c.SolicitudId)
            .SingleOrDefaultAsync();
        if (solicitudId is null)
        {
            return null;
        }

        await using var transaccion = await _db.Database.BeginTransactionAsync();
        var solicitud = await BloquearSolicitudAsync(solicitudId.Value);
        if (solicitud is null)
        {
            return null;
        }

        var cotizacion = await _db.Cotizaciones
            .SingleOrDefaultAsync(c => c.Id == id && c.SolicitudId == solicitudId.Value);
        if (cotizacion is null)
        {
            return null;
        }

        if (cotizacion.Estado != EstadoCotizacion.Enviada)
        {
            throw new DatosInvalidosException($"Solo se puede aceptar una cotización Enviada (esta está {cotizacion.Estado}).");
        }

        if (cotizacion.VigenteHasta < DateTime.Now)
        {
            // Contrato existente: responder 400, pero persistir Vencida. La bitácora
            // participa en esta transacción y debe guardarse antes del commit.
            cotizacion.Estado = EstadoCotizacion.Vencida;
            await _db.SaveChangesAsync();
            await transaccion.CommitAsync();
            throw new DatosInvalidosException("La cotización está vencida y no se puede aceptar.");
        }

        if (solicitud.Estado is EstadoSolicitud.Aceptada or EstadoSolicitud.Rechazada or EstadoSolicitud.Cancelada)
        {
            throw new DatosInvalidosException($"La solicitud está {solicitud.Estado} y ya no se puede aceptar.");
        }

        cotizacion.Estado = EstadoCotizacion.Aceptada;

        var otras = await _db.Cotizaciones
            .Where(c => c.SolicitudId == solicitudId.Value && c.Id != id && c.Estado == EstadoCotizacion.Enviada)
            .ToListAsync();
        foreach (var otra in otras)
        {
            otra.Estado = EstadoCotizacion.Rechazada;
        }

        solicitud.Estado = EstadoSolicitud.Aceptada;
        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return ADto(cotizacion);
    }

    // Si soloDelClienteId trae valor, la solicitud tiene que ser de ese cliente (si no, null → 404)
    public async Task<CotizacionDto?> RechazarAsync(int id, string? soloDelClienteId)
    {
        var solicitudId = await _db.Cotizaciones.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => (int?)c.SolicitudId)
            .SingleOrDefaultAsync();
        if (solicitudId is null)
        {
            return null;
        }

        await using var transaccion = await _db.Database.BeginTransactionAsync();
        var solicitud = await BloquearSolicitudAsync(solicitudId.Value);
        if (solicitud is null ||
            (soloDelClienteId is not null && solicitud.ClienteUsuarioId != soloDelClienteId))
        {
            return null;
        }

        var cotizacion = await _db.Cotizaciones
            .SingleOrDefaultAsync(c => c.Id == id && c.SolicitudId == solicitudId.Value);

        if (cotizacion is null)
        {
            return null;
        }

        if (cotizacion.Estado != EstadoCotizacion.Enviada)
        {
            throw new DatosInvalidosException($"Solo se puede rechazar una cotización Enviada (esta está {cotizacion.Estado}).");
        }

        cotizacion.Estado = EstadoCotizacion.Rechazada;
        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return ADto(cotizacion);
    }

    // Todas las mutaciones de cotizaciones toman primero el mismo bloqueo por solicitud.
    // AsNoTracking evita reutilizar una entidad precargada antes de adquirirlo.
    private async Task<Solicitud?> BloquearSolicitudAsync(int solicitudId)
    {
        var solicitud = await _db.Solicitudes
            .FromSqlInterpolated($"SELECT * FROM [Solicitudes] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {solicitudId}")
            .AsNoTracking()
            .SingleOrDefaultAsync();
        if (solicitud is null) return null;

        // Descartar solo copias anteriores de esta solicitud. Otros cambios pendientes
        // del mismo DbContext pertenecen a su consumidor y deben conservarse.
        foreach (var anterior in _db.Cotizaciones.Local.Where(c => c.SolicitudId == solicitudId).ToList())
        {
            _db.Entry(anterior).State = EntityState.Detached;
        }
        foreach (var anterior in _db.Solicitudes.Local.Where(s => s.Id == solicitudId).ToList())
        {
            _db.Entry(anterior).State = EntityState.Detached;
        }
        _db.Solicitudes.Attach(solicitud);
        return solicitud;
    }

    // Público y estático para que SolicitudService también lo use
    public static CotizacionDto ADto(Cotizacion c) => new()
    {
        Id = c.Id,
        SolicitudId = c.SolicitudId,
        Monto = c.Monto,
        Descuento = c.Descuento,
        Total = c.Monto - c.Descuento,
        Detalle = c.Detalle,
        VigenteHasta = c.VigenteHasta,
        Estado = c.Estado.ToString(),
        FechaCreacion = c.FechaCreacion
    };
}
