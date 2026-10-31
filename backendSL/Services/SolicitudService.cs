using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface ISolicitudService
{
    Task<SolicitudDto> CrearAsync(SolicitudCrearDto dto, string? clienteUsuarioId);
    Task<List<SolicitudDto>> ObtenerTodasAsync(string? estado);
    Task<List<SolicitudDto>> ObtenerDeClienteAsync(string clienteUsuarioId);
    Task<SolicitudDto?> ObtenerPorIdAsync(int id, string? soloDelClienteId);
    Task<SolicitudDto?> CambiarEstadoAsync(int id, string estado);
}

public class SolicitudService : ISolicitudService
{
    private readonly ApplicationDbContext _db;

    public SolicitudService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Consulta base: trae el paquete y las cotizaciones de cada solicitud
    private IQueryable<Solicitud> ConDetalle() => _db.Solicitudes
        .Include(s => s.Paquete)
        .Include(s => s.Cotizaciones);

    public async Task<SolicitudDto> CrearAsync(SolicitudCrearDto dto, string? clienteUsuarioId)
    {
        var fechaEvento = dto.FechaEvento!.Value;
        if (fechaEvento <= DateTime.Now)
        {
            throw new DatosInvalidosException("La fecha del evento debe ser en el futuro.");
        }

        if (dto.PaqueteId is not null &&
            !await _db.Paquetes.AnyAsync(p => p.Id == dto.PaqueteId && p.Activo && p.Servicio!.Activo))
        {
            throw new DatosInvalidosException($"No existe un paquete activo con Id {dto.PaqueteId}.");
        }

        var solicitud = new Solicitud
        {
            ClienteUsuarioId = clienteUsuarioId,
            NombreContacto = dto.NombreContacto.Trim(),
            CorreoContacto = dto.CorreoContacto.Trim(),
            TelefonoContacto = dto.TelefonoContacto,
            PaqueteId = dto.PaqueteId,
            TipoEvento = dto.TipoEvento.Trim(),
            FechaEvento = fechaEvento,
            Lugar = dto.Lugar.Trim(),
            CantidadInvitados = dto.CantidadInvitados,
            Comentarios = dto.Comentarios
        };

        _db.Solicitudes.Add(solicitud);
        await _db.SaveChangesAsync();

        return (await ObtenerPorIdAsync(solicitud.Id, soloDelClienteId: null))!;
    }

    // Para el admin. Filtro opcional por estado: ?estado=Pendiente
    public async Task<List<SolicitudDto>> ObtenerTodasAsync(string? estado)
    {
        var consulta = ConDetalle();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<EstadoSolicitud>(estado, ignoreCase: true, out var filtro))
            {
                throw new DatosInvalidosException($"Estado no válido: {estado}.");
            }

            consulta = consulta.Where(s => s.Estado == filtro);
        }

        var solicitudes = await consulta
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync();

        return solicitudes.Select(ADto).ToList();
    }

    // Para el cliente: solo las suyas
    public async Task<List<SolicitudDto>> ObtenerDeClienteAsync(string clienteUsuarioId)
    {
        var solicitudes = await ConDetalle()
            .Where(s => s.ClienteUsuarioId == clienteUsuarioId)
            .OrderByDescending(s => s.FechaCreacion)
            .ToListAsync();

        return solicitudes.Select(ADto).ToList();
    }

    // Si soloDelClienteId trae valor, la solicitud tiene que ser de ese cliente (si no, null → 404)
    public async Task<SolicitudDto?> ObtenerPorIdAsync(int id, string? soloDelClienteId)
    {
        var solicitud = await ConDetalle().FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud is null ||
            (soloDelClienteId is not null && solicitud.ClienteUsuarioId != soloDelClienteId))
        {
            return null;
        }

        return ADto(solicitud);
    }

    // El admin solo puede rechazar o cancelar, y nunca una solicitud ya aceptada
    public async Task<SolicitudDto?> CambiarEstadoAsync(int id, string estado)
    {
        await using var transaccion = await _db.Database.BeginTransactionAsync();
        var solicitud = await _db.Solicitudes
            .FromSqlInterpolated($"SELECT * FROM [Solicitudes] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .AsNoTracking()
            .SingleOrDefaultAsync();
        if (solicitud is null)
        {
            return null;
        }

        // La lectura bloqueada prevalece sobre copias anteriores de esta solicitud.
        // Se conservan los cambios pendientes de otras entidades del contexto.
        foreach (var anterior in _db.Cotizaciones.Local.Where(c => c.SolicitudId == id).ToList())
        {
            _db.Entry(anterior).State = EntityState.Detached;
        }
        foreach (var anterior in _db.Solicitudes.Local.Where(s => s.Id == id).ToList())
        {
            _db.Entry(anterior).State = EntityState.Detached;
        }
        _db.Solicitudes.Attach(solicitud);

        if (!Enum.TryParse<EstadoSolicitud>(estado, ignoreCase: true, out var nuevo) ||
            (nuevo != EstadoSolicitud.Rechazada && nuevo != EstadoSolicitud.Cancelada))
        {
            throw new DatosInvalidosException("Solo se puede cambiar el estado a Rechazada o Cancelada.");
        }

        if (solicitud.Estado == EstadoSolicitud.Aceptada)
        {
            throw new DatosInvalidosException("La solicitud ya fue aceptada y no se puede cambiar.");
        }

        var enviadas = await _db.Cotizaciones
            .Where(c => c.SolicitudId == id && c.Estado == EstadoCotizacion.Enviada)
            .ToListAsync();
        foreach (var cotizacion in enviadas)
        {
            cotizacion.Estado = EstadoCotizacion.Rechazada;
        }

        solicitud.Estado = nuevo;
        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        return await ObtenerPorIdAsync(id, soloDelClienteId: null);
    }

    private static SolicitudDto ADto(Solicitud s) => new()
    {
        Id = s.Id,
        ClienteUsuarioId = s.ClienteUsuarioId,
        NombreContacto = s.NombreContacto,
        CorreoContacto = s.CorreoContacto,
        TelefonoContacto = s.TelefonoContacto,
        PaqueteId = s.PaqueteId,
        PaqueteNombre = s.Paquete?.Nombre,
        TipoEvento = s.TipoEvento,
        FechaEvento = s.FechaEvento,
        Lugar = s.Lugar,
        CantidadInvitados = s.CantidadInvitados,
        Comentarios = s.Comentarios,
        Estado = s.Estado.ToString(),
        FechaCreacion = s.FechaCreacion,
        Cotizaciones = s.Cotizaciones
            .OrderByDescending(c => c.FechaCreacion)
            .Select(CotizacionService.ADto)
            .ToList()
    };
}
