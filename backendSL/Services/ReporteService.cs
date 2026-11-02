using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface IReporteService
{
    Task<FidelidadDto?> ObtenerFidelidadAsync(int solicitudId);
    Task<ResumenDto> ObtenerResumenAsync(int? anio, int? mes);
}

public class ReporteService : IReporteService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguracionService _configuracion;

    public ReporteService(ApplicationDbContext db, IConfiguracionService configuracion)
    {
        _db = db;
        _configuracion = configuracion;
    }

    // ¿El cliente de esta solicitud ya tiene suficientes eventos finalizados para el descuento?
    public async Task<FidelidadDto?> ObtenerFidelidadAsync(int solicitudId)
    {
        var solicitud = await _db.Solicitudes
            .Include(s => s.Paquete)
            .FirstOrDefaultAsync(s => s.Id == solicitudId);
        if (solicitud is null)
        {
            return null;
        }

        var minimos = await _configuracion.ObtenerEnteroAsync(ConfiguracionService.EventosMinimos, 3);
        var porcentaje = await _configuracion.ObtenerEnteroAsync(ConfiguracionService.PorcentajeDescuento, 10);

        // Un visitante sin cuenta no acumula eventos
        var finalizados = solicitud.ClienteUsuarioId is null
            ? 0
            : await _db.Eventos.CountAsync(e =>
                e.Estado == EstadoEvento.Finalizado &&
                e.Cotizacion!.Solicitud!.ClienteUsuarioId == solicitud.ClienteUsuarioId);

        var aplica = solicitud.ClienteUsuarioId is not null && finalizados >= minimos;
        var precio = solicitud.Paquete?.PrecioBase;

        string? promoNombre = null;
        decimal promoPorcentaje = 0m;
        decimal? promoDescuentoSugerido = null;

        if (solicitud.PaqueteId is int paqueteId && precio is decimal precioBase)
        {
            var ahoraUtc = DateTime.UtcNow;
            var ahoraLocal = DateTime.Now;
            var fechaEvento = solicitud.FechaEvento;

            var promo = await _db.PromocionesTemporada
                .AsNoTracking()
                .Where(p => p.Activo &&
                    ((p.FechaInicio <= fechaEvento && p.FechaFin >= fechaEvento) ||
                     (p.FechaInicio <= ahoraUtc && p.FechaFin >= ahoraUtc) ||
                     (p.FechaInicio <= ahoraLocal && p.FechaFin >= ahoraLocal)) &&
                    p.Paquetes.Any(pp => pp.PaqueteId == paqueteId))
                .OrderByDescending(p => p.PorcentajeDescuento)
                .ThenByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            if (promo is not null)
            {
                promoNombre = promo.Nombre;
                promoPorcentaje = promo.PorcentajeDescuento;
                promoDescuentoSugerido = Math.Round(precioBase * promo.PorcentajeDescuento / 100m, 2);
            }
        }

        return new FidelidadDto
        {
            SolicitudId = solicitud.Id,
            ClienteUsuarioId = solicitud.ClienteUsuarioId,
            EventosFinalizados = finalizados,
            EventosMinimos = minimos,
            PorcentajeDescuento = porcentaje,
            Aplica = aplica,
            PrecioBase = precio,
            DescuentoSugerido = precio is null ? null : (aplica ? Math.Round(precio.Value * porcentaje / 100m, 2) : 0m),
            PromocionTemporadaNombre = promoNombre,
            PorcentajePromocionTemporada = promoPorcentaje,
            DescuentoPromocionSugerido = promoDescuentoSugerido
        };
    }

    // Resumen de un mes (por defecto el actual)
    public async Task<ResumenDto> ObtenerResumenAsync(int? anio, int? mes)
    {
        var hoy = DateTime.Now;
        var a = anio ?? hoy.Year;
        var m = mes ?? hoy.Month;

        if (a < 2000 || a > 2100 || m < 1 || m > 12)
        {
            throw new DatosInvalidosException("Año o mes no válido.");
        }

        var inicio = new DateTime(a, m, 1);
        var fin = inicio.AddMonths(1);

        var eventosDelMes = _db.Eventos.Where(e => e.FechaInicio >= inicio && e.FechaInicio < fin);

        // Eventos del mes por estado (incluye los estados en 0)
        var porEstado = await eventosDelMes
            .GroupBy(e => e.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Estado, x => x.Cantidad);

        // Ingresos: lo que pagan (Monto - Descuento) los eventos finalizados del mes
        var ingresos = await eventosDelMes
            .Where(e => e.Estado == EstadoEvento.Finalizado)
            .SumAsync(e => (decimal?)(e.Cotizacion!.Monto - e.Cotizacion.Descuento)) ?? 0m;

        // Paquetes más contratados en el mes (sin contar cancelados)
        var topPaquetes = await eventosDelMes
            .Where(e => e.Estado != EstadoEvento.Cancelado && e.PaqueteId != null)
            .GroupBy(e => e.Paquete!.Nombre)
            .Select(g => new ConteoDto { Nombre = g.Key, Cantidad = g.Count() })
            .OrderByDescending(x => x.Cantidad)
            .ThenBy(x => x.Nombre)
            .Take(5)
            .ToListAsync();

        // Clientes con más eventos finalizados (de todos los tiempos)
        var topIds = await _db.Eventos
            .Where(e => e.Estado == EstadoEvento.Finalizado && e.Cotizacion!.Solicitud!.ClienteUsuarioId != null)
            .GroupBy(e => e.Cotizacion!.Solicitud!.ClienteUsuarioId!)
            .Select(g => new { ClienteId = g.Key, Cantidad = g.Count() })
            .OrderByDescending(x => x.Cantidad)
            .Take(5)
            .ToListAsync();

        var ids = topIds.Select(x => x.ClienteId).ToList();
        var usuarios = await _db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var topClientes = topIds.Select(x => new ClienteFrecuenteDto
        {
            ClienteUsuarioId = x.ClienteId,
            NombreCompleto = usuarios.GetValueOrDefault(x.ClienteId)?.NombreCompleto ?? string.Empty,
            Email = usuarios.GetValueOrDefault(x.ClienteId)?.Email ?? string.Empty,
            EventosFinalizados = x.Cantidad
        }).ToList();

        var pendientes = await _db.Solicitudes.CountAsync(s => s.Estado == EstadoSolicitud.Pendiente);

        return new ResumenDto
        {
            Anio = a,
            Mes = m,
            EventosPorEstado = Enum.GetValues<EstadoEvento>()
                .Select(e => new ConteoDto { Nombre = e.ToString(), Cantidad = porEstado.GetValueOrDefault(e) })
                .ToList(),
            Ingresos = ingresos,
            TopPaquetes = topPaquetes,
            TopClientes = topClientes,
            SolicitudesPendientes = pendientes
        };
    }
}
