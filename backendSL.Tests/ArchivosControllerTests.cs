using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class ArchivosControllerTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public ArchivosControllerTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubirImagen_Anonimo_Retorna401()
    {
        using var anon = _factory.CreateClient();
        using var content = CrearContenidoMultipart(GenerarBytesJpeg(), "foto.jpg", "image/jpeg");

        var resp = await anon.PostAsync("/api/archivos/subir-imagen", content);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task SubirImagen_RolCliente_Retorna403()
    {
        using var anon = _factory.CreateClient();
        var (_, tokenCliente) = await RegistrarYAutenticarClienteAsync(anon, $"cliente.{Guid.NewGuid():N}@ejemplo.com");
        using var clienteHttp = CrearClienteAutenticado(tokenCliente);
        using var content = CrearContenidoMultipart(GenerarBytesJpeg(), "foto.jpg", "image/jpeg");

        var resp = await clienteHttp.PostAsync("/api/archivos/subir-imagen", content);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SubirImagen_RolTrabajador_Retorna403()
    {
        using var anon = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anon);
        using var superHttp = CrearClienteAutenticado(superToken);

        var tokenTrabajador = await CrearYAutenticarUsuarioPorRolAsync(
            superHttp,
            anon,
            $"trabajador.{Guid.NewGuid():N}@ejemplo.com",
            "Trabajador Archivos",
            Roles.Trabajador);

        using var trabajadorHttp = CrearClienteAutenticado(tokenTrabajador);
        using var content = CrearContenidoMultipart(GenerarBytesJpeg(), "foto.jpg", "image/jpeg");

        var resp = await trabajadorHttp.PostAsync("/api/archivos/subir-imagen", content);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task SubirImagen_SinArchivo_Retorna400()
    {
        using var anon = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anon);
        using var superHttp = CrearClienteAutenticado(superToken);

        using var content = new MultipartFormDataContent();
        var resp = await superHttp.PostAsync("/api/archivos/subir-imagen", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task SubirImagen_ExtensionNoPermitida_Retorna400()
    {
        using var anon = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anon);
        using var superHttp = CrearClienteAutenticado(superToken);

        var bytesTexto = Encoding.UTF8.GetBytes("esto no es una imagen");
        using var content = CrearContenidoMultipart(bytesTexto, "script.exe", "application/octet-stream");

        var resp = await superHttp.PostAsync("/api/archivos/subir-imagen", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task SubirImagen_FirmaFalsa_Retorna400()
    {
        using var anon = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anon);
        using var superHttp = CrearClienteAutenticado(superToken);

        // Archivo que se llama .jpg pero sus bytes son texto plano
        var bytesFalsos = Encoding.UTF8.GetBytes("fake content pretending to be jpeg");
        using var content = CrearContenidoMultipart(bytesFalsos, "fake.jpg", "image/jpeg");

        var resp = await superHttp.PostAsync("/api/archivos/subir-imagen", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task SubirImagen_Valida_Jpeg_Png_Webp_Retorna200YUrlAccesible()
    {
        using var anon = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anon);
        using var superHttp = CrearClienteAutenticado(superToken);

        // 1. JPEG
        using (var content = CrearContenidoMultipart(GenerarBytesJpeg(), "evento.jpg", "image/jpeg"))
        {
            var resp = await superHttp.PostAsync("/api/archivos/subir-imagen", content);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var res = await resp.Content.ReadFromJsonAsync<ResultadoSubidaArchivoDto>();
            Assert.NotNull(res);
            Assert.StartsWith("http", res.Url);
            Assert.Contains("/uploads/imagenes/", res.RutaRelativa);
            Assert.EndsWith(".jpg", res.RutaRelativa);

            // Verificamos que el archivo estático sea accesible de forma pública
            var staticResp = await anon.GetAsync(res.RutaRelativa);
            Assert.Equal(HttpStatusCode.OK, staticResp.StatusCode);
        }

        // 2. PNG
        using (var content = CrearContenidoMultipart(GenerarBytesPng(), "logo.png", "image/png"))
        {
            var resp = await superHttp.PostAsync("/api/archivos/subir-imagen", content);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var res = await resp.Content.ReadFromJsonAsync<ResultadoSubidaArchivoDto>();
            Assert.NotNull(res);
            Assert.EndsWith(".png", res.RutaRelativa);
        }

        // 3. WebP
        using (var content = CrearContenidoMultipart(GenerarBytesWebp(), "foto_celular.webp", "image/webp"))
        {
            var resp = await superHttp.PostAsync("/api/archivos/subir-imagen", content);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var res = await resp.Content.ReadFromJsonAsync<ResultadoSubidaArchivoDto>();
            Assert.NotNull(res);
            Assert.EndsWith(".webp", res.RutaRelativa);
        }
    }

    // ================= Helpers =================

    private static MultipartFormDataContent CrearContenidoMultipart(byte[] bytes, string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var byteContent = new ByteArrayContent(bytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(byteContent, "archivo", fileName);
        return content;
    }

    private static byte[] GenerarBytesJpeg()
    {
        // Firma FF D8 FF E0 + relleno
        return [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    }

    private static byte[] GenerarBytesPng()
    {
        // Firma 89 50 4E 47 0D 0A 1A 0A + relleno
        return [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    }

    private static byte[] GenerarBytesWebp()
    {
        // RIFF .... WEBP
        return [0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];
    }

    private HttpClient CrearClienteAutenticado(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> IniciarSesionSuperAdminAsync(HttpClient anonClient)
    {
        var resp = await anonClient.PostAsJsonAsync("/api/identity/login", new
        {
            email = BackendTestFactory.TestSuperAdminEmail,
            password = _factory.TestSuperAdminPassword
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<(string UserId, string Token)> RegistrarYAutenticarClienteAsync(HttpClient anonClient, string email)
    {
        var password = GenerarCredencialDePrueba();
        var regResp = await anonClient.PostAsJsonAsync("/api/cuenta/registro", new
        {
            nombreCompleto = "Cliente Archivos",
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
