using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectSaborLatino.Data;
using Xunit;

namespace ProjectSaborLatino.Tests.Infrastructure;

public class BackendTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DatabasePrefix = "SaborLatinoDb_Test_";
    public const string ProductionDatabaseName = "SaborLatinoDb";
    public const string TestSuperAdminEmail = "superadmin.test@saborlatino.local";

    private readonly string _masterConnectionString;
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);
    private bool _cleanupCompleted;

    public string DatabaseName { get; }
    public string ConnectionString { get; }
    public string TestSuperAdminPassword { get; }

    public BackendTestFactory()
    {
        DatabaseName = $"{DatabasePrefix}{Guid.NewGuid():N}";
        ValidarNombreBaseAislada(DatabaseName);
        TestSuperAdminPassword = GenerarCredencialAleatoriaPorInstancia();

        var server = Environment.GetEnvironmentVariable("SABORLATINO_TEST_SQL_SERVER")
            ?? @"(localdb)\mssqllocaldb";

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = DatabaseName,
            IntegratedSecurity = true,
            MultipleActiveResultSets = true,
            TrustServerCertificate = true
        };
        ConnectionString = builder.ConnectionString;

        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master",
            MultipleActiveResultSets = false
        };
        _masterConnectionString = masterBuilder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // 1. Aplicar las seis migraciones reales sobre la base temporal antes de iniciar el host
            await using (var db = CrearDbContextDirecto())
            {
                ValidarNombreBaseAislada(db.Database.GetDbConnection().Database);
                await db.Database.MigrateAsync();
            }

            // 2. Arrancar el host de pruebas (ejecuta Program.cs y DbSeeder.SembrarAsync sobre la base temporal)
            _ = Services;
        }
        catch
        {
            await EliminarBaseTemporalAsync();
            throw;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["SuperAdmin:Email"] = TestSuperAdminEmail,
                ["SuperAdmin:Password"] = TestSuperAdminPassword
            });
        });

        builder.ConfigureServices(services =>
        {
            // Program.cs lee builder.Configuration.GetConnectionString("DefaultConnection") antes de builder.Build().
            // Reemplazamos el registro de ApplicationDbContext antes de que builder.Build() termine
            // para garantizar que el host jamás se conecte a SaborLatinoDb.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();

            var configuracionesPrevias = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.GetGenericTypeDefinition().Name.Contains("IDbContextOptionsConfiguration", StringComparison.Ordinal) &&
                            d.ServiceType.GenericTypeArguments.FirstOrDefault() == typeof(ApplicationDbContext))
                .ToList();

            foreach (var descriptor in configuracionesPrevias)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
                options.UseSqlServer(ConnectionString)
                    .AddInterceptors(sp.GetRequiredService<BitacoraInterceptor>()));

        });
    }

    public ApplicationDbContext CrearDbContextDirecto()
    {
        ValidarNombreBaseAislada(DatabaseName);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task<bool> ExisteBaseEnServidorAsync()
    {
        await using var conn = new SqlConnection(_masterConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM sys.databases WHERE name = @name;";
        cmd.Parameters.AddWithValue("@name", DatabaseName);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            await EliminarBaseTemporalAsync();
        }
    }

    private async Task EliminarBaseTemporalAsync()
    {
        if (Volatile.Read(ref _cleanupCompleted))
        {
            return;
        }

        await _cleanupLock.WaitAsync();
        try
        {
            if (_cleanupCompleted)
            {
                return;
            }

            ValidarNombreBaseAislada(DatabaseName);
            await EliminarBaseEnServidorAsync();
            Volatile.Write(ref _cleanupCompleted, true);
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    protected virtual async Task EliminarBaseEnServidorAsync()
    {
        await using var db = CrearDbContextDirecto();
        await db.Database.EnsureDeletedAsync();
        SqlConnection.ClearAllPools();
    }

    private static string GenerarCredencialAleatoriaPorInstancia()
    {
        Span<char> buffer = stackalloc char[24];
        ReadOnlySpan<char> simbolos = "!@#$%^&*_-+=";

        buffer[0] = (char)('A' + RandomNumberGenerator.GetInt32(26));
        buffer[1] = (char)('a' + RandomNumberGenerator.GetInt32(26));
        buffer[2] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        buffer[3] = simbolos[RandomNumberGenerator.GetInt32(simbolos.Length)];

        for (var i = 4; i < buffer.Length; i++)
        {
            buffer[i] = RandomNumberGenerator.GetInt32(4) switch
            {
                0 => (char)('A' + RandomNumberGenerator.GetInt32(26)),
                1 => (char)('a' + RandomNumberGenerator.GetInt32(26)),
                2 => (char)('0' + RandomNumberGenerator.GetInt32(10)),
                _ => simbolos[RandomNumberGenerator.GetInt32(simbolos.Length)]
            };
        }

        RandomNumberGenerator.Shuffle(buffer);
        return new string(buffer);
    }

    private static void ValidarNombreBaseAislada(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) ||
            !databaseName.StartsWith(DatabasePrefix, StringComparison.Ordinal) ||
            string.Equals(databaseName, ProductionDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Nombre de base inválido para pruebas ('{databaseName}'). Se exige prefijo '{DatabasePrefix}' y nunca '{ProductionDatabaseName}'.");
        }
    }
}
