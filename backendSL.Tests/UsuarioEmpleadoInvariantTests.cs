using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ProjectSaborLatino.DTOs.Eventos;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class UsuarioEmpleadoInvariantTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public UsuarioEmpleadoInvariantTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CambiarRol_RechazaCon400CuandoUsuarioTrabajadorTieneEmpleadoEnlazadoActivo_SinAlterarRolNiVinculo()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var superHttp = CrearClienteAutenticado(superToken);

        // 1. Crear usuario con rol Trabajador
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var crearUsuarioResponse = await superHttp.PostAsJsonAsync("/api/usuarios", new UsuarioCrearDto
        {
            Email = $"trabajador.activo.{sufijo}@saborlatino.local",
            Password = GenerarCredencialDePrueba(),
            NombreCompleto = $"Trabajador Enlazado Activo {sufijo}",
            Telefono = "88881001",
            Rol = Roles.Trabajador
        });
        Assert.Equal(HttpStatusCode.Created, crearUsuarioResponse.StatusCode);
        var usuario = await crearUsuarioResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuario);
        Assert.Equal(Roles.Trabajador, usuario.Rol);

        // 2. Crear empleado activo enlazado al usuario Trabajador
        var cedula = $"1-0{RandomNumberGenerator.GetInt32(100, 999)}-0{RandomNumberGenerator.GetInt32(100, 999)}";
        var crearEmpleadoResponse = await superHttp.PostAsJsonAsync("/api/empleados", new EmpleadoGuardarDto
        {
            Cedula = cedula,
            NombreCompleto = $"Empleado Enlazado Activo {sufijo}",
            Puesto = "Animador",
            Telefono = "88881001",
            Activo = true,
            UsuarioId = usuario.Id
        });
        Assert.Equal(HttpStatusCode.Created, crearEmpleadoResponse.StatusCode);
        var empleado = await crearEmpleadoResponse.Content.ReadFromJsonAsync<EmpleadoDto>();
        Assert.NotNull(empleado);
        Assert.True(empleado.Activo);
        Assert.Equal(usuario.Id, empleado.UsuarioId);

        // 3. Intentar cambiar el rol a Cliente y a Administrador mientras el empleado sigue enlazado
        var patchClienteResponse = await superHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuario.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Cliente });

        var usuarioTrasIntentoCliente = await (await superHttp.GetAsync($"/api/usuarios/{usuario.Id}"))
            .Content.ReadFromJsonAsync<UsuarioDto>();
        var empleadoTrasIntentoCliente = await (await superHttp.GetAsync($"/api/empleados/{empleado.Id}"))
            .Content.ReadFromJsonAsync<EmpleadoDto>();

        var patchAdminResponse = await superHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuario.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Administrador });

        var usuarioTrasIntentoAdmin = await (await superHttp.GetAsync($"/api/usuarios/{usuario.Id}"))
            .Content.ReadFromJsonAsync<UsuarioDto>();
        var empleadoTrasIntentoAdmin = await (await superHttp.GetAsync($"/api/empleados/{empleado.Id}"))
            .Content.ReadFromJsonAsync<EmpleadoDto>();

        Assert.True(
            patchClienteResponse.StatusCode == HttpStatusCode.BadRequest &&
            patchAdminResponse.StatusCode == HttpStatusCode.BadRequest &&
            usuarioTrasIntentoCliente?.Rol == Roles.Trabajador &&
            usuarioTrasIntentoAdmin?.Rol == Roles.Trabajador &&
            empleadoTrasIntentoCliente?.UsuarioId == usuario.Id &&
            empleadoTrasIntentoAdmin?.UsuarioId == usuario.Id,
            $"Con empleado activo enlazado (EmpleadoId={empleado.Id}, UsuarioId={usuario.Id}): " +
            $"PATCH rol->Cliente devolvió {(int)patchClienteResponse.StatusCode} ({patchClienteResponse.StatusCode}) " +
            $"[RolEnBd='{usuarioTrasIntentoCliente?.Rol}', Empleado.UsuarioId='{empleadoTrasIntentoCliente?.UsuarioId}']; " +
            $"PATCH rol->Administrador devolvió {(int)patchAdminResponse.StatusCode} ({patchAdminResponse.StatusCode}) " +
            $"[RolEnBd='{usuarioTrasIntentoAdmin?.Rol}', Empleado.UsuarioId='{empleadoTrasIntentoAdmin?.UsuarioId}']. " +
            $"Se esperaba 400 (BadRequest) sin alterar el rol Trabajador ni el vínculo.");

        // 4. Confirmar que mantener el mismo rol Trabajador es idempotente (200 OK) y preserva el vínculo
        var patchMismoRolResponse = await superHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuario.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Trabajador });
        Assert.Equal(HttpStatusCode.OK, patchMismoRolResponse.StatusCode);
    }

    [Fact]
    public async Task CambiarRol_RechazaCon400CuandoEmpleadoEnlazadoEstaInactivo_YPermiteCambioTrasDesenlazarExplicitamentePorApi()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var superHttp = CrearClienteAutenticado(superToken);

        // 1. Crear usuario con rol Trabajador y empleado enlazado
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var crearUsuarioResponse = await superHttp.PostAsJsonAsync("/api/usuarios", new UsuarioCrearDto
        {
            Email = $"trabajador.inactivo.{sufijo}@saborlatino.local",
            Password = GenerarCredencialDePrueba(),
            NombreCompleto = $"Trabajador Enlazado Inactivo {sufijo}",
            Telefono = "88882002",
            Rol = Roles.Trabajador
        });
        Assert.Equal(HttpStatusCode.Created, crearUsuarioResponse.StatusCode);
        var usuario = await crearUsuarioResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuario);

        var cedula = $"2-0{RandomNumberGenerator.GetInt32(100, 999)}-0{RandomNumberGenerator.GetInt32(100, 999)}";
        var crearEmpleadoResponse = await superHttp.PostAsJsonAsync("/api/empleados", new EmpleadoGuardarDto
        {
            Cedula = cedula,
            NombreCompleto = $"Empleado Enlazado Inactivo {sufijo}",
            Puesto = "DJ",
            Telefono = "88882002",
            Activo = true,
            UsuarioId = usuario.Id
        });
        Assert.Equal(HttpStatusCode.Created, crearEmpleadoResponse.StatusCode);
        var empleado = await crearEmpleadoResponse.Content.ReadFromJsonAsync<EmpleadoDto>();
        Assert.NotNull(empleado);

        // 2. Desactivar el empleado (DELETE /api/empleados/{id} -> Activo = false, pero conserva UsuarioId)
        var desactivarEmpleadoResponse = await superHttp.DeleteAsync($"/api/empleados/{empleado.Id}");
        Assert.Equal(HttpStatusCode.NoContent, desactivarEmpleadoResponse.StatusCode);

        var empleadoInactivo = await (await superHttp.GetAsync($"/api/empleados/{empleado.Id}"))
            .Content.ReadFromJsonAsync<EmpleadoDto>();
        Assert.NotNull(empleadoInactivo);
        Assert.False(empleadoInactivo.Activo);
        Assert.Equal(usuario.Id, empleadoInactivo.UsuarioId);

        // 3. Intentar cambiar el rol a Cliente mientras el empleado enlazado está inactivo -> debe rechazarse con 400
        var patchConEmpleadoInactivoResponse = await superHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuario.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Cliente });

        var usuarioTrasRechazoInactivo = await (await superHttp.GetAsync($"/api/usuarios/{usuario.Id}"))
            .Content.ReadFromJsonAsync<UsuarioDto>();
        var empleadoTrasRechazoInactivo = await (await superHttp.GetAsync($"/api/empleados/{empleado.Id}"))
            .Content.ReadFromJsonAsync<EmpleadoDto>();

        Assert.True(
            patchConEmpleadoInactivoResponse.StatusCode == HttpStatusCode.BadRequest &&
            usuarioTrasRechazoInactivo?.Rol == Roles.Trabajador &&
            empleadoTrasRechazoInactivo?.UsuarioId == usuario.Id,
            $"Con empleado inactivo enlazado (EmpleadoId={empleado.Id}, Activo=false, UsuarioId={usuario.Id}): " +
            $"PATCH rol->Cliente devolvió {(int)patchConEmpleadoInactivoResponse.StatusCode} ({patchConEmpleadoInactivoResponse.StatusCode}) " +
            $"[RolEnBd='{usuarioTrasRechazoInactivo?.Rol}', Empleado.UsuarioId='{empleadoTrasRechazoInactivo?.UsuarioId}']. " +
            $"Se esperaba 400 (BadRequest) sin alterar rol ni vínculo.");

        // 4. Desenlazar explícitamente mediante PUT /api/empleados/{id} con UsuarioId = null
        var desenlazarResponse = await superHttp.PutAsJsonAsync($"/api/empleados/{empleado.Id}", new EmpleadoGuardarDto
        {
            Cedula = empleadoInactivo.Cedula,
            NombreCompleto = empleadoInactivo.NombreCompleto,
            Puesto = empleadoInactivo.Puesto,
            Telefono = empleadoInactivo.Telefono,
            Activo = false,
            UsuarioId = null
        });
        Assert.Equal(HttpStatusCode.OK, desenlazarResponse.StatusCode);
        var empleadoDesenlazado = await desenlazarResponse.Content.ReadFromJsonAsync<EmpleadoDto>();
        Assert.NotNull(empleadoDesenlazado);
        Assert.Null(empleadoDesenlazado.UsuarioId);

        // 5. Ahora que el empleado fue desenlazado explícitamente, el cambio de rol a Cliente y a Administrador pasa (200 OK)
        var patchClienteTrasDesenlace = await superHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuario.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Cliente });
        Assert.Equal(HttpStatusCode.OK, patchClienteTrasDesenlace.StatusCode);
        var usuarioComoCliente = await patchClienteTrasDesenlace.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuarioComoCliente);
        Assert.Equal(Roles.Cliente, usuarioComoCliente.Rol);

        var patchAdminTrasDesenlace = await superHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuario.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Administrador });
        Assert.Equal(HttpStatusCode.OK, patchAdminTrasDesenlace.StatusCode);
        var usuarioComoAdmin = await patchAdminTrasDesenlace.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuarioComoAdmin);
        Assert.Equal(Roles.Administrador, usuarioComoAdmin.Rol);
    }

    [Fact]
    public async Task CambiarRol_UsuarioTrabajadorSinEmpleadoEnlazado_PermiteCambiarRolNormalmente()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var superHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var crearUsuarioResponse = await superHttp.PostAsJsonAsync("/api/usuarios", new UsuarioCrearDto
        {
            Email = $"trabajador.libre.{sufijo}@saborlatino.local",
            Password = GenerarCredencialDePrueba(),
            NombreCompleto = $"Trabajador Sin Empleado {sufijo}",
            Telefono = "88883003",
            Rol = Roles.Trabajador
        });
        Assert.Equal(HttpStatusCode.Created, crearUsuarioResponse.StatusCode);
        var usuario = await crearUsuarioResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuario);

        var aClienteResponse = await superHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuario.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Cliente });
        Assert.Equal(HttpStatusCode.OK, aClienteResponse.StatusCode);
        var dtoCliente = await aClienteResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.Equal(Roles.Cliente, dtoCliente?.Rol);

        var aAdminResponse = await superHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuario.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Administrador });
        Assert.Equal(HttpStatusCode.OK, aAdminResponse.StatusCode);
        var dtoAdmin = await aAdminResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.Equal(Roles.Administrador, dtoAdmin?.Rol);
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
        Assert.True(doc.RootElement.TryGetProperty("accessToken", out var accessElement));
        var token = accessElement.GetString();
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
