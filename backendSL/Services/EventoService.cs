using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Eventos;
using ProjectSaborLatino.Models;
using Microsoft.Data.SqlClient;

namespace ProjectSaborLatino.Services;

// Los bloqueos de aplicación serializan las escrituras de un recurso entre DbContexts.
internal static class ReservaLocks
{
    public static async Task TomarAsync(ApplicationDbContext db, IEnumerable<string> recursos)
    {
        foreach (var recurso in recursos.Distinct().Order(StringComparer.Ordinal))
        {
            try
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @resultado int; EXEC @resultado = sp_getapplock @Resource={"SaborLatino:" + recurso}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @resultado < 0 THROW 51000, 'No se pudo bloquear el recurso.', 1;");
            }
            catch (SqlException ex) when (ex.Number is 1205 or 1222 or 51000)
            {
                throw new DatosInvalidosException("El recurso está ocupado; vuelva a intentar la operación.");
            }
        }
    }
}

public interface IEventoService
{
    Task<EventoDto> CrearAsync(EventoCrearDto dto, string usuarioId);
    Task<List<EventoDto>> ObtenerTodosAsync(DateTime? desde, DateTime? hasta, string? estado, string? clienteId, int? paqueteId);
    Task<EventoDto?> ObtenerPorIdAsync(int id, string? soloDelClienteId, string? soloDelTrabajadorUsuarioId);
    Task<List<EventoDto>> ObtenerDeClienteAsync(string clienteUsuarioId);
    Task<List<EventoDto>> ObtenerDeTrabajadorAsync(string trabajadorUsuarioId);
    Task<EventoDto?> AsignarEquiposAsync(int id, List<EventoEquipoAsignarDto> equipos);
    Task<EventoDto?> AsignarTrabajadoresAsync(int id, List<EventoTrabajadorAsignarDto> trabajadores);
    Task<EventoDto?> CambiarEstadoAsync(int id, EventoCambiarEstadoDto dto, string usuarioId, string? soloDelTrabajadorUsuarioId);
    Task<EventoDto?> CancelarAsync(int id, string motivo, string usuarioId);
    Task<List<DisponibilidadEquipoDto>> ObtenerDisponibilidadAsync(DateTime desde, DateTime hasta);
}

public class EventoService : IEventoService
{
    private readonly ApplicationDbContext _db;

    public EventoService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Consulta base: trae todo lo que se muestra de un evento.
    // AsSplitQuery hace una consulta por cada lista, para no multiplicar filas.
    private IQueryable<Evento> ConDetalle() => _db.Eventos
        .Include(e => e.Cotizacion)
            .ThenInclude(c => c!.Solicitud)
        .Include(e => e.Paquete)
        .Include(e => e.Equipos)
            .ThenInclude(ee => ee.Equipo)
        .Include(e => e.Trabajadores)
            .ThenInclude(t => t.Empleado)
        .Include(e => e.Historial)
        .AsSplitQuery();

