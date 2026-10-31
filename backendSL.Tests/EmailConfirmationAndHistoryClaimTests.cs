using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Cuenta;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class EmailConfirmationAndHistoryClaimTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public EmailConfirmationAndHistoryClaimTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TerceroRegistraCorreoDeVisitanteSinConfirmarBuzon_NoVinculaSolicitudPreviaNiPermiteVerORechazarCotizacion()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionAsync(anonClient, BackendTestFactory.TestSuperAdminEmail, _factory.TestSuperAdminPassword);
        using var adminHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var correoVisitante = $"titular.visitante.{sufijo}@saborlatino.local";

        // 1. Identidad A (visitante titular del correo) envía una solicitud anónima
        var crearSolicitudResp = await anonClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Visitante Titular {sufijo}",
            CorreoContacto = correoVisitante,
            TelefonoContacto = "88881234",
            TipoEvento = "Boda",
            FechaEvento = DateTime.Now.AddDays(40),
            Lugar = "San José, Costa Rica",
            CantidadInvitados = 90,
            Comentarios = "Solicitud previa del titular antes de registrarse"
        });
        Assert.Equal(HttpStatusCode.Created, crearSolicitudResp.StatusCode);
        var solicitudVisitante = await crearSolicitudResp.Content.ReadFromJsonAsync<SolicitudDto>();
        Assert.NotNull(solicitudVisitante);
        Assert.Null(solicitudVisitante.ClienteUsuarioId);

        // El administrador emite una cotización para esa solicitud de visitante
        var crearCotizacionResp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitudVisitante.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 250000m,
                Descuento = 10000m,
                Detalle = "Cotización enviada al visitante",
                VigenteHasta = DateTime.Now.AddDays(15)
            });
        Assert.Equal(HttpStatusCode.Created, crearCotizacionResp.StatusCode);
        var cotizacion = await crearCotizacionResp.Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cotizacion);

        // 2. Identidad B (un tercero que NO controla el buzón de correoVisitante) registra una cuenta usando ese correo
        var passwordTercero = GenerarCredencialDePrueba();
        var registroTerceroResp = await anonClient.PostAsJsonAsync("/api/cuenta/registro", new RegistroDto
        {
            NombreCompleto = $"Tercero No Verificado {sufijo}",
            Email = correoVisitante,
            Password = passwordTercero,
            Telefono = "89990000"
        });
        Assert.Equal(HttpStatusCode.Created, registroTerceroResp.StatusCode);
        var perfilTercero = await registroTerceroResp.Content.ReadFromJsonAsync<PerfilDto>();
        Assert.NotNull(perfilTercero);
        Assert.Equal([Roles.Cliente], perfilTercero.Roles);

        // El tercero se autentica con la contraseña que él mismo eligió, pero SIN haber confirmado posesión del correo
        var tokenTercero = await IniciarSesionAsync(anonClient, correoVisitante, passwordTercero);
        using var terceroHttp = CrearClienteAutenticado(tokenTercero);

        // 3. Inspección de lo que ocurre sin haber confirmado el buzón
        var miasTerceroResp = await terceroHttp.GetAsync("/api/solicitudes/mias");
        Assert.Equal(HttpStatusCode.OK, miasTerceroResp.StatusCode);
        var solicitudesVisiblesPorTercero = await miasTerceroResp.Content.ReadFromJsonAsync<List<SolicitudDto>>();
        Assert.NotNull(solicitudesVisiblesPorTercero);

        var detallePorTerceroResp = await terceroHttp.GetAsync($"/api/solicitudes/{solicitudVisitante.Id}");
        var rechazarCotizacionPorTerceroResp = await terceroHttp.PatchAsync($"/api/cotizaciones/{cotizacion.Id}/rechazar", content: null);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var solicitudEnBd = await db.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudVisitante.Id);
        var cotizacionEnBd = await db.Cotizaciones.AsNoTracking().SingleAsync(c => c.Id == cotizacion.Id);

        Assert.True(
            solicitudesVisiblesPorTercero.Count == 0 &&
            detallePorTerceroResp.StatusCode == HttpStatusCode.NotFound &&
            rechazarCotizacionPorTerceroResp.StatusCode == HttpStatusCode.NotFound &&
            solicitudEnBd.ClienteUsuarioId is null &&
            cotizacionEnBd.Estado == EstadoCotizacion.Enviada,
            $"Fuga de historial sin confirmar correo: TerceroId='{perfilTercero.Id}', " +
            $"GET /api/solicitudes/mias devolvió {solicitudesVisiblesPorTercero.Count} solicitud(es), " +
            $"GET /api/solicitudes/{solicitudVisitante.Id} devolvió {(int)detallePorTerceroResp.StatusCode} ({detallePorTerceroResp.StatusCode}), " +
            $"PATCH /api/cotizaciones/{cotizacion.Id}/rechazar devolvió {(int)rechazarCotizacionPorTerceroResp.StatusCode} ({rechazarCotizacionPorTerceroResp.StatusCode}), " +
            $"Solicitud.ClienteUsuarioId='{solicitudEnBd.ClienteUsuarioId ?? "null"}', " +
            $"Cotizacion.Estado='{cotizacionEnBd.Estado}'.");
    }

    private HttpClient CrearClienteAutenticado(string bearerToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return client;
    }

    private static async Task<string> IniciarSesionAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/identity/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("accessToken", out var tokenElement));
        var token = tokenElement.GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private static string GenerarCredencialDePrueba()
    {
        Span<char> buffer = stackalloc char[20];
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
}
