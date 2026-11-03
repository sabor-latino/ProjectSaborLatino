using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class SolicitudWhatsAppManualTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;
    public SolicitudWhatsAppManualTests(BackendTestFactory factory) => _factory = factory;

    private static Dictionary<string, object?> Nueva(Guid? clave = null) => new()
    {
        ["nombreContacto"] = "Contacto " + Guid.NewGuid().ToString("N"),
        ["correoContacto"] = null,
        ["telefonoContacto"] = "+506 8888 1111",
        ["tipoEvento"] = "Boda",
        ["fechaEvento"] = DateTime.Now.AddDays(20),
        ["lugar"] = "San José",
        ["cantidadInvitados"] = 30,
        ["claveIdempotencia"] = clave
    };

    [Fact]
    public async Task TelefonoSinCorreoSeGuardaConCorreoNull_YContactoAusenteOInvalidoSeRechaza()
    {
        using var http = _factory.CreateClient();
        var dto = Nueva();
        using var response = await http.PostAsJsonAsync("/api/solicitudes", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, creada.GetProperty("correoContacto").ValueKind);
        Assert.Equal(JsonValueKind.Null, creada.GetProperty("recibidoWhatsAppEn").ValueKind);
        await using var db = _factory.CrearDbContextDirecto();
        Assert.Null((await db.Solicitudes.FindAsync(creada.GetProperty("id").GetInt32()))!.CorreoContacto);

        dto["telefonoContacto"] = " ";
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/solicitudes", dto)).StatusCode);
        dto["correoContacto"] = "correo-invalido";
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/solicitudes", dto)).StatusCode);
        dto["correoContacto"] = "contacto@example.com";
        Assert.Equal(HttpStatusCode.Created, (await http.PostAsJsonAsync("/api/solicitudes", dto)).StatusCode);
    }

    [Fact]
    public async Task TelefonoSinFormatoValidoNoSeAceptaComoContacto()
    {
        using var http = _factory.CreateClient();
        var dto = Nueva();
        dto["telefonoContacto"] = "x";
        var respuesta = await http.PostAsJsonAsync("/api/solicitudes", dto);
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await using var db = _factory.CrearDbContextDirecto();
        var nombre = (string)dto["nombreContacto"]!;
        Assert.False(await db.Solicitudes.AnyAsync(s => s.NombreContacto == nombre));
    }

    [Fact]
    public async Task MismaClaveYContenidoNormalizadoReutilizaSolicitud_PayloadDistintoDaConflicto()
    {
        using var http = _factory.CreateClient();
        var dto = Nueva(Guid.NewGuid());
        dto["correoContacto"] = "contacto@example.com";
        var primera = await Crear(http, dto);
        dto["nombreContacto"] = "  " + dto["nombreContacto"] + "  ";
        var repetida = await Crear(http, dto);
        Assert.Equal(primera.GetProperty("id").GetInt32(), repetida.GetProperty("id").GetInt32());
        dto["lugar"] = "Otra ciudad";
        var conflicto = await http.PostAsJsonAsync("/api/solicitudes", dto);
        Assert.Equal(HttpStatusCode.Conflict, conflicto.StatusCode);
        Assert.DoesNotContain(primera.GetProperty("nombreContacto").GetString()!, await conflicto.Content.ReadAsStringAsync());
        await using var db = _factory.CrearDbContextDirecto();
        var nombre = primera.GetProperty("nombreContacto").GetString();
        Assert.Equal(1, await db.Solicitudes.CountAsync(s => s.NombreContacto == nombre));
    }

    [Fact]
    public async Task OtraCuentaNoPuedeReutilizarClaveNiRecibirDatosDeSolicitudAjena()
    {
        using var a = await Usuario(Roles.Cliente);
        using var b = await Usuario(Roles.Cliente);
        using var anon = _factory.CreateClient();
        var dto = Nueva(Guid.NewGuid());
        var original = await Crear(a, dto);
        Assert.Equal(original.GetProperty("id").GetInt32(), (await Crear(a, dto)).GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await b.PostAsJsonAsync("/api/solicitudes", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await anon.PostAsJsonAsync("/api/solicitudes", dto)).StatusCode);
    }

    [Fact]
    public async Task ClaveVaciaSeRechaza_YClientesSinClaveSiguenCreandoSolicitudesIndependientes()
    {
        using var http = _factory.CreateClient();
        var dto = Nueva(Guid.Empty);
        dto["correoContacto"] = "legacy@example.com";
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/solicitudes", dto)).StatusCode);
        dto.Remove("claveIdempotencia");
        Assert.NotEqual((await Crear(http, dto)).GetProperty("id").GetInt32(), (await Crear(http, dto)).GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task ReplayAnonimoNoExponeCotizacionesNiConfirmacionAdministrativaPosteriores()
    {
        using var anon = _factory.CreateClient();
        using var admin = await Usuario(Roles.Administrador);
        var dto = Nueva(Guid.NewGuid());
        var creada = await Crear(anon, dto);
        var id = creada.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"/api/solicitudes/{id}/cotizaciones",
            new { monto = 90000, descuento = 0, detalle = "Oferta privada", vigenteHasta = DateTime.Now.AddDays(3) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/solicitudes/{id}/whatsapp-recibido", null)).StatusCode);

        var replay = await Crear(anon, dto);
        Assert.Equal(id, replay.GetProperty("id").GetInt32());
        Assert.Empty(replay.GetProperty("cotizaciones").EnumerateArray());
        Assert.Equal("Pendiente", replay.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, replay.GetProperty("recibidoWhatsAppEn").ValueKind);
        Assert.Equal(JsonValueKind.Null, replay.GetProperty("recibidoWhatsAppPorUsuarioId").ValueKind);
        Assert.False(replay.TryGetProperty("claveIdempotencia", out _));
        Assert.False(replay.TryGetProperty("huellaIdempotencia", out _));
        var privada = await admin.GetFromJsonAsync<JsonElement>($"/api/solicitudes/{id}");
        Assert.Single(privada.GetProperty("cotizaciones").EnumerateArray());
        Assert.Equal("Cotizada", privada.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.String, privada.GetProperty("recibidoWhatsAppEn").ValueKind);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync($"/api/solicitudes/{id}")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DosInsercionesSimultaneasMismaClaveDevuelvenUnaSolicitudSin500(bool cambiarPayload)
    {
        var barrera = new BarreraInserciones();
        using var app = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<ApplicationDbContext>((_, options) => options.AddInterceptors(barrera))));
        using var a = app.CreateClient();
        using var b = app.CreateClient();
        var dto = Nueva(Guid.NewGuid());
        dto["correoContacto"] = "concurrencia@example.com";
        var otroDto = new Dictionary<string, object?>(dto);
        if (cambiarPayload) otroDto["lugar"] = "Lugar distinto";
        var respuestas = await Task.WhenAll(a.PostAsJsonAsync("/api/solicitudes", dto), b.PostAsJsonAsync("/api/solicitudes", otroDto));
        var creada = respuestas.First(r => r.StatusCode == HttpStatusCode.Created);
        var primera = await creada.Content.ReadFromJsonAsync<JsonElement>();
        if (cambiarPayload)
        {
            Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        }
        else
        {
            Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
            var segunda = await respuestas[1].Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(primera.GetProperty("id").GetInt32(), segunda.GetProperty("id").GetInt32());
        }
        Assert.Equal(2, barrera.Llegadas);
        await using var db = _factory.CrearDbContextDirecto();
        var nombre = (string)dto["nombreContacto"]!;
        Assert.Equal(1, await db.Solicitudes.CountAsync(s => s.NombreContacto == nombre));
        var referencia = primera.GetProperty("id").GetInt32().ToString();
        Assert.Equal(1, await db.Bitacora.CountAsync(x => x.Entidad == "Solicitud" && x.EntidadId == referencia && x.Accion == "Crear"));
    }

    [Fact]
    public async Task RecibidoWhatsAppExigeAdmin_ConservaPrimerAutorFechaYEstadoComercial()
    {
        using var anon = _factory.CreateClient();
        var solicitud = await Crear(anon, Nueva());
        var id = solicitud.GetProperty("id").GetInt32();
        var ruta = $"/api/solicitudes/{id}/whatsapp-recibido";
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsync(ruta, null)).StatusCode);
        foreach (var rol in new[] { Roles.Cliente, Roles.Trabajador })
        {
            using var sinPermiso = await Usuario(rol);
            Assert.Equal(HttpStatusCode.Forbidden, (await sinPermiso.PostAsync(ruta, null)).StatusCode);
        }
        using var admin = await Usuario(Roles.Administrador);
        using var super = await Usuario(Roles.Superadministrador);
        var antes = DateTimeOffset.UtcNow;
        var primeraRespuesta = await admin.PostAsync(ruta, null);
        Assert.Equal(HttpStatusCode.OK, primeraRespuesta.StatusCode);
        var primera = await primeraRespuesta.Content.ReadFromJsonAsync<JsonElement>();
        var fecha = primera.GetProperty("recibidoWhatsAppEn").GetDateTimeOffset();
        Assert.InRange(fecha, antes, DateTimeOffset.UtcNow);
        Assert.Equal(TimeSpan.Zero, fecha.Offset);
        Assert.EndsWith("Z", primera.GetProperty("recibidoWhatsAppEn").GetString());
        Assert.False(string.IsNullOrWhiteSpace(primera.GetProperty("recibidoWhatsAppPorUsuarioId").GetString()));
        var segunda = await (await super.PostAsync(ruta, null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(primera.GetProperty("recibidoWhatsAppEn").GetString(), segunda.GetProperty("recibidoWhatsAppEn").GetString());
        Assert.Equal(primera.GetProperty("recibidoWhatsAppPorUsuarioId").GetString(), segunda.GetProperty("recibidoWhatsAppPorUsuarioId").GetString());
        Assert.Equal(solicitud.GetProperty("estado").GetString(), segunda.GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await super.PostAsync("/api/solicitudes/2147483647/whatsapp-recibido", null)).StatusCode);
    }

    private static async Task<JsonElement> Crear(HttpClient http, object dto)
    {
        var respuesta = await http.PostAsJsonAsync("/api/solicitudes", dto);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<HttpClient> Usuario(string rol)
    {
        using var scope = _factory.Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var correo = $"wa.{Guid.NewGuid():N}@example.com";
        var usuario = new ApplicationUser { UserName = correo, Email = correo, NombreCompleto = rol, EmailConfirmed = true };
        Assert.True((await usuarios.CreateAsync(usuario, _factory.TestSuperAdminPassword)).Succeeded);
        Assert.True((await usuarios.AddToRoleAsync(usuario, rol)).Succeeded);
        var http = _factory.CreateClient();
        var login = await http.PostAsJsonAsync("/api/identity/login", new { email = correo, password = _factory.TestSuperAdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    private sealed class BarreraInserciones : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _dosLlegaron = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _llegadas;
        public int Llegadas => Volatile.Read(ref _llegadas);
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO [Solicitudes]", StringComparison.Ordinal))
            {
                if (Interlocked.Increment(ref _llegadas) == 2) _dosLlegaron.TrySetResult();
                await _dosLlegaron.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }
}
