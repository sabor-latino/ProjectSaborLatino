using System.Data.Common;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class StartupConnectionRetryTests
{
    [Fact]
    public async Task Inicio_ReintentaFalloTransitorioDeConexion_YConservaCatalogoIdempotente()
    {
        await using var factory = new ConexionFallidaFactory(transitorio: true);
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/servicios");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Interceptor.Fallos);
        Assert.True(factory.Interceptor.Aperturas >= 2);

        await DbSeeder.SembrarAsync(factory.Services);
        await using var db = factory.CrearDbContextDirecto();
        Assert.Equal(3, await db.Servicios.CountAsync());
        Assert.Equal(4, await db.Paquetes.CountAsync());
    }

    [Fact]
    public async Task Inicio_NoReintentaFalloPermanenteDeConexion()
    {
        await using var factory = new ConexionFallidaFactory(transitorio: false);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(factory.InitializeAsync);
        Assert.Equal("Fallo permanente simulado de conexion.", exception.Message);
        Assert.Equal(1, factory.Interceptor.Aperturas);
        Assert.False(await factory.ExisteBaseEnServidorAsync());
    }

    private sealed class ConexionFallidaFactory(bool transitorio) : BackendTestFactory
    {
        public ConexionFallidaInterceptor Interceptor { get; } = new(transitorio);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
                services.AddDbContext<ApplicationDbContext>((_, options) => options.AddInterceptors(Interceptor)));
        }
    }

    private sealed class ConexionFallidaInterceptor(bool transitorio) : DbConnectionInterceptor
    {
        private int _aperturas;
        private int _fallos;
        public int Aperturas => Volatile.Read(ref _aperturas);
        public int Fallos => Volatile.Read(ref _fallos);

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _aperturas) == 1)
            {
                Interlocked.Increment(ref _fallos);
                if (transitorio)
                    throw new TimeoutException("Conexion temporalmente no disponible durante el arranque.");
                throw new InvalidOperationException("Fallo permanente simulado de conexion.");
            }
            return ValueTask.FromResult(result);
        }
    }
}