    public async Task<EventoDto> CrearAsync(EventoCrearDto dto, string usuarioId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var cotizacion = await _db.Cotizaciones
            .Include(c => c.Solicitud)
            .FirstOrDefaultAsync(c => c.Id == dto.CotizacionId);

        if (cotizacion is null)
        {
            throw new DatosInvalidosException($"No existe la cotización con Id {dto.CotizacionId}.");
        }

        if (cotizacion.Estado != EstadoCotizacion.Aceptada)
        {
            throw new DatosInvalidosException($"Solo se crea un evento desde una cotización Aceptada (esta está {cotizacion.Estado}).");
        }

        if (await _db.Eventos.AnyAsync(e => e.CotizacionId == dto.CotizacionId))
        {
            throw new DatosInvalidosException("Ya existe un evento para esa cotización.");
        }

        var inicio = dto.FechaInicio!.Value;
        var fin = dto.FechaFin!.Value;
        if (fin <= inicio)
        {
            throw new DatosInvalidosException("La fecha de fin debe ser posterior a la de inicio.");
        }

        var solicitud = cotizacion.Solicitud!;

        var evento = new Evento
        {
            CotizacionId = cotizacion.Id,
            PaqueteId = solicitud.PaqueteId,
            FechaInicio = inicio,
            FechaFin = fin,
            Lugar = string.IsNullOrWhiteSpace(dto.Lugar) ? solicitud.Lugar : dto.Lugar.Trim(),
            Notas = dto.Notas
        };

        // Reserva el mismo equipo del paquete, si hay suficiente en esas fechas
        if (dto.CopiarEquipoDelPaquete && solicitud.PaqueteId is not null)
        {
            var equipoDelPaquete = await _db.PaquetesEquipos
                .Where(pe => pe.PaqueteId == solicitud.PaqueteId)
                .Select(pe => new EventoEquipoAsignarDto { EquipoId = pe.EquipoId, Cantidad = pe.Cantidad })
                .ToListAsync();

            await ReservaLocks.TomarAsync(_db, equipoDelPaquete.Select(e => $"equipo:{e.EquipoId}"));
            await ValidarDisponibilidadAsync(equipoDelPaquete, inicio, fin, excluirEventoId: null);

            foreach (var e in equipoDelPaquete)
            {
                evento.Equipos.Add(new EventoEquipo { EquipoId = e.EquipoId, Cantidad = e.Cantidad });
            }
        }

        evento.Historial.Add(new EventoHistorialEstado
        {
            EstadoAnterior = null,
            EstadoNuevo = EstadoEvento.Programado,
            UsuarioId = usuarioId,
            Comentario = "Evento creado"
        });

        _db.Eventos.Add(evento);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await ObtenerPorIdAsync(evento.Id, null, null))!;
    }

    // Para el admin. Todos los filtros son opcionales; desde/hasta traen los eventos que caen en ese rango
    public async Task<List<EventoDto>> ObtenerTodosAsync(DateTime? desde, DateTime? hasta, string? estado, string? clienteId, int? paqueteId)
    {
        var consulta = ConDetalle();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<EstadoEvento>(estado, ignoreCase: true, out var filtro))
            {
                throw new DatosInvalidosException($"Estado no válido: {estado}.");
            }

            consulta = consulta.Where(e => e.Estado == filtro);
        }

        if (desde is not null)
        {
            consulta = consulta.Where(e => e.FechaFin > desde);
        }

        if (hasta is not null)
        {
            consulta = consulta.Where(e => e.FechaInicio < hasta);
        }

        if (!string.IsNullOrWhiteSpace(clienteId))
        {
            consulta = consulta.Where(e => e.Cotizacion!.Solicitud!.ClienteUsuarioId == clienteId);
        }

        if (paqueteId is not null)
        {
            consulta = consulta.Where(e => e.PaqueteId == paqueteId);
        }

        var eventos = await consulta.OrderBy(e => e.FechaInicio).ToListAsync();
        return eventos.Select(ADto).ToList();
    }

    // Admin: ambos null. Cliente: solo si la solicitud es suya. Trabajador: solo si está asignado. Si no → null (404)
    public async Task<EventoDto?> ObtenerPorIdAsync(int id, string? soloDelClienteId, string? soloDelTrabajadorUsuarioId)
    {
        var consulta = ConDetalle().Where(e => e.Id == id);

        if (soloDelClienteId is not null)
        {
            consulta = consulta.Where(e => e.Cotizacion!.Solicitud!.ClienteUsuarioId == soloDelClienteId);
        }

        if (soloDelTrabajadorUsuarioId is not null)
        {
            consulta = consulta.Where(e => e.Trabajadores.Any(t => t.Empleado!.UsuarioId == soloDelTrabajadorUsuarioId));
        }

        var evento = await consulta.FirstOrDefaultAsync();
        return evento is null ? null : ADto(evento);
    }

    public async Task<List<EventoDto>> ObtenerDeClienteAsync(string clienteUsuarioId)
    {
        var eventos = await ConDetalle()
            .Where(e => e.Cotizacion!.Solicitud!.ClienteUsuarioId == clienteUsuarioId)
            .OrderBy(e => e.FechaInicio)
            .ToListAsync();

        return eventos.Select(ADto).ToList();
    }

    public async Task<List<EventoDto>> ObtenerDeTrabajadorAsync(string trabajadorUsuarioId)
    {
        var eventos = await ConDetalle()
            .Where(e => e.Trabajadores.Any(t => t.Empleado!.UsuarioId == trabajadorUsuarioId))
            .OrderBy(e => e.FechaInicio)
            .ToListAsync();

        return eventos.Select(ADto).ToList();
    }

    // Reemplaza todo el equipo del evento por la lista recibida
    public async Task<EventoDto?> AsignarEquiposAsync(int id, List<EventoEquipoAsignarDto> equipos)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await ReservaLocks.TomarAsync(_db, [$"evento-equipo:{id}"]);
        var evento = await _db.Eventos
            .Include(e => e.Equipos)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (evento is null)
        {
            return null;
        }

        ValidarQueSePuedeModificar(evento);
        await ReservaLocks.TomarAsync(_db, evento.Equipos.Select(e => $"equipo:{e.EquipoId}")
            .Concat(equipos.Select(e => $"equipo:{e.EquipoId}")));

        if (equipos.Select(e => e.EquipoId).Distinct().Count() != equipos.Count)
        {
            throw new DatosInvalidosException("Un mismo equipo aparece repetido en la lista.");
        }

        // Se excluye este mismo evento para no contar dos veces lo que ya tiene reservado
        await ValidarDisponibilidadAsync(equipos, evento.FechaInicio, evento.FechaFin, excluirEventoId: id);

        // Deja la lista igual a la recibida: quita, actualiza y agrega
        evento.Equipos.RemoveAll(ee => !equipos.Any(e => e.EquipoId == ee.EquipoId));

        foreach (var e in equipos)
        {
            var existente = evento.Equipos.FirstOrDefault(ee => ee.EquipoId == e.EquipoId);
            if (existente is null)
            {
                evento.Equipos.Add(new EventoEquipo { EquipoId = e.EquipoId, Cantidad = e.Cantidad });
            }
            else
            {
                existente.Cantidad = e.Cantidad;
            }
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await ObtenerPorIdAsync(id, null, null);
    }

    // Reemplaza todos los trabajadores del evento por la lista recibida
    public async Task<EventoDto?> AsignarTrabajadoresAsync(int id, List<EventoTrabajadorAsignarDto> trabajadores)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await ReservaLocks.TomarAsync(_db, [$"evento-trabajador:{id}"]);
        var evento = await _db.Eventos
            .Include(e => e.Trabajadores)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (evento is null)
        {
            return null;
        }

        ValidarQueSePuedeModificar(evento);
        await ReservaLocks.TomarAsync(_db, evento.Trabajadores.Select(t => $"empleado:{t.EmpleadoId}")
            .Concat(trabajadores.Select(t => $"empleado:{t.EmpleadoId}")));

        var ids = trabajadores.Select(t => t.EmpleadoId).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            throw new DatosInvalidosException("Un mismo empleado aparece repetido en la lista.");
        }

        var empleados = await _db.Empleados
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id);

        foreach (var empleadoId in ids)
        {
            if (!empleados.TryGetValue(empleadoId, out var empleado) || !empleado.Activo)
            {
                throw new DatosInvalidosException($"No existe un empleado activo con Id {empleadoId}.");
            }
        }

        // ¿Alguno ya está en otro evento (no cancelado) que se cruza en horario?
        var ocupado = await _db.EventosTrabajadores
            .Where(t => ids.Contains(t.EmpleadoId)
                && t.EventoId != id
                && t.Evento!.Estado != EstadoEvento.Cancelado
                && t.Evento.FechaInicio < evento.FechaFin
                && evento.FechaInicio < t.Evento.FechaFin)
            .Select(t => t.EmpleadoId)
            .FirstOrDefaultAsync();

        if (ocupado != 0)
        {
            throw new DatosInvalidosException(
                $"{empleados[ocupado].NombreCompleto} ya está asignado a otro evento en ese horario.");
        }

        evento.Trabajadores.RemoveAll(t => !ids.Contains(t.EmpleadoId));

        foreach (var t in trabajadores)
        {
            var existente = evento.Trabajadores.FirstOrDefault(x => x.EmpleadoId == t.EmpleadoId);
            if (existente is null)
            {
                evento.Trabajadores.Add(new EventoTrabajador { EmpleadoId = t.EmpleadoId, Funcion = t.Funcion });
            }
            else
            {
                existente.Funcion = t.Funcion;
            }
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await ObtenerPorIdAsync(id, null, null);
    }

    // Solo avanza al siguiente estado: Programado → EnMontaje → EnServicio → Finalizado
    public async Task<EventoDto?> CambiarEstadoAsync(int id, EventoCambiarEstadoDto dto, string usuarioId, string? soloDelTrabajadorUsuarioId)
    {
        var evento = await _db.Eventos
            .Include(e => e.Trabajadores)
                .ThenInclude(t => t.Empleado)
            .FirstOrDefaultAsync(e => e.Id == id);

        // Un trabajador que no está asignado no "ve" el evento (404)
        if (evento is null ||
            (soloDelTrabajadorUsuarioId is not null &&
             !evento.Trabajadores.Any(t => t.Empleado!.UsuarioId == soloDelTrabajadorUsuarioId)))
        {
            return null;
        }

        if (!Enum.TryParse<EstadoEvento>(dto.Estado, ignoreCase: true, out var nuevo))
        {
            throw new DatosInvalidosException($"Estado no válido: {dto.Estado}.");
        }

        if (nuevo == EstadoEvento.Cancelado)
        {
            throw new DatosInvalidosException("Para cancelar use PATCH api/eventos/{id}/cancelar.");
        }

        if (evento.Estado is EstadoEvento.Finalizado or EstadoEvento.Cancelado)
        {
            throw new DatosInvalidosException($"El evento está {evento.Estado} y ya no cambia de estado.");
        }

        var siguiente = evento.Estado + 1;
        if (nuevo != siguiente)
        {
            throw new DatosInvalidosException($"Desde {evento.Estado} solo se puede pasar a {siguiente}.");
        }

        RegistrarCambio(evento, nuevo, usuarioId, dto.Comentario);
        await _db.SaveChangesAsync();

        return await ObtenerPorIdAsync(id, null, null);
    }

    public async Task<EventoDto?> CancelarAsync(int id, string motivo, string usuarioId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await ReservaLocks.TomarAsync(_db, [$"evento-equipo:{id}", $"evento-trabajador:{id}"]);
        var evento = await _db.Eventos
            .Include(e => e.Equipos)
            .Include(e => e.Trabajadores)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (evento is null)
        {
            return null;
        }

        if (evento.Estado is EstadoEvento.Finalizado or EstadoEvento.Cancelado)
        {
            throw new DatosInvalidosException($"El evento está {evento.Estado} y no se puede cancelar.");
        }

        await ReservaLocks.TomarAsync(_db, evento.Equipos.Select(e => $"equipo:{e.EquipoId}")
            .Concat(evento.Trabajadores.Select(t => $"empleado:{t.EmpleadoId}")));

        evento.MotivoCancelacion = motivo.Trim();
        RegistrarCambio(evento, EstadoEvento.Cancelado, usuarioId, motivo.Trim());
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await ObtenerPorIdAsync(id, null, null);
    }

    // Cuánto hay de cada equipo activo entre dos fechas
    public async Task<List<DisponibilidadEquipoDto>> ObtenerDisponibilidadAsync(DateTime desde, DateTime hasta)
    {
        if (desde >= hasta)
        {
            throw new DatosInvalidosException("La fecha 'desde' debe ser anterior a 'hasta'.");
        }

        var equipos = await _db.Equipos
            .Where(e => e.Activo)
            .OrderBy(e => e.Nombre)
            .ToListAsync();

        var resultados = new List<DisponibilidadEquipoDto>();
        foreach (var e in equipos)
        {
            var reservada = await MaximoReservadoAsync(_db, e.Id, desde, hasta, null);
            resultados.Add(new DisponibilidadEquipoDto
            {
                EquipoId = e.Id,
                Nombre = e.Nombre,
                CantidadTotal = e.CantidadTotal,
                Reservada = reservada,
                Disponible = Math.Max(0, e.CantidadTotal - reservada)
            });
        }
        return resultados;
    }

    // Barrido de extremos: en el mismo instante se liberan primero las reservas que terminan.
    internal static async Task<int> MaximoReservadoAsync(ApplicationDbContext db, int equipoId, DateTime inicio, DateTime fin, int? excluirEventoId)
    {
        var reservas = await db.EventosEquipos
            .Where(ee => ee.Evento!.Estado != EstadoEvento.Cancelado
                && ee.EquipoId == equipoId
                && (excluirEventoId == null || ee.EventoId != excluirEventoId)
                && ee.Evento.FechaInicio < fin
                && inicio < ee.Evento.FechaFin)
            .Select(ee => new { ee.Cantidad, ee.Evento!.FechaInicio, ee.Evento.FechaFin })
            .ToListAsync();
        var extremos = reservas.SelectMany(r => new[]
        {
            (Fecha: r.FechaInicio < inicio ? inicio : r.FechaInicio, Cambio: r.Cantidad),
            (Fecha: r.FechaFin > fin ? fin : r.FechaFin, Cambio: -r.Cantidad)
        }).OrderBy(x => x.Fecha).ThenBy(x => x.Cambio);
        var actual = 0;
        var maximo = 0;
        foreach (var extremo in extremos)
        {
            actual += extremo.Cambio;
            maximo = Math.Max(maximo, actual);
        }
        return maximo;
    }

    // Revisa que cada equipo exista, esté activo y alcance en esas fechas
    private async Task ValidarDisponibilidadAsync(List<EventoEquipoAsignarDto> pedidos, DateTime inicio, DateTime fin, int? excluirEventoId)
    {
        if (pedidos.Count == 0)
        {
            return;
        }

        var ids = pedidos.Select(p => p.EquipoId).ToList();
        var equipos = await _db.Equipos
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id);

        foreach (var p in pedidos)
        {
            if (!equipos.TryGetValue(p.EquipoId, out var equipo) || !equipo.Activo)
            {
                throw new DatosInvalidosException($"No existe un equipo activo con Id {p.EquipoId}.");
            }

            var ocupado = await MaximoReservadoAsync(_db, p.EquipoId, inicio, fin, excluirEventoId);
            if (ocupado + p.Cantidad > equipo.CantidadTotal)
            {
                var libre = Math.Max(0, equipo.CantidadTotal - ocupado);
                throw new DatosInvalidosException(
                    $"No hay suficiente '{equipo.Nombre}' en esas fechas: se piden {p.Cantidad} y quedan {libre} de {equipo.CantidadTotal}.");
            }
        }
    }

    private static void ValidarQueSePuedeModificar(Evento evento)
    {
        if (evento.Estado is EstadoEvento.Finalizado or EstadoEvento.Cancelado)
        {
            throw new DatosInvalidosException($"El evento está {evento.Estado} y ya no se puede modificar.");
        }
    }

    // Cambia el estado y deja el registro en el historial
    private static void RegistrarCambio(Evento evento, EstadoEvento nuevo, string usuarioId, string? comentario)
    {
        evento.Historial.Add(new EventoHistorialEstado
        {
            EstadoAnterior = evento.Estado,
            EstadoNuevo = nuevo,
            UsuarioId = usuarioId,
            Comentario = comentario
        });

        evento.Estado = nuevo;
    }

    private static EventoDto ADto(Evento e) => new()
    {
        Id = e.Id,
        CotizacionId = e.CotizacionId,
        SolicitudId = e.Cotizacion?.SolicitudId ?? 0,
        NombreContacto = e.Cotizacion?.Solicitud?.NombreContacto ?? string.Empty,
        PaqueteId = e.PaqueteId,
        PaqueteNombre = e.Paquete?.Nombre,
        FechaInicio = e.FechaInicio,
        FechaFin = e.FechaFin,
        Lugar = e.Lugar,
        Estado = e.Estado.ToString(),
        MotivoCancelacion = e.MotivoCancelacion,
        Notas = e.Notas,
        FechaCreacion = e.FechaCreacion,
        Equipos = e.Equipos
            .OrderBy(x => x.Equipo?.Nombre)
            .Select(x => new EventoEquipoDto
            {
                EquipoId = x.EquipoId,
                EquipoNombre = x.Equipo?.Nombre ?? string.Empty,
                Cantidad = x.Cantidad
            })
            .ToList(),
        Trabajadores = e.Trabajadores
            .OrderBy(t => t.Empleado?.NombreCompleto)
            .Select(t => new EventoTrabajadorDto
            {
                EmpleadoId = t.EmpleadoId,
                EmpleadoNombre = t.Empleado?.NombreCompleto ?? string.Empty,
                Puesto = t.Empleado?.Puesto ?? string.Empty,
                Funcion = t.Funcion
            })
            .ToList(),
        Historial = e.Historial
            .OrderBy(h => h.Fecha)
            .ThenBy(h => h.Id)
            .Select(h => new EventoHistorialDto
            {
                EstadoAnterior = h.EstadoAnterior?.ToString(),
                EstadoNuevo = h.EstadoNuevo.ToString(),
                UsuarioId = h.UsuarioId,
                Fecha = h.Fecha,
                Comentario = h.Comentario
            })
            .ToList()
    };
}
