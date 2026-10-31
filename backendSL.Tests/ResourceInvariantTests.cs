using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Collections.Concurrent;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.DTOs.Eventos;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class ResourceInvariantTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;
    public ResourceInvariantTests(BackendTestFactory factory) => _factory = factory;

    [Fact]
    public async Task DisponibilidadUsaPicoSimultaneoYPermiteReservarEnHueco()
    {
        await using var db = _factory.CrearDbContextDirecto();
        var (equipo, eventos) = await PrepararAsync(db, 5, 3);
        var dia = DateTime.UtcNow.Date.AddDays(50);
        eventos[0].FechaInicio = dia.AddHours(8);
        eventos[0].FechaFin = dia.AddHours(10);
        eventos[1].FechaInicio = dia.AddHours(12);
        eventos[1].FechaFin = dia.AddHours(14);
        eventos[2].FechaInicio = dia.AddHours(9);
        eventos[2].FechaFin = dia.AddHours(13);
        eventos[0].Equipos.Add(new EventoEquipo { EquipoId = equipo.Id, Cantidad = 3 });
        eventos[1].Equipos.Add(new EventoEquipo { EquipoId = equipo.Id, Cantidad = 4 });
        await db.SaveChangesAsync();

        var service = new EventoService(db);
        var disponible = Assert.Single(await service.ObtenerDisponibilidadAsync(dia.AddHours(8), dia.AddHours(14)), x => x.EquipoId == equipo.Id);
        Assert.Equal(4, disponible.Reservada);
        Assert.Equal(1, disponible.Disponible);
        var asignado = await service.AsignarEquiposAsync(eventos[2].Id, [new() { EquipoId = equipo.Id, Cantidad = 1 }]);
        Assert.NotNull(asignado);
        Assert.Equal(1, Assert.Single(asignado.Equipos).Cantidad);
    }

    [Fact]
    public async Task ReduccionDeInventarioRespetaPicoDeReservas()
    {
        await using var db = _factory.CrearDbContextDirecto();
        var (equipo, eventos) = await PrepararAsync(db, 5, 1);
        eventos[0].Equipos.Add(new EventoEquipo { EquipoId = equipo.Id, Cantidad = 4 });
        await db.SaveChangesAsync();
        var service = new EquipoService(db);
        await Assert.ThrowsAsync<DatosInvalidosException>(() => service.ActualizarAsync(equipo.Id,
            new EquipoGuardarDto { Nombre = equipo.Nombre, CantidadTotal = 3, Activo = true }));
        db.ChangeTracker.Clear();
        Assert.Equal(5, (await db.Equipos.FindAsync(equipo.Id))!.CantidadTotal);
        var compatible = await service.ActualizarAsync(equipo.Id,
            new EquipoGuardarDto { Nombre = equipo.Nombre, CantidadTotal = 4, Activo = true });
        Assert.Equal(4, compatible!.CantidadTotal);
        Assert.Equal(4, (await new EventoService(db).AsignarEquiposAsync(eventos[0].Id,
            [new() { EquipoId = equipo.Id, Cantidad = 4 }]))!.Equipos.Single().Cantidad);
    }

    [Fact]
    public async Task ReservaPasadaNoImpideReducirPeroReservaFuturaSi()
    {
        await using var db = _factory.CrearDbContextDirecto();
        var (equipo, eventos) = await PrepararAsync(db, 5, 2);
        eventos[0].FechaInicio = DateTime.Now.AddDays(-5);
        eventos[0].FechaFin = DateTime.Now.AddDays(-4);
        eventos[0].Equipos.Add(new EventoEquipo { EquipoId = equipo.Id, Cantidad = 5 });
        eventos[1].FechaInicio = DateTime.Now.AddDays(5);
        eventos[1].FechaFin = DateTime.Now.AddDays(6);
        eventos[1].Equipos.Add(new EventoEquipo { EquipoId = equipo.Id, Cantidad = 2 });
        await db.SaveChangesAsync();
        var service = new EquipoService(db);
        var actualizado = await service.ActualizarAsync(equipo.Id,
            new EquipoGuardarDto { Nombre = equipo.Nombre, CantidadTotal = 2, Activo = true });
        Assert.Equal(2, actualizado!.CantidadTotal);
        await Assert.ThrowsAsync<DatosInvalidosException>(() => service.ActualizarAsync(equipo.Id,
            new EquipoGuardarDto { Nombre = equipo.Nombre, CantidadTotal = 1, Activo = true }));
        db.ChangeTracker.Clear();
        Assert.Equal(2, (await db.Equipos.FindAsync(equipo.Id))!.CantidadTotal);
    }

    [Fact]
    public async Task DesactivarConservaReservaAnteriorYRechazaAsignacionNueva()
    {
        await using var seed = _factory.CrearDbContextDirecto();
        var (equipo, eventos) = await PrepararAsync(seed, 2, 2);
        await new EventoService(seed).AsignarEquiposAsync(eventos[0].Id,
            [new() { EquipoId = equipo.Id, Cantidad = 1 }]);

        await using var dbDesactivar = _factory.CrearDbContextDirecto();
        Assert.True(await new EquipoService(dbDesactivar).DesactivarAsync(equipo.Id));

        await using var dbAsignar = _factory.CrearDbContextDirecto();
        await Assert.ThrowsAsync<DatosInvalidosException>(() => new EventoService(dbAsignar)
            .AsignarEquiposAsync(eventos[1].Id, [new() { EquipoId = equipo.Id, Cantidad = 1 }]));
        await using var verificar = _factory.CrearDbContextDirecto();
        Assert.False((await verificar.Equipos.FindAsync(equipo.Id))!.Activo);
        Assert.Equal(1, await verificar.EventosEquipos.CountAsync(x => x.EquipoId == equipo.Id && x.EventoId == eventos[0].Id));
        Assert.False(await verificar.EventosEquipos.AnyAsync(x => x.EquipoId == equipo.Id && x.EventoId == eventos[1].Id));
    }

    [Fact]
    public async Task CopiaDePaqueteFallaCompletaSiUnEquipoEstaInactivo()
    {
        await using var db = _factory.CrearDbContextDirecto();
        var (equipo, _) = await PrepararAsync(db, 3, 0);
        equipo.Activo = false;
        var servicio = new Servicio { Nombre = $"Servicio {Guid.NewGuid():N}" };
        var paquete = new Paquete { Servicio = servicio, Nombre = "Paquete", Equipos = [new PaqueteEquipo { Equipo = equipo, Cantidad = 1 }] };
        db.Paquetes.Add(paquete);
        var usuarioId = await db.Users.Select(u => u.Id).FirstAsync();
        var solicitud = new Solicitud { Paquete = paquete, NombreContacto = "Prueba", CorreoContacto = "prueba@example.test" };
        var cotizacion = new Cotizacion { Solicitud = solicitud, Estado = EstadoCotizacion.Aceptada, CreadaPorUsuarioId = usuarioId };
        db.Cotizaciones.Add(cotizacion);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<DatosInvalidosException>(() => new EventoService(db).CrearAsync(
            new EventoCrearDto { CotizacionId = cotizacion.Id, FechaInicio = DateTime.UtcNow.Date.AddDays(60), FechaFin = DateTime.UtcNow.Date.AddDays(61), CopiarEquipoDelPaquete = true }, usuarioId));
        Assert.False(await db.Eventos.AnyAsync(e => e.CotizacionId == cotizacion.Id));
        Assert.False(await db.EventosHistorialEstados.AnyAsync(h => h.Evento!.CotizacionId == cotizacion.Id));
        var sinCopia = await new EventoService(db).CrearAsync(
            new EventoCrearDto { CotizacionId = cotizacion.Id, FechaInicio = DateTime.UtcNow.Date.AddDays(60), FechaFin = DateTime.UtcNow.Date.AddDays(61), CopiarEquipoDelPaquete = false }, usuarioId);
        Assert.Empty(sinCopia.Equipos);
        db.ChangeTracker.Clear();
        Assert.True(await db.Eventos.AnyAsync(e => e.Id == sinCopia.Id));
        Assert.False((await db.Equipos.FindAsync(equipo.Id))!.Activo);
    }

    [Fact]
    public async Task ContextosDistintosNoSobreReservanEquipoNiTrabajador()
    {
        await using var seed = _factory.CrearDbContextDirecto();
        var (equipo, eventos) = await PrepararAsync(seed, 1, 4);
        var empleado = new Empleado { Cedula = Guid.NewGuid().ToString("N")[..20], NombreCompleto = "Operador" };
        seed.Empleados.Add(empleado);
        await seed.SaveChangesAsync();
        var barreraEquipo = new BarreraDeBloqueo();
        await using var db1 = CrearContextoConBarrera(barreraEquipo);
        await using var db2 = CrearContextoConBarrera(barreraEquipo);
        var resultadosEquipo = await Task.WhenAll(
            IntentarAsync(() => new EventoService(db1).AsignarEquiposAsync(eventos[0].Id, [new() { EquipoId = equipo.Id, Cantidad = 1 }])),
            IntentarAsync(() => new EventoService(db2).AsignarEquiposAsync(eventos[1].Id, [new() { EquipoId = equipo.Id, Cantidad = 1 }])));
        Assert.Equal(1, resultadosEquipo.Count(x => x));
        var barreraTrabajador = new BarreraDeBloqueo();
        await using var db3 = CrearContextoConBarrera(barreraTrabajador);
        await using var db4 = CrearContextoConBarrera(barreraTrabajador);
        var resultadosTrabajador = await Task.WhenAll(
            IntentarAsync(() => new EventoService(db3).AsignarTrabajadoresAsync(eventos[2].Id, [new() { EmpleadoId = empleado.Id }])),
            IntentarAsync(() => new EventoService(db4).AsignarTrabajadoresAsync(eventos[3].Id, [new() { EmpleadoId = empleado.Id }])));
        Assert.Equal(1, resultadosTrabajador.Count(x => x));
        await using var verificar = _factory.CrearDbContextDirecto();
        Assert.Equal(1, await verificar.EventosEquipos.CountAsync(e => e.EquipoId == equipo.Id));
        Assert.Equal(1, await verificar.EventosTrabajadores.CountAsync(t => t.EmpleadoId == empleado.Id));
    }

    [Fact]
    public async Task ReduccionYAsignacionConcurrentesConservanStock()
    {
        await using var seed = _factory.CrearDbContextDirecto();
        var (equipo, eventos) = await PrepararAsync(seed, 2, 1);
        var barrera = new BarreraDeBloqueo();
        await using var dbAsignacion = CrearContextoConBarrera(barrera);
        await using var dbInventario = CrearContextoConBarrera(barrera);
        var resultados = await Task.WhenAll(
            IntentarAsync(async () => { await new EventoService(dbAsignacion).AsignarEquiposAsync(eventos[0].Id, [new() { EquipoId = equipo.Id, Cantidad = 2 }]); }),
            IntentarAsync(async () => { await new EquipoService(dbInventario).ActualizarAsync(equipo.Id,
                new EquipoGuardarDto { Nombre = equipo.Nombre, CantidadTotal = 1, Activo = true }); }));
        Assert.Equal(1, resultados.Count(x => x));
        await using var verificar = _factory.CrearDbContextDirecto();
        var stock = (await verificar.Equipos.FindAsync(equipo.Id))!.CantidadTotal;
        var reservado = await verificar.EventosEquipos.Where(x => x.EquipoId == equipo.Id).SumAsync(x => (int?)x.Cantidad) ?? 0;
        Assert.True(reservado <= stock);
    }

    [Fact]
    public async Task CreacionConCopiaYAsignacionConcurrentesNoSobreReservan()
    {
        await using var seed = _factory.CrearDbContextDirecto();
        var (equipo, eventos) = await PrepararAsync(seed, 1, 1);
        var servicio = new Servicio { Nombre = $"Servicio {Guid.NewGuid():N}" };
        var paquete = new Paquete { Servicio = servicio, Nombre = "Paquete", Equipos = [new PaqueteEquipo { EquipoId = equipo.Id, Cantidad = 1 }] };
        var usuarioId = await seed.Users.Select(u => u.Id).FirstAsync();
        var cotizacion = new Cotizacion
        {
            Solicitud = new Solicitud { Paquete = paquete, NombreContacto = "Prueba", CorreoContacto = "prueba@example.test" },
            Estado = EstadoCotizacion.Aceptada,
            CreadaPorUsuarioId = usuarioId
        };
        seed.Cotizaciones.Add(cotizacion);
        await seed.SaveChangesAsync();
        var barrera = new BarreraDeBloqueo();
        await using var dbCreacion = CrearContextoConBarrera(barrera);
        await using var dbAsignacion = CrearContextoConBarrera(barrera);
        var resultados = await Task.WhenAll(
            IntentarAsync(async () => { await new EventoService(dbCreacion).CrearAsync(new EventoCrearDto
            {
                CotizacionId = cotizacion.Id,
                FechaInicio = eventos[0].FechaInicio,
                FechaFin = eventos[0].FechaFin,
                CopiarEquipoDelPaquete = true
            }, usuarioId); }),
            IntentarAsync(async () => { await new EventoService(dbAsignacion).AsignarEquiposAsync(eventos[0].Id,
                [new() { EquipoId = equipo.Id, Cantidad = 1 }]); }));
        Assert.Equal(1, resultados.Count(x => x));
        await using var verificar = _factory.CrearDbContextDirecto();
        Assert.Equal(1, await verificar.EventosEquipos.CountAsync(x => x.EquipoId == equipo.Id));
    }

    [Fact]
    public async Task ReemplazoLiberaEquipoYTrabajadorParaOtroEvento()
    {
        await using var db = _factory.CrearDbContextDirecto();
        var (equipo, eventos) = await PrepararAsync(db, 1, 2);
        var otro = new Equipo { Nombre = $"Otro {Guid.NewGuid():N}", CantidadTotal = 1 };
        var empleado = new Empleado { Cedula = Guid.NewGuid().ToString("N")[..20], NombreCompleto = "Operador" };
        db.AddRange(otro, empleado);
        await db.SaveChangesAsync();
        var service = new EventoService(db);
        await service.AsignarEquiposAsync(eventos[0].Id, [new() { EquipoId = equipo.Id, Cantidad = 1 }]);
        await service.AsignarTrabajadoresAsync(eventos[0].Id, [new() { EmpleadoId = empleado.Id }]);
        await service.AsignarEquiposAsync(eventos[0].Id, [new() { EquipoId = otro.Id, Cantidad = 1 }]);
        await service.AsignarTrabajadoresAsync(eventos[0].Id, []);
        await service.AsignarEquiposAsync(eventos[1].Id, [new() { EquipoId = equipo.Id, Cantidad = 1 }]);
        await service.AsignarTrabajadoresAsync(eventos[1].Id, [new() { EmpleadoId = empleado.Id }]);
        Assert.Equal(1, await db.EventosEquipos.CountAsync(x => x.EquipoId == equipo.Id));
        Assert.Equal(1, await db.EventosTrabajadores.CountAsync(x => x.EmpleadoId == empleado.Id));
    }

    private static async Task<bool> IntentarAsync(Func<Task> accion)
    {
        try { await accion(); return true; }
        catch (DatosInvalidosException) { return false; }
    }

    private static async Task<bool> IntentarAsync(Func<Task<EventoDto?>> accion)
    {
        try { return await accion() is not null; }
        catch (DatosInvalidosException) { return false; }
    }

    private ApplicationDbContext CrearContextoConBarrera(BarreraDeBloqueo barrera)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_factory.ConnectionString)
            .AddInterceptors(barrera)
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class BarreraDeBloqueo : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _ambosLlegaron = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentDictionary<DbConnection, byte> _conexionesConBloqueo = new();
        private int _llegadas;

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("sp_getapplock", StringComparison.Ordinal))
            {
                _conexionesConBloqueo.TryAdd(command.Connection!, 0);
            }
            return ValueTask.FromResult(result);
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            // Esta consulta existe también en la implementación antigua. Solo allí faltaba el bloqueo:
            // ambas lecturas llegan aquí antes de cualquiera de las dos escrituras.
            if ((command.CommandText.Contains("[EventosEquipos]", StringComparison.Ordinal) ||
                 command.CommandText.Contains("[EventosTrabajadores]", StringComparison.Ordinal)) &&
                !_conexionesConBloqueo.ContainsKey(command.Connection!))
            {
                if (Interlocked.Increment(ref _llegadas) == 2) _ambosLlegaron.TrySetResult();
                await _ambosLlegaron.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }

    private static async Task<(Equipo Equipo, List<Evento> Eventos)> PrepararAsync(ApplicationDbContext db, int stock, int cantidadEventos)
    {
        var equipo = new Equipo { Nombre = $"Equipo {Guid.NewGuid():N}", CantidadTotal = stock };
        db.Equipos.Add(equipo);
        var usuarioId = await db.Users.Select(u => u.Id).FirstAsync();
        var eventos = new List<Evento>();
        for (var i = 0; i < cantidadEventos; i++)
        {
            var solicitud = new Solicitud { NombreContacto = "Prueba", CorreoContacto = "prueba@example.test" };
            var cotizacion = new Cotizacion { Solicitud = solicitud, Estado = EstadoCotizacion.Aceptada, CreadaPorUsuarioId = usuarioId };
            eventos.Add(new Evento { Cotizacion = cotizacion, FechaInicio = DateTime.UtcNow.Date.AddDays(30), FechaFin = DateTime.UtcNow.Date.AddDays(31) });
        }
        db.Eventos.AddRange(eventos);
        await db.SaveChangesAsync();
        return (equipo, eventos);
    }
}
