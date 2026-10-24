using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.DTOs.Cuenta;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class IdentityRegistrationTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public IdentityRegistrationTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task IdentityRegister_RutaCerrada_NoCreaCuentaHuerfanaSinRolNiPermiteLogin()
    {
        using var client = _factory.CreateClient();
        var email = $"visitante.{Guid.NewGuid():N}@saborlatino.local";
        var password = GenerarCredencialDePrueba();

        var paqueteId = await ObtenerPrimerPaqueteActivoIdAsync(client);

        // 1. Un visitante sin cuenta envía primero una solicitud con su correo
        var crearSolicitudResponse = await client.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = "Visitante Previo",
            CorreoContacto = email,
            TelefonoContacto = "88887777",
            PaqueteId = paqueteId,
            TipoEvento = "Boda",
            FechaEvento = DateTime.Now.AddDays(30),
            Lugar = "San José, Costa Rica",
            CantidadInvitados = 80,
            Comentarios = "Solicitud enviada antes de registrarse"
        });
        Assert.Equal(HttpStatusCode.Created, crearSolicitudResponse.StatusCode);

        var solicitudInicial = await crearSolicitudResponse.Content.ReadFromJsonAsync<SolicitudDto>();
        Assert.NotNull(solicitudInicial);
        Assert.Null(solicitudInicial.ClienteUsuarioId);

        // 2. Intento de registro por la ruta alternativa de Identity (/api/identity/register)
        var registerResponse = await client.PostAsJsonAsync("/api/identity/register", new
        {
            email,
            password
        });

        // 3. Intento de inicio de sesión con las credenciales enviadas a /api/identity/register
        var loginResponse = await client.PostAsJsonAsync("/api/identity/login", new
        {
            email,
            password
        });

        // 4. Inspección en base de datos y endpoints protegidos para documentar cualquier regresión
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var usuarioEnDb = await userManager.FindByEmailAsync(email);
        IList<string> rolesObservados = usuarioEnDb is not null
            ? await userManager.GetRolesAsync(usuarioEnDb)
            : [];

        var solicitudEnDb = await db.Solicitudes
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitudInicial.Id);

        var statusYo = "N/A";
        var statusMias = "N/A";

        if (loginResponse.StatusCode == HttpStatusCode.OK)
        {
            var token = await ExtraerAccessTokenAsync(loginResponse);
            using var authClient = _factory.CreateClient();
            authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var yoResponse = await authClient.GetAsync("/api/cuenta/yo");
            var miasResponse = await authClient.GetAsync("/api/solicitudes/mias");
            statusYo = $"{(int)yoResponse.StatusCode} ({yoResponse.StatusCode})";
            statusMias = $"{(int)miasResponse.StatusCode} ({miasResponse.StatusCode})";
        }

        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.NotFound && usuarioEnDb is null,
            $"POST /api/identity/register devolvió {(int)registerResponse.StatusCode} ({registerResponse.StatusCode}). " +
            $"UsuarioEnDb={usuarioEnDb is not null}, Roles=[{string.Join(", ", rolesObservados)}], " +
            $"NombreCompleto='{usuarioEnDb?.NombreCompleto}', Solicitud.ClienteUsuarioId='{solicitudEnDb.ClienteUsuarioId ?? "null"}', " +
            $"LoginHTTP={(int)loginResponse.StatusCode} ({loginResponse.StatusCode}), " +
            $"GET /api/cuenta/yo={statusYo}, GET /api/solicitudes/mias={statusMias}.");

        Assert.Equal(HttpStatusCode.NotFound, registerResponse.StatusCode);
        Assert.Null(usuarioEnDb);
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        Assert.Null(solicitudEnDb.ClienteUsuarioId);

        // 5. Como /api/identity/register no contaminó AspNetUsers, el mismo correo puede registrarse
        // inmediatamente por la vía oficial POST /api/cuenta/registro, obteniendo rol Cliente.
        // La solicitud previa de visitante NO se vincula automáticamente sin confirmar posesión del buzón.
        var registroLegitimoResponse = await client.PostAsJsonAsync("/api/cuenta/registro", new RegistroDto
        {
            NombreCompleto = "Visitante Previo Convertido",
            Email = email,
            Password = password,
            Telefono = "88887777"
        });
        Assert.Equal(HttpStatusCode.Created, registroLegitimoResponse.StatusCode);

        var perfilLegitimo = await registroLegitimoResponse.Content.ReadFromJsonAsync<PerfilDto>();
        Assert.NotNull(perfilLegitimo);
        Assert.Equal([Roles.Cliente], perfilLegitimo.Roles);

        var solicitudSinVincularEnDb = await db.Solicitudes
            .AsNoTracking()
            .SingleAsync(s => s.Id == solicitudInicial.Id);
        Assert.Null(solicitudSinVincularEnDb.ClienteUsuarioId);
    }

    [Fact]
    public async Task CuentaRegistro_CreaClienteSinVincularSolicitudDeVisitanteYLoginIdentityFuncionaConAutorizacionPorRol()
    {
        using var client = _factory.CreateClient();
        var email = $"cliente.{Guid.NewGuid():N}@saborlatino.local";
        var password = GenerarCredencialDePrueba();
        var paqueteId = await ObtenerPrimerPaqueteActivoIdAsync(client);

        // 1. Un visitante crea una solicitud antes de que exista la cuenta con ese correo
        var crearSolicitudResponse = await client.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = "Cliente Legítimo",
            CorreoContacto = email,
            TelefonoContacto = "88889999",
            PaqueteId = paqueteId,
            TipoEvento = "Aniversario",
            FechaEvento = DateTime.Now.AddDays(45),
            Lugar = "Heredia, Costa Rica",
            CantidadInvitados = 50
        });
        Assert.Equal(HttpStatusCode.Created, crearSolicitudResponse.StatusCode);
        var solicitudVisitante = await crearSolicitudResponse.Content.ReadFromJsonAsync<SolicitudDto>();
        Assert.NotNull(solicitudVisitante);
        Assert.Null(solicitudVisitante.ClienteUsuarioId);

        // 2. Sin token, los endpoints protegidos rechazan con 401 Unauthorized
        var yoSinTokenResponse = await client.GetAsync("/api/cuenta/yo");
        Assert.Equal(HttpStatusCode.Unauthorized, yoSinTokenResponse.StatusCode);

        var miasSinTokenResponse = await client.GetAsync("/api/solicitudes/mias");
        Assert.Equal(HttpStatusCode.Unauthorized, miasSinTokenResponse.StatusCode);

        // 3. Registro por POST /api/cuenta/registro crea cuenta con rol Cliente
        var registroResponse = await client.PostAsJsonAsync("/api/cuenta/registro", new RegistroDto
        {
            NombreCompleto = "Cliente Legítimo",
            Email = email,
            Password = password,
            Telefono = "88889999"
        });
        Assert.Equal(HttpStatusCode.Created, registroResponse.StatusCode);

        var perfilCreado = await registroResponse.Content.ReadFromJsonAsync<PerfilDto>();
        Assert.NotNull(perfilCreado);
        Assert.False(string.IsNullOrWhiteSpace(perfilCreado.Id));
        Assert.Equal("Cliente Legítimo", perfilCreado.NombreCompleto);
        Assert.Equal(email, perfilCreado.Email);
        Assert.Equal([Roles.Cliente], perfilCreado.Roles);

        // 4. Inicio de sesión mediante POST /api/identity/login sigue operativo
        var loginResponse = await client.PostAsJsonAsync("/api/identity/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var accessToken = await ExtraerAccessTokenAsync(loginResponse);
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        using var clienteAutenticado = _factory.CreateClient();
        clienteAutenticado.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // 5. GET /api/cuenta/yo devuelve el perfil con rol Cliente únicamente
        var yoResponse = await clienteAutenticado.GetAsync("/api/cuenta/yo");
        Assert.Equal(HttpStatusCode.OK, yoResponse.StatusCode);
        var perfilActual = await yoResponse.Content.ReadFromJsonAsync<PerfilDto>();
        Assert.NotNull(perfilActual);
        Assert.Equal(perfilCreado.Id, perfilActual.Id);
        Assert.Equal([Roles.Cliente], perfilActual.Roles);

        // 6. El mero registro no vincula ni expone automáticamente la solicitud previa de visitante
        var miasTrasRegistroResponse = await clienteAutenticado.GetAsync("/api/solicitudes/mias");
        Assert.Equal(HttpStatusCode.OK, miasTrasRegistroResponse.StatusCode);
        var misSolicitudesIniciales = await miasTrasRegistroResponse.Content.ReadFromJsonAsync<List<SolicitudDto>>();
        Assert.NotNull(misSolicitudesIniciales);
        Assert.Empty(misSolicitudesIniciales);

        var detalleVisitanteResponse = await clienteAutenticado.GetAsync($"/api/solicitudes/{solicitudVisitante.Id}");
        Assert.Equal(HttpStatusCode.NotFound, detalleVisitanteResponse.StatusCode);

        // En cambio, una solicitud creada ya autenticado como Cliente sí queda asociada a su cuenta
        var crearSolicitudAutenticadoResponse = await clienteAutenticado.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = "Cliente Legítimo",
            CorreoContacto = email,
            TelefonoContacto = "88889999",
            PaqueteId = paqueteId,
            TipoEvento = "Graduación",
            FechaEvento = DateTime.Now.AddDays(60),
            Lugar = "Alajuela, Costa Rica",
            CantidadInvitados = 70
        });
        Assert.Equal(HttpStatusCode.Created, crearSolicitudAutenticadoResponse.StatusCode);
        var solicitudAutenticada = await crearSolicitudAutenticadoResponse.Content.ReadFromJsonAsync<SolicitudDto>();
        Assert.NotNull(solicitudAutenticada);
        Assert.Equal(perfilCreado.Id, solicitudAutenticada.ClienteUsuarioId);

        var miasResponse = await clienteAutenticado.GetAsync("/api/solicitudes/mias");
        Assert.Equal(HttpStatusCode.OK, miasResponse.StatusCode);
        var misSolicitudes = await miasResponse.Content.ReadFromJsonAsync<List<SolicitudDto>>();
        Assert.NotNull(misSolicitudes);
        var solicitudPropia = Assert.Single(misSolicitudes);
        Assert.Equal(solicitudAutenticada.Id, solicitudPropia.Id);
        Assert.Equal(perfilCreado.Id, solicitudPropia.ClienteUsuarioId);

        // 7. El Cliente no adquiere permisos administrativos (recibe 403 Forbidden en rutas de administración)
        var usuariosAdminResponse = await clienteAutenticado.GetAsync("/api/usuarios");
        Assert.Equal(HttpStatusCode.Forbidden, usuariosAdminResponse.StatusCode);

        var todasSolicitudesAdminResponse = await clienteAutenticado.GetAsync("/api/solicitudes");
        Assert.Equal(HttpStatusCode.Forbidden, todasSolicitudesAdminResponse.StatusCode);

        var configuracionSuperAdminResponse = await clienteAutenticado.GetAsync("/api/configuracion");
        Assert.Equal(HttpStatusCode.Forbidden, configuracionSuperAdminResponse.StatusCode);
    }

    [Theory]
    [InlineData("/api/identity/register")]
    [InlineData("/api/identity/REGISTER")]
    [InlineData("/api/identity/register/")]
    [InlineData("/api/identity/register?useCookies=true")]
    public async Task IdentityRegister_VariantesDeRutaEIntentosAutenticados_Responden404SinCrearUsuario(string ruta)
    {
        using var client = _factory.CreateClient();
        var emailAnonimo = $"anon.{Guid.NewGuid():N}@saborlatino.local";
        var passwordAnonimo = GenerarCredencialDePrueba();

        // 1. Petición anónima a la variante de ruta
        var anonResponse = await client.PostAsJsonAsync(ruta, new
        {
            email = emailAnonimo,
            password = passwordAnonimo
        });
        Assert.Equal(HttpStatusCode.NotFound, anonResponse.StatusCode);

        // 2. Incluso con un token válido de Superadministrador, /api/identity/register permanece cerrado (404)
        var superAdminLoginResponse = await client.PostAsJsonAsync("/api/identity/login", new
        {
            email = BackendTestFactory.TestSuperAdminEmail,
            password = _factory.TestSuperAdminPassword
        });
        Assert.Equal(HttpStatusCode.OK, superAdminLoginResponse.StatusCode);
        var superAdminToken = await ExtraerAccessTokenAsync(superAdminLoginResponse);

        using var superAdminClient = _factory.CreateClient();
        superAdminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", superAdminToken);

        var emailConToken = $"auth.{Guid.NewGuid():N}@saborlatino.local";
        var authResponse = await superAdminClient.PostAsJsonAsync(ruta, new
        {
            email = emailConToken,
            password = GenerarCredencialDePrueba()
        });
        Assert.Equal(HttpStatusCode.NotFound, authResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await userManager.FindByEmailAsync(emailAnonimo));
        Assert.Null(await userManager.FindByEmailAsync(emailConToken));
    }

    private static async Task<int> ObtenerPrimerPaqueteActivoIdAsync(HttpClient client)
    {
        var paquetesResponse = await client.GetAsync("/api/paquetes");
        Assert.Equal(HttpStatusCode.OK, paquetesResponse.StatusCode);
        var paquetes = await paquetesResponse.Content.ReadFromJsonAsync<List<PaqueteDto>>();
        Assert.NotNull(paquetes);
        Assert.NotEmpty(paquetes);
        return paquetes[0].Id;
    }

    private static async Task<string> ExtraerAccessTokenAsync(HttpResponseMessage loginResponse)
    {
        using var doc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
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
