using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Services;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class CotizacionConcurrencyTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public CotizacionConcurrencyTests(BackendTestFactory factory) => _factory = factory;

    [Fact]
    public async Task CrearTrasAceptacion_ConSolicitudPrecargada_NoInsertaCotizacionTardia()
    {
        var (solicitudId, primeraId, _) = await PrepararAsync();
        await using var creadorDb = _factory.CrearDbContextDirecto();
        await using var aceptadorDb = _factory.CrearDbContextDirecto();
        var creador = new CotizacionService(creadorDb);
        var aceptador = new CotizacionService(aceptadorDb);

        // El contexto creador conserva el estado anterior a la aceptación.
        await creadorDb.Solicitudes.SingleAsync(s => s.Id == solicitudId);
        var aceptada = await aceptador.AceptarAsync(primeraId);
        Assert.Equal(nameof(EstadoCotizacion.Aceptada), aceptada!.Estado);

        var usuarioId = await ObtenerAdminIdAsync(creadorDb);
        await Assert.ThrowsAsync<DatosInvalidosException>(() => creador.CrearAsync(
            solicitudId, NuevaCotizacion(), usuarioId));

        await using var verificador = _factory.CrearDbContextDirecto();
        var solicitud = await verificador.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId);
        var cotizaciones = await verificador.Cotizaciones.AsNoTracking()
            .Where(c => c.SolicitudId == solicitudId).ToListAsync();
        Assert.Equal(EstadoSolicitud.Aceptada, solicitud.Estado);
        Assert.Equal(2, cotizaciones.Count);
        Assert.Single(cotizaciones, c => c.Estado == EstadoCotizacion.Aceptada);
        Assert.DoesNotContain(cotizaciones, c => c.Estado == EstadoCotizacion.Enviada);
    }

    [Fact]
    public async Task RechazarTrasAceptacion_ConCotizacionPrecargada_NoSobrescribeAceptacion()
    {
        var (solicitudId, primeraId, _) = await PrepararAsync();
        await using var rechazadorDb = _factory.CrearDbContextDirecto();
        await using var aceptadorDb = _factory.CrearDbContextDirecto();
        await rechazadorDb.Cotizaciones.Include(c => c.Solicitud)
            .SingleAsync(c => c.Id == primeraId);

        await new CotizacionService(aceptadorDb).AceptarAsync(primeraId);
        await Assert.ThrowsAsync<DatosInvalidosException>(() =>
            new CotizacionService(rechazadorDb).RechazarAsync(primeraId, null));

        await using var verificador = _factory.CrearDbContextDirecto();
        Assert.Equal(EstadoSolicitud.Aceptada,
            (await verificador.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).Estado);
        Assert.Equal(EstadoCotizacion.Aceptada,
            (await verificador.Cotizaciones.AsNoTracking().SingleAsync(c => c.Id == primeraId)).Estado);
    }

    [Fact]
    public async Task DosAceptaciones_ConGrafosPrecargados_SoloUnaPuedeConfirmarse()
    {
        var (solicitudId, primeraId, segundaId) = await PrepararAsync();
        await using var primeroDb = _factory.CrearDbContextDirecto();
        await using var segundoDb = _factory.CrearDbContextDirecto();
        await PrecargarGrafoAsync(primeroDb, primeraId);
        await PrecargarGrafoAsync(segundoDb, segundaId);

        await new CotizacionService(primeroDb).AceptarAsync(primeraId);
        await Assert.ThrowsAsync<DatosInvalidosException>(() =>
            new CotizacionService(segundoDb).AceptarAsync(segundaId));

        await using var verificador = _factory.CrearDbContextDirecto();
        var solicitud = await verificador.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId);
        var cotizaciones = await verificador.Cotizaciones.AsNoTracking()
            .Where(c => c.SolicitudId == solicitudId).ToListAsync();
        Assert.Equal(EstadoSolicitud.Aceptada, solicitud.Estado);
        Assert.Single(cotizaciones, c => c.Estado == EstadoCotizacion.Aceptada);
        Assert.Equal(EstadoCotizacion.Rechazada,
            cotizaciones.Single(c => c.Id == segundaId).Estado);
    }

    [Fact]
    public async Task CrearMientrasOtraOperacionAcepta_EsperaBloqueoYNoInserta()
    {
        var (solicitudId, primeraId, _) = await PrepararAsync();
        var primeraEnBloqueo = NuevaBarrera();
        var liberarPrimera = NuevaBarrera();
        var segundaIntentandoBloqueo = NuevaBarrera();
        await using var aceptadorDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(trasBloqueo: primeraEnBloqueo, continuar: liberarPrimera));
        await using var creadorDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(antesBloqueo: segundaIntentandoBloqueo));

        var aceptar = new CotizacionService(aceptadorDb).AceptarAsync(primeraId);
        await primeraEnBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var adminId = await ObtenerAdminIdAsync(creadorDb);
        var crear = new CotizacionService(creadorDb).CrearAsync(
            solicitudId, NuevaCotizacion(), adminId);
        try
        {
            await segundaIntentandoBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(crear.IsCompleted);
        }
        finally
        {
            liberarPrimera.SetResult();
        }

        await aceptar.WaitAsync(TimeSpan.FromSeconds(15));
        await Assert.ThrowsAsync<DatosInvalidosException>(async () =>
            await crear.WaitAsync(TimeSpan.FromSeconds(15)));
        await using var verificador = _factory.CrearDbContextDirecto();
        Assert.Equal(EstadoSolicitud.Aceptada,
            (await verificador.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).Estado);
        Assert.Equal(2, await verificador.Cotizaciones.CountAsync(c => c.SolicitudId == solicitudId));
    }

    [Fact]
    public async Task DosAceptacionesSimultaneas_LaSegundaEsperaYFalla()
    {
        var (solicitudId, primeraId, segundaId) = await PrepararAsync();
        var primeraEnBloqueo = NuevaBarrera();
        var liberarPrimera = NuevaBarrera();
        var segundaIntentandoBloqueo = NuevaBarrera();
        await using var primeroDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(trasBloqueo: primeraEnBloqueo, continuar: liberarPrimera));
        await using var segundoDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(antesBloqueo: segundaIntentandoBloqueo));

        var aceptarPrimera = new CotizacionService(primeroDb).AceptarAsync(primeraId);
        await primeraEnBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var aceptarSegunda = new CotizacionService(segundoDb).AceptarAsync(segundaId);
        try
        {
            await segundaIntentandoBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(aceptarSegunda.IsCompleted);
        }
        finally
        {
            liberarPrimera.SetResult();
        }

        await aceptarPrimera.WaitAsync(TimeSpan.FromSeconds(15));
        await Assert.ThrowsAsync<DatosInvalidosException>(async () =>
            await aceptarSegunda.WaitAsync(TimeSpan.FromSeconds(15)));
        await using var verificador = _factory.CrearDbContextDirecto();
        var cotizaciones = await verificador.Cotizaciones.AsNoTracking()
            .Where(c => c.SolicitudId == solicitudId).ToListAsync();
        Assert.Single(cotizaciones, c => c.Estado == EstadoCotizacion.Aceptada);
        Assert.Equal(EstadoCotizacion.Rechazada,
            cotizaciones.Single(c => c.Id == segundaId).Estado);
    }

    [Fact]
    public async Task RechazarMientrasOtraOperacionAcepta_EsperaYNoRevierteAceptacion()
    {
        var (solicitudId, primeraId, _) = await PrepararAsync();
        var primeraEnBloqueo = NuevaBarrera();
        var liberarPrimera = NuevaBarrera();
        var segundaIntentandoBloqueo = NuevaBarrera();
        await using var aceptadorDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(trasBloqueo: primeraEnBloqueo, continuar: liberarPrimera));
        await using var rechazadorDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(antesBloqueo: segundaIntentandoBloqueo));

        var aceptar = new CotizacionService(aceptadorDb).AceptarAsync(primeraId);
        await primeraEnBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var rechazar = new CotizacionService(rechazadorDb).RechazarAsync(primeraId, null);
        try
        {
            await segundaIntentandoBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(rechazar.IsCompleted);
        }
        finally
        {
            liberarPrimera.SetResult();
        }

        await aceptar.WaitAsync(TimeSpan.FromSeconds(15));
        await Assert.ThrowsAsync<DatosInvalidosException>(async () =>
            await rechazar.WaitAsync(TimeSpan.FromSeconds(15)));
        await using var verificador = _factory.CrearDbContextDirecto();
        Assert.Equal(EstadoSolicitud.Aceptada,
            (await verificador.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).Estado);
        Assert.Equal(EstadoCotizacion.Aceptada,
            (await verificador.Cotizaciones.AsNoTracking().SingleAsync(c => c.Id == primeraId)).Estado);
    }

    [Fact]
    public async Task CancelarPrimero_AceptacionEsperaYFalla()
    {
        var (solicitudId, primeraId, _) = await PrepararAsync();
        var primeraEnBloqueo = NuevaBarrera();
        var liberarPrimera = NuevaBarrera();
        var segundaIntentandoBloqueo = NuevaBarrera();
        await using var canceladorDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(trasBloqueo: primeraEnBloqueo, continuar: liberarPrimera));
        await using var aceptadorDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(antesBloqueo: segundaIntentandoBloqueo));

        var cancelar = new SolicitudService(canceladorDb)
            .CambiarEstadoAsync(solicitudId, nameof(EstadoSolicitud.Cancelada));
        await primeraEnBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var aceptar = new CotizacionService(aceptadorDb).AceptarAsync(primeraId);
        try
        {
            await segundaIntentandoBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(aceptar.IsCompleted);
        }
        finally
        {
            liberarPrimera.SetResult();
        }

        Assert.Equal(nameof(EstadoSolicitud.Cancelada),
            (await cancelar.WaitAsync(TimeSpan.FromSeconds(15)))!.Estado);
        await Assert.ThrowsAsync<DatosInvalidosException>(async () =>
            await aceptar.WaitAsync(TimeSpan.FromSeconds(15)));
        await using var verificador = _factory.CrearDbContextDirecto();
        Assert.Equal(EstadoSolicitud.Cancelada,
            (await verificador.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).Estado);
        Assert.Equal(2, await verificador.Cotizaciones.CountAsync(c =>
            c.SolicitudId == solicitudId && c.Estado == EstadoCotizacion.Rechazada));
        Assert.Equal(0, await verificador.Cotizaciones.CountAsync(c =>
            c.SolicitudId == solicitudId && c.Estado == EstadoCotizacion.Aceptada));
    }

    [Fact]
    public async Task AceptarPrimero_CancelacionEsperaYFalla()
    {
        var (solicitudId, primeraId, _) = await PrepararAsync();
        var primeraEnBloqueo = NuevaBarrera();
        var liberarPrimera = NuevaBarrera();
        var segundaIntentandoBloqueo = NuevaBarrera();
        await using var aceptadorDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(trasBloqueo: primeraEnBloqueo, continuar: liberarPrimera));
        await using var canceladorDb = CrearContextoConInterceptor(
            new InterceptorBloqueo(antesBloqueo: segundaIntentandoBloqueo));

        var aceptar = new CotizacionService(aceptadorDb).AceptarAsync(primeraId);
        await primeraEnBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var cancelar = new SolicitudService(canceladorDb)
            .CambiarEstadoAsync(solicitudId, nameof(EstadoSolicitud.Cancelada));
        try
        {
            await segundaIntentandoBloqueo.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(cancelar.IsCompleted);
        }
        finally
        {
            liberarPrimera.SetResult();
        }

        Assert.Equal(nameof(EstadoCotizacion.Aceptada),
            (await aceptar.WaitAsync(TimeSpan.FromSeconds(15)))!.Estado);
        await Assert.ThrowsAsync<DatosInvalidosException>(async () =>
            await cancelar.WaitAsync(TimeSpan.FromSeconds(15)));
        await using var verificador = _factory.CrearDbContextDirecto();
        Assert.Equal(EstadoSolicitud.Aceptada,
            (await verificador.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).Estado);
        Assert.Equal(EstadoCotizacion.Aceptada,
            (await verificador.Cotizaciones.AsNoTracking().SingleAsync(c => c.Id == primeraId)).Estado);
    }

    [Theory]
    [InlineData(EstadoSolicitud.Cancelada)]
    [InlineData(EstadoSolicitud.Rechazada)]
    public async Task CerrarSolicitud_CierraEnviadasYAuditaEnLaMismaTransaccion(EstadoSolicitud cierre)
    {
        var (solicitudId, primeraId, segundaId) = await PrepararAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var resultado = await new SolicitudService(db).CambiarEstadoAsync(solicitudId, cierre.ToString());
            Assert.Equal(cierre.ToString(), resultado!.Estado);
        }

        await using var verificador = _factory.CrearDbContextDirecto();
        Assert.Equal(cierre,
            (await verificador.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).Estado);
        var cotizaciones = await verificador.Cotizaciones.AsNoTracking()
            .Where(c => c.SolicitudId == solicitudId).ToListAsync();
        Assert.Equal(2, cotizaciones.Count);
        Assert.All(cotizaciones, c => Assert.Equal(EstadoCotizacion.Rechazada, c.Estado));
        Assert.Contains(cotizaciones, c => c.Id == primeraId);
        Assert.Contains(cotizaciones, c => c.Id == segundaId);
        Assert.True(await verificador.Bitacora.CountAsync(b => b.Entidad == nameof(Cotizacion)
            && (b.EntidadId == primeraId.ToString() || b.EntidadId == segundaId.ToString())
            && b.Accion == "Modificar") >= 2);
        Assert.True(await verificador.Bitacora.AnyAsync(b => b.Entidad == nameof(Solicitud)
            && b.EntidadId == solicitudId.ToString() && b.Accion == "Modificar"));
    }

    [Fact]
    public async Task AceptarVencida_DevuelveErrorDespuesDeConfirmarEstadoYAuditoria()
    {
        var (solicitudId, primeraId, _) = await PrepararAsync();
        await using (var preparador = _factory.CrearDbContextDirecto())
        {
            var cotizacion = await preparador.Cotizaciones.SingleAsync(c => c.Id == primeraId);
            cotizacion.VigenteHasta = DateTime.Now.AddDays(-1);
            await preparador.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await Assert.ThrowsAsync<DatosInvalidosException>(() =>
                new CotizacionService(db).AceptarAsync(primeraId));
        }

        await using var verificador = _factory.CrearDbContextDirecto();
        Assert.Equal(EstadoCotizacion.Vencida,
            (await verificador.Cotizaciones.AsNoTracking().SingleAsync(c => c.Id == primeraId)).Estado);
        Assert.Equal(EstadoSolicitud.Cotizada,
            (await verificador.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).Estado);
        Assert.True(await verificador.Bitacora.AnyAsync(b => b.Entidad == nameof(Cotizacion)
            && b.EntidadId == primeraId.ToString() && b.Accion == "Modificar"));
    }

    [Fact]
    public async Task DosAceptacionesHttp_NoDevuelven500NiConfirmanDosContratos()
    {
        var (solicitudId, primeraId, segundaId) = await PrepararAsync();
        using var loginClient = _factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("/api/identity/login", new
        {
            email = BackendTestFactory.TestSuperAdminEmail,
            password = _factory.TestSuperAdminPassword
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        using var primero = _factory.CreateClient();
        using var segundo = _factory.CreateClient();
        primero.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        segundo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuestas = await Task.WhenAll(
            primero.PatchAsync($"/api/cotizaciones/{primeraId}/aceptar", null),
            segundo.PatchAsync($"/api/cotizaciones/{segundaId}/aceptar", null));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.BadRequest);
        await using var db = _factory.CrearDbContextDirecto();
        Assert.Equal(EstadoSolicitud.Aceptada,
            (await db.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).Estado);
        Assert.Equal(1, await db.Cotizaciones.CountAsync(c =>
            c.SolicitudId == solicitudId && c.Estado == EstadoCotizacion.Aceptada));
    }

    private ApplicationDbContext CrearContextoConInterceptor(InterceptorBloqueo interceptor)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_factory.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static TaskCompletionSource NuevaBarrera() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class InterceptorBloqueo : DbCommandInterceptor
    {
        private readonly TaskCompletionSource? _antesBloqueo;
        private readonly TaskCompletionSource? _trasBloqueo;
        private readonly TaskCompletionSource? _continuar;

        public InterceptorBloqueo(TaskCompletionSource? antesBloqueo = null,
            TaskCompletionSource? trasBloqueo = null, TaskCompletionSource? continuar = null)
        {
            _antesBloqueo = antesBloqueo;
            _trasBloqueo = trasBloqueo;
            _continuar = continuar;
        }

        private static bool EsBloqueo(DbCommand command) =>
            command.CommandText.Contains("UPDLOCK, HOLDLOCK", StringComparison.Ordinal);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (EsBloqueo(command)) _antesBloqueo?.TrySetResult();
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (EsBloqueo(command) && _trasBloqueo is not null)
            {
                _trasBloqueo.TrySetResult();
                await _continuar!.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }

    private async Task<(int solicitudId, int primeraId, int segundaId)> PrepararAsync()
    {
        await using var db = _factory.CrearDbContextDirecto();
        var adminId = await ObtenerAdminIdAsync(db);
        var solicitud = new Solicitud
        {
            NombreContacto = "Concurrencia",
            CorreoContacto = $"concurrencia.{Guid.NewGuid():N}@saborlatino.local",
            TipoEvento = "Prueba",
            FechaEvento = DateTime.Now.AddDays(30),
            Lugar = "Heredia",
            Estado = EstadoSolicitud.Cotizada
        };
        var primera = NuevaEntidad(adminId);
        var segunda = NuevaEntidad(adminId);
        solicitud.Cotizaciones.Add(primera);
        solicitud.Cotizaciones.Add(segunda);
        db.Solicitudes.Add(solicitud);
        await db.SaveChangesAsync();
        return (solicitud.Id, primera.Id, segunda.Id);
    }

    private static Cotizacion NuevaEntidad(string adminId) => new()
    {
        Monto = 100000m,
        VigenteHasta = DateTime.Now.AddDays(10),
        CreadaPorUsuarioId = adminId
    };

    private static CotizacionCrearDto NuevaCotizacion() => new()
    {
        Monto = 120000m,
        VigenteHasta = DateTime.Now.AddDays(10)
    };

    private static Task<string> ObtenerAdminIdAsync(ApplicationDbContext db) =>
        db.Users.Where(u => u.Email == BackendTestFactory.TestSuperAdminEmail)
            .Select(u => u.Id).SingleAsync();

    private static Task<Cotizacion?> PrecargarGrafoAsync(ApplicationDbContext db, int id) =>
        db.Cotizaciones.Include(c => c.Solicitud)
            .ThenInclude(s => s!.Cotizaciones).SingleOrDefaultAsync(c => c.Id == id);
}
