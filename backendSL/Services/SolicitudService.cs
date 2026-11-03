using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;

using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;

namespace ProjectSaborLatino.Services;

public interface ISolicitudService
{
    Task<SolicitudDto> CrearAsync(SolicitudCrearDto dto, string? clienteUsuarioId, string? actorUsuarioId = null);
    Task<List<SolicitudDto>> ObtenerTodasAsync(string? estado);
    Task<List<SolicitudDto>> ObtenerDeClienteAsync(string clienteUsuarioId);
    Task<SolicitudDto?> ObtenerPorIdAsync(int id, string? soloDelClienteId);
    Task<SolicitudDto?> CambiarEstadoAsync(int id, string estado);
    Task<SolicitudDto?> MarcarWhatsAppRecibidoAsync(int id, string usuarioId);
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

    public async Task<SolicitudDto> CrearAsync(SolicitudCrearDto dto, string? clienteUsuarioId, string? actorUsuarioId = null)
    {
        var correo = Normalizar(dto.CorreoContacto);
        var telefono = Normalizar(dto.TelefonoContacto);
        if (correo is null && telefono is null)
            throw new DatosInvalidosException("Indique al menos un correo o un teléfono de contacto.");
        if (correo is not null && !new EmailAddressAttribute().IsValid(correo))
            throw new DatosInvalidosException("El correo de contacto no es válido.");
        if (telefono is not null && !new PhoneAttribute().IsValid(telefono))
            throw new DatosInvalidosException("El teléfono de contacto no es válido.");
        if (dto.ClaveIdempotencia == Guid.Empty)
            throw new DatosInvalidosException("La clave de idempotencia no puede estar vacía.");

        var huella = dto.ClaveIdempotencia is null ? null : Convert.ToHexString(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(new
            {
                ActorUsuarioId = actorUsuarioId ?? clienteUsuarioId,
                ClienteUsuarioId = clienteUsuarioId,
                NombreContacto = dto.NombreContacto.Trim(),
                CorreoContacto = correo,
                TelefonoContacto = telefono,
                dto.PaqueteId,
                TipoEvento = dto.TipoEvento.Trim(),
                dto.FechaEvento,
                Lugar = dto.Lugar.Trim(),
                dto.CantidadInvitados,
                Comentarios = Normalizar(dto.Comentarios)
            })));

        if (dto.ClaveIdempotencia is Guid clave)
        {
            var existente = await ReutilizarAsync(clave, huella!);
            if (existente is not null) return existente;
        }

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
            CorreoContacto = correo,
            TelefonoContacto = telefono,
            ClaveIdempotencia = dto.ClaveIdempotencia,
            HuellaIdempotencia = huella,
            PaqueteId = dto.PaqueteId,
            TipoEvento = dto.TipoEvento.Trim(),
            FechaEvento = fechaEvento,
            Lugar = dto.Lugar.Trim(),
            CantidadInvitados = dto.CantidadInvitados,
            Comentarios = Normalizar(dto.Comentarios)
        };

        _db.Solicitudes.Add(solicitud);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (dto.ClaveIdempotencia is not null &&
            ex.InnerException is SqlException { Number: 2601 or 2627 } sql &&
            sql.Message.Contains("IX_Solicitudes_ClaveIdempotencia", StringComparison.Ordinal))
        {
            // El índice único arbitra entre procesos. El interceptor ya revirtió
            // negocio y auditoría del perdedor; solo se devuelve el ganador válido.
            _db.Entry(solicitud).State = EntityState.Detached;
            var existente = await ReutilizarAsync(dto.ClaveIdempotencia.Value, huella!);
            if (existente is not null) return existente;
            throw;
        }

        await _db.Entry(solicitud).Reference(s => s.Paquete).LoadAsync();
        return ADtoCreacion(solicitud);
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private async Task<SolicitudDto?> ReutilizarAsync(Guid clave, string huella)
    {
        var existente = await _db.Solicitudes.AsNoTracking().Include(s => s.Paquete)
            .SingleOrDefaultAsync(s => s.ClaveIdempotencia == clave);
        if (existente is null) return null;
        if (!string.Equals(existente.HuellaIdempotencia, huella, StringComparison.Ordinal))
            throw new ConflictoIdempotenciaException();
        return ADtoCreacion(existente);
    }

    private static SolicitudDto ADtoCreacion(Solicitud solicitud)
    {
        // Repetir una creación anónima no concede acceso a la evolución privada
        // que sí requiere autenticación en GET (cotizaciones, recepción y estado).
        var respuesta = ADto(solicitud);
        respuesta.Estado = EstadoSolicitud.Pendiente.ToString();
        respuesta.Cotizaciones = [];
        respuesta.RecibidoWhatsAppEn = null;
        respuesta.RecibidoWhatsAppPorUsuarioId = null;
        return respuesta;
    }

    public async Task<SolicitudDto?> MarcarWhatsAppRecibidoAsync(int id, string usuarioId)
    {
        await using var transaccion = await _db.Database.BeginTransactionAsync();
        var solicitud = await _db.Solicitudes.FromSqlInterpolated(
            $"SELECT * FROM [Solicitudes] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .AsNoTracking().SingleOrDefaultAsync();
        if (solicitud is null) return null;
        if (solicitud.RecibidoWhatsAppEn is null)
        {
            foreach (var previa in _db.Solicitudes.Local.Where(s => s.Id == id).ToList())
                _db.Entry(previa).State = EntityState.Detached;
            _db.Solicitudes.Attach(solicitud);
            solicitud.RecibidoWhatsAppEn = DateTime.UtcNow;
            solicitud.RecibidoWhatsAppPorUsuarioId = usuarioId;
            await _db.SaveChangesAsync();
        }
        await transaccion.CommitAsync();
        return await ObtenerPorIdAsync(id, soloDelClienteId: null);
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
        RecibidoWhatsAppEn = s.RecibidoWhatsAppEn is DateTime recibido
            ? DateTime.SpecifyKind(recibido, DateTimeKind.Utc) : null,
        RecibidoWhatsAppPorUsuarioId = s.RecibidoWhatsAppPorUsuarioId,
        Cotizaciones = s.Cotizaciones
            .OrderByDescending(c => c.FechaCreacion)
            .Select(CotizacionService.ADto)
            .ToList()
    };
}
