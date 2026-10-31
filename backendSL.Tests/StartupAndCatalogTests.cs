using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class StartupAndCatalogTests : IClassFixture<BackendTestFactory>
{
    private static readonly string[] MigracionesEsperadas =
    [
        "20260926160341_InitialCreate",
        "20261006052219_UsuarioPropioYRoles",
        "20261006220938_Catalogo",
        "20261007011559_SolicitudesCotizaciones",
        "20261007040140_Eventos",
        "20261007043719_UsuariosSistema"
    ];

    private readonly BackendTestFactory _factory;

    public StartupAndCatalogTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MigracionesReales_CreanEsquemaDesdeCeroEnBaseAislada_SinPendientes()
    {
        Assert.StartsWith(BackendTestFactory.DatabasePrefix, _factory.DatabaseName, StringComparison.Ordinal);
        Assert.NotEqual(BackendTestFactory.ProductionDatabaseName, _factory.DatabaseName, StringComparer.OrdinalIgnoreCase);
        Assert.True(await _factory.ExisteBaseEnServidorAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var nombreBaseActiva = db.Database.GetDbConnection().Database;
        Assert.Equal(_factory.DatabaseName, nombreBaseActiva);
        Assert.NotEqual(BackendTestFactory.ProductionDatabaseName, nombreBaseActiva, StringComparer.OrdinalIgnoreCase);

        var pendientes = (await db.Database.GetPendingMigrationsAsync()).ToList();
        Assert.Empty(pendientes);

        var aplicadas = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Equal(MigracionesEsperadas, aplicadas);

        var tablasCreadas = await db.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
            .ToListAsync();

        var tablasEsperadas = new[]
        {
            "Servicios",
            "Equipos",
            "Paquetes",
            "PaquetesEquipos",
            "ImagenesGaleria",
            "PreguntasFrecuentes",
            "Solicitudes",
            "Cotizaciones",
            "Empleados",
            "Eventos",
            "EventosEquipos",
            "EventosTrabajadores",
            "EventosHistorialEstados",
            "Configuraciones",
            "Bitacora",
            "AspNetUsers",
            "AspNetRoles"
        };

        foreach (var tabla in tablasEsperadas)
        {
            Assert.Contains(tabla, tablasCreadas, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task LimpiezaBaseTemporal_EliminaUnicamenteBaseEfimeraEnDisposeAsync_InclusoTrasExcepcion()
    {
        var factoryEfimera = new BackendTestFactory();
        Assert.NotEqual(_factory.DatabaseName, factoryEfimera.DatabaseName);
        Assert.False(string.IsNullOrWhiteSpace(factoryEfimera.TestSuperAdminPassword));
        Assert.NotEqual(_factory.TestSuperAdminPassword, factoryEfimera.TestSuperAdminPassword);

        var falloSimuladoCapturado = false;

        try
        {
            await factoryEfimera.InitializeAsync();
            Assert.True(await factoryEfimera.ExisteBaseEnServidorAsync());

            throw new InvalidOperationException("Fallo simulado para comprobar limpieza en finally/DisposeAsync.");
        }
        catch (InvalidOperationException)
        {
            falloSimuladoCapturado = true;
        }
        finally
        {
            await factoryEfimera.DisposeAsync();
        }

        Assert.True(falloSimuladoCapturado);
        Assert.False(await factoryEfimera.ExisteBaseEnServidorAsync());
        Assert.True(await _factory.ExisteBaseEnServidorAsync());
    }

    [Fact]
    public async Task LimpiezaBaseTemporal_ReintentaBorradoTrasFalloTransitorioYConservaIdempotenciaConcurrente()
    {
        var factoryConFallo = new FalloTransitorioBorradoFactory();

        try
        {
            await factoryConFallo.InitializeAsync();
            Assert.True(await factoryConFallo.ExisteBaseEnServidorAsync());

            // 1. El primer intento de DisposeAsync falla transitoriamente antes de confirmar el borrado:
            // la base aún existe en SQL Server y la fábrica NO debe marcar la limpieza como completada.
            await Assert.ThrowsAsync<TimeoutException>(() => factoryConFallo.DisposeAsync().AsTask());
            Assert.True(await factoryConFallo.ExisteBaseEnServidorAsync());
            Assert.Equal(1, factoryConFallo.IntentosBorrado);

            // 2. Un segundo llamado (incluso concurrente con un tercero) reintenta el borrado real,
            // serializa el acceso sin ejecutar dos borrados simultáneos y deja la base eliminada.
            await Task.WhenAll(
                factoryConFallo.DisposeAsync().AsTask(),
                factoryConFallo.DisposeAsync().AsTask());

            Assert.False(await factoryConFallo.ExisteBaseEnServidorAsync());
            Assert.Equal(2, factoryConFallo.IntentosBorrado);
        }
        finally
        {
            if (await factoryConFallo.ExisteBaseEnServidorAsync())
            {
                await factoryConFallo.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task DbSeeder_EjecutadoDosVeces_EsIdempotenteYNoDuplicaDatosIniciales()
    {
        // Primera ejecución ocurrió al iniciar el host en Program.cs; ejecutamos la segunda explícitamente
        await DbSeeder.SembrarAsync(_factory.Services);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var servicios = await db.Servicios.AsNoTracking().OrderBy(s => s.Nombre).ToListAsync();
        Assert.Equal(3, servicios.Count);
        Assert.Equal(["Karaoke", "Música en vivo", "Sonido"], servicios.Select(s => s.Nombre).ToArray());

        var equipos = await db.Equipos.AsNoTracking().ToListAsync();
        Assert.Equal(8, equipos.Count);
        Assert.Equal(8, equipos.Select(e => e.Nombre).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var paquetes = await db.Paquetes
            .AsNoTracking()
            .Include(p => p.Equipos)
            .ToListAsync();
        Assert.Equal(4, paquetes.Count);
        Assert.Equal(4, paquetes.Select(p => p.Nombre).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(24, paquetes.Sum(p => p.Equipos.Count));

        var configuraciones = await db.Configuraciones.AsNoTracking().OrderBy(c => c.Clave).ToListAsync();
        Assert.Equal(2, configuraciones.Count);
        Assert.Contains(configuraciones, c => c.Clave == "Fidelidad.EventosMinimos" && c.Valor == "3");
        Assert.Contains(configuraciones, c => c.Clave == "Fidelidad.PorcentajeDescuento" && c.Valor == "10");

        var roles = await roleManager.Roles.Select(r => r.Name!).ToListAsync();
        Assert.Equal(Roles.Todos.Length, roles.Count);
        foreach (var rolEsperado in Roles.Todos)
        {
            Assert.Contains(rolEsperado, roles);
        }

        var superAdmins = await userManager.Users
            .Where(u => u.Email == BackendTestFactory.TestSuperAdminEmail)
            .ToListAsync();
        var superAdmin = Assert.Single(superAdmins);
        Assert.True(await userManager.IsInRoleAsync(superAdmin, Roles.Superadministrador));
        Assert.True(await userManager.CheckPasswordAsync(superAdmin, _factory.TestSuperAdminPassword));
    }

    [Fact]
    public async Task CatalogoPublico_GetServiciosYPaquetes_RespondeHttp200ConDatosSembrados()
    {
        using var client = _factory.CreateClient();

        var serviciosResponse = await client.GetAsync("/api/servicios");
        Assert.Equal(HttpStatusCode.OK, serviciosResponse.StatusCode);

        var servicios = await serviciosResponse.Content.ReadFromJsonAsync<List<ServicioDto>>();
        Assert.NotNull(servicios);
        Assert.Equal(3, servicios.Count);
        Assert.All(servicios, s => Assert.True(s.Activo));
        Assert.Equal(["Karaoke", "Música en vivo", "Sonido"], servicios.Select(s => s.Nombre).ToArray());

        var paquetesResponse = await client.GetAsync("/api/paquetes");
        Assert.Equal(HttpStatusCode.OK, paquetesResponse.StatusCode);

        var paquetes = await paquetesResponse.Content.ReadFromJsonAsync<List<PaqueteDto>>();
        Assert.NotNull(paquetes);
        Assert.Equal(4, paquetes.Count);
        Assert.All(paquetes, p =>
        {
            Assert.True(p.Activo);
            Assert.False(string.IsNullOrWhiteSpace(p.ServicioNombre));
            Assert.NotEmpty(p.Equipos);
            Assert.All(p.Equipos, eq =>
            {
                Assert.False(string.IsNullOrWhiteSpace(eq.EquipoNombre));
                Assert.True(eq.Cantidad > 0);
            });
        });

        Assert.Equal([60000m, 80000m, 170000m, 300000m], paquetes.Select(p => p.PrecioBase).ToArray());
        Assert.Equal(
            [
                "Sonorización para grupos o DJs",
                "DJ Open Format",
                "Grupo secuenciado Villa Guanacaste",
                "Marimba Orquesta Son del Gallo (en vivo)"
            ],
            paquetes.Select(p => p.Nombre).ToArray());
        Assert.All(paquetes, p =>
        {
            Assert.NotNull(p.Descripcion);
            Assert.Contains("Precio base", p.Descripcion, StringComparison.OrdinalIgnoreCase);
        });

        var musicaEnVivo = Assert.Single(servicios, s => s.Nombre == "Música en vivo");
        var paquetesFiltradosResponse = await client.GetAsync($"/api/paquetes?servicioId={musicaEnVivo.Id}");
        Assert.Equal(HttpStatusCode.OK, paquetesFiltradosResponse.StatusCode);

        var paquetesMusicaEnVivo = await paquetesFiltradosResponse.Content.ReadFromJsonAsync<List<PaqueteDto>>();
        Assert.NotNull(paquetesMusicaEnVivo);
        Assert.Equal(2, paquetesMusicaEnVivo.Count);
        Assert.All(paquetesMusicaEnVivo, p =>
        {
            Assert.Equal(musicaEnVivo.Id, p.ServicioId);
            Assert.Equal("Música en vivo", p.ServicioNombre);
        });
    }

    private sealed class FalloTransitorioBorradoFactory : BackendTestFactory
    {
        private int _intentosBorrado;

        public int IntentosBorrado => Volatile.Read(ref _intentosBorrado);

        protected override async Task EliminarBaseEnServidorAsync()
        {
            if (Interlocked.Increment(ref _intentosBorrado) == 1)
            {
                throw new TimeoutException("Fallo transitorio simulado de SQL al intentar borrar la base temporal.");
            }

            await base.EliminarBaseEnServidorAsync();
        }
    }
}
