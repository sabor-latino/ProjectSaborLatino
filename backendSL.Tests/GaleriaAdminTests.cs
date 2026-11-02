using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class GaleriaAdminTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public GaleriaAdminTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    private static ImagenGaleriaGuardarDto Nueva(string sufijo, bool activo = true) => new()
    {
        Titulo = $"Boda {sufijo}",
        TituloEn = $"Wedding {sufijo}",
        ImagenUrl = $"https://example.com/{sufijo}.jpg",
        Orden = 5,
        Activo = activo
    };

    [Fact]
    public async Task Galeria_GetPublicoDevuelveSoloActivos()
    {
        using var anon = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anon);
        using var superHttp = CrearClienteAutenticado(superToken);
        var sufijo = Guid.NewGuid().ToString("N")[..8];

        var activa = await (await superHttp.PostAsJsonAsync("/api/galeria", Nueva("act" + sufijo))).Content.ReadFromJsonAsync<ImagenGaleriaDto>();
        var inactiva = await (await superHttp.PostAsJsonAsync("/api/galeria", Nueva("ina" + sufijo, false))).Content.ReadFromJsonAsync<ImagenGaleriaDto>();

        var resp = await anon.GetAsync("/api/galeria");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var lista = (await resp.Content.ReadFromJsonAsync<List<ImagenGaleriaDto>>())!;
        Assert.All(lista, x => Assert.True(x.Activo));
        Assert.Contains(lista, x => x.Id == activa!.Id);
        Assert.DoesNotContain(lista, x => x.Id == inactiva!.Id);

        // Anónimo pidiendo inactivos no los obtiene
        var anonInc = (await (await anon.GetAsync("/api/galeria?incluirInactivos=true")).Content.ReadFromJsonAsync<List<ImagenGaleriaDto>>())!;
        Assert.DoesNotContain(anonInc, x => x.Id == inactiva!.Id);
    }

    [Fact]
    public async Task Galeria_PermisosPorRolEnEscritura()
    {
        using var anon = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anon);
        using var superHttp = CrearClienteAutenticado(superToken);
        var sufijo = Guid.NewGuid().ToString("N")[..8];

        var (_, clienteToken) = await RegistrarYAutenticarClienteAsync(anon, $"cliente.gal.{sufijo}@saborlatino.local", $"Cliente Gal {sufijo}");
        var adminToken = await CrearYAutenticarUsuarioPorRolAsync(superHttp, anon, $"admin.gal.{sufijo}@saborlatino.local", $"Admin Gal {sufijo}", Roles.Administrador);
        var trabToken = await CrearYAutenticarUsuarioPorRolAsync(superHttp, anon, $"trab.gal.{sufijo}@saborlatino.local", $"Trab Gal {sufijo}", Roles.Trabajador);
        using var clienteHttp = CrearClienteAutenticado(clienteToken);
        using var adminHttp = CrearClienteAutenticado(adminToken);
        using var trabHttp = CrearClienteAutenticado(trabToken);

        var semilla = (await (await superHttp.PostAsJsonAsync("/api/galeria", Nueva("sem" + sufijo))).Content.ReadFromJsonAsync<ImagenGaleriaDto>())!;

        // Anónimo = 401
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync("/api/galeria", Nueva("a" + sufijo))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PutAsJsonAsync($"/api/galeria/{semilla.Id}", Nueva("a" + sufijo))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.DeleteAsync($"/api/galeria/{semilla.Id}")).StatusCode);

        // Cliente y Trabajador = 403
        foreach (var http in new[] { clienteHttp, trabHttp })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/galeria", Nueva("f" + sufijo))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.PutAsJsonAsync($"/api/galeria/{semilla.Id}", Nueva("f" + sufijo))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.DeleteAsync($"/api/galeria/{semilla.Id}")).StatusCode);
        }

        // Administrador y Superadministrador = 2xx
        foreach (var http in new[] { adminHttp, superHttp })
        {
            var crear = await http.PostAsJsonAsync("/api/galeria", Nueva("ok" + sufijo));
            Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
            var creada = (await crear.Content.ReadFromJsonAsync<ImagenGaleriaDto>())!;
            Assert.True((await http.PutAsJsonAsync($"/api/galeria/{creada.Id}", Nueva("ed" + sufijo))).IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"/api/galeria/{creada.Id}")).StatusCode);
        }
    }

    [Fact]
    public async Task Galeria_IdInexistenteDevuelve404()
    {
        using var anon = _factory.CreateClient();
        using var superHttp = CrearClienteAutenticado(await IniciarSesionSuperAdminAsync(anon));

        Assert.Equal(HttpStatusCode.NotFound, (await superHttp.PutAsJsonAsync("/api/galeria/999999", Nueva("x"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await superHttp.DeleteAsync("/api/galeria/999999")).StatusCode);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("/relativa/foto.jpg")]
    [InlineData("ftp://example.com/a.jpg")]
    public async Task Galeria_UrlNoHttpEsRechazadaConMensajeEnEspanol(string url)
    {
        using var anon = _factory.CreateClient();
        using var superHttp = CrearClienteAutenticado(await IniciarSesionSuperAdminAsync(anon));
        var dto = Nueva("v" + Guid.NewGuid().ToString("N")[..6]);
        dto.ImagenUrl = url;

        var resp = await superHttp.PostAsJsonAsync("/api/galeria", dto);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("http o https", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Galeria_TituloEnPersisteYDeleteDesactivaSinBorrar()
    {
        using var anon = _factory.CreateClient();
        using var superHttp = CrearClienteAutenticado(await IniciarSesionSuperAdminAsync(anon));
        var sufijo = Guid.NewGuid().ToString("N")[..8];

        var creada = (await (await superHttp.PostAsJsonAsync("/api/galeria", Nueva(sufijo))).Content.ReadFromJsonAsync<ImagenGaleriaDto>())!;
        Assert.Equal($"Wedding {sufijo}", creada.TituloEn);

        var leida = (await anon.GetFromJsonAsync<ImagenGaleriaDto>($"/api/galeria/{creada.Id}"))!;
        Assert.Equal($"Wedding {sufijo}", leida.TituloEn);

        var cambio = Nueva(sufijo);
        cambio.TituloEn = $"Gala {sufijo}";
        var editada = (await (await superHttp.PutAsJsonAsync($"/api/galeria/{creada.Id}", cambio)).Content.ReadFromJsonAsync<ImagenGaleriaDto>())!;
        Assert.Equal($"Gala {sufijo}", editada.TituloEn);

        Assert.Equal(HttpStatusCode.NoContent, (await superHttp.DeleteAsync($"/api/galeria/{creada.Id}")).StatusCode);

        var publica = (await anon.GetFromJsonAsync<List<ImagenGaleriaDto>>("/api/galeria"))!;
        Assert.DoesNotContain(publica, x => x.Id == creada.Id);

        var admin = (await superHttp.GetFromJsonAsync<List<ImagenGaleriaDto>>("/api/galeria?incluirInactivos=true"))!;
        var fila = Assert.Single(admin, x => x.Id == creada.Id);
        Assert.False(fila.Activo);
        Assert.Equal($"Gala {sufijo}", fila.TituloEn);
    }
    private HttpClient CrearClienteAutenticado(string accessToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<string> IniciarSesionSuperAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/identity/login", new
        {
            email = BackendTestFactory.TestSuperAdminEmail,
            password = _factory.TestSuperAdminPassword
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<(string UserId, string AccessToken)> RegistrarYAutenticarClienteAsync(
        HttpClient anonClient,
        string email,
        string nombreCompleto)
    {
        var password = GenerarCredencialDePrueba();
        var regResp = await anonClient.PostAsJsonAsync("/api/cuenta/registro", new
        {
            nombreCompleto,
            email,
            telefono = "88880000",
            password
        });
        Assert.Equal(HttpStatusCode.Created, regResp.StatusCode);

        using var regDoc = JsonDocument.Parse(await regResp.Content.ReadAsStringAsync());
        var userId = regDoc.RootElement.GetProperty("id").GetString()!;

        var loginResp = await anonClient.PostAsJsonAsync("/api/identity/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        using var loginDoc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        var token = loginDoc.RootElement.GetProperty("accessToken").GetString()!;
        return (userId, token);
    }

    private static async Task<string> CrearYAutenticarUsuarioPorRolAsync(
        HttpClient adminHttp,
        HttpClient anonClient,
        string email,
        string nombreCompleto,
        string rol)
    {
        var password = GenerarCredencialDePrueba();
        var crearResp = await adminHttp.PostAsJsonAsync("/api/usuarios", new UsuarioCrearDto
        {
            Email = email,
            Password = password,
            NombreCompleto = nombreCompleto,
            Telefono = "88889999",
            Rol = rol
        });
        Assert.Equal(HttpStatusCode.Created, crearResp.StatusCode);

        var loginResp = await anonClient.PostAsJsonAsync("/api/identity/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        using var loginDoc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
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
