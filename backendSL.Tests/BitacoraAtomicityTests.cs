using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class BitacoraAtomicityTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public BitacoraAtomicityTests(BackendTestFactory factory) => _factory = factory;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FalloDeBitacoraRevierteNegocio_EnGuardadoSincronoYAsincrono(bool asyncSave)
    {
        await using var setup = _factory.CrearDbContextDirecto();
        await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER BitacoraAtomicityFailure ON Bitacora AFTER INSERT AS BEGIN THROW 51000, 'Fallo inyectado de auditoria', 1; END");
        var nombre = $"Atomicidad {Guid.NewGuid():N}";
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_factory.ConnectionString)
                .AddInterceptors(new BitacoraInterceptor(new HttpContextAccessor()))
                .Options;
            await using var db = new ApplicationDbContext(options);
            db.Servicios.Add(new Servicio { Nombre = nombre });

            if (asyncSave)
                await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
            else
                Assert.ThrowsAny<Exception>(() => db.SaveChanges());

            await using var verify = _factory.CrearDbContextDirecto();
            Assert.False(await verify.Servicios.AnyAsync(s => s.Nombre == nombre));
        }
        finally
        {
            await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER BitacoraAtomicityFailure");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FalloDeBitacoraRevierteTransaccionExternaAunqueElLlamadorIntenteConfirmar(bool asyncSave)
    {
        // Cada clase tiene su propia BackendTestFactory y base temporal. xUnit no
        // ejecuta los casos de una misma clase en paralelo, así que el trigger
        // nunca afecta a otra prueba ni a otra base.
        await using var setup = _factory.CrearDbContextDirecto();
        await setup.Database.ExecuteSqlRawAsync("CREATE TRIGGER BitacoraAtomicityFailure ON Bitacora AFTER INSERT AS BEGIN THROW 51000, 'Fallo inyectado de auditoria', 1; END");
        var nombre = $"Externa {Guid.NewGuid():N}";
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_factory.ConnectionString)
                .AddInterceptors(new BitacoraInterceptor(new HttpContextAccessor()))
                .Options;
            await using var db = new ApplicationDbContext(options);
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.Servicios.Add(new Servicio { Nombre = nombre });

            if (asyncSave)
                await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
            else
                Assert.ThrowsAny<Exception>(() => db.SaveChanges());

            // La transacción externa debe quedar abortada; el llamador no puede
            // convertir un fallo de auditoría en un commit del negocio.
            await Assert.ThrowsAnyAsync<Exception>(() => transaction.CommitAsync());
            await using var verify = _factory.CrearDbContextDirecto();
            Assert.False(await verify.Servicios.AnyAsync(s => s.Nombre == nombre));
        }
        finally
        {
            await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER BitacoraAtomicityFailure");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GuardadoExitosoConservaClaveRealYBitacora(bool asyncSave)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_factory.ConnectionString)
            .AddInterceptors(new BitacoraInterceptor(new HttpContextAccessor()))
            .Options;
        await using var db = new ApplicationDbContext(options);
        var servicio = new Servicio { Nombre = $"Clave {Guid.NewGuid():N}" };
        db.Servicios.Add(servicio);
        if (asyncSave) await db.SaveChangesAsync();
        else db.SaveChanges();

        await using var verify = _factory.CrearDbContextDirecto();
        Assert.True(await verify.Servicios.AnyAsync(s => s.Id == servicio.Id));
        Assert.Equal(1, await verify.Bitacora.CountAsync(b => b.Entidad == nameof(Servicio)
            && b.EntidadId == servicio.Id.ToString() && b.Accion == "Crear"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransaccionExternaConservaNegocioYBitacoraHastaCommitDelLlamador(bool asyncSave)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_factory.ConnectionString)
            .AddInterceptors(new BitacoraInterceptor(new HttpContextAccessor()))
            .Options;
        await using var db = new ApplicationDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var servicio = new Servicio { Nombre = $"Externa exitosa {Guid.NewGuid():N}" };
        db.Servicios.Add(servicio);
        if (asyncSave) await db.SaveChangesAsync();
        else db.SaveChanges();

        await transaction.CommitAsync();
        await using var despues = _factory.CrearDbContextDirecto();
        Assert.True(await despues.Servicios.AnyAsync(s => s.Id == servicio.Id));
        Assert.Equal(1, await despues.Bitacora.CountAsync(b => b.Entidad == nameof(Servicio)
            && b.EntidadId == servicio.Id.ToString() && b.Accion == "Crear"));
    }
}
