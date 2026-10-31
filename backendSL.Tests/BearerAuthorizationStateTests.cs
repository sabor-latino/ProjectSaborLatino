using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Cuenta;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class BearerAuthorizationStateTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public BearerAuthorizationStateTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TokenEmitidoAntesDeDesactivarUsuario_EsRechazadoCon401EnRutasProtegidas_YCuentaActivaConservaAcceso()
    {
        using var anonClient = _factory.CreateClient();

        // 1. Registrar dos clientes de prueba (uno que será desactivado y otro de control que permanecerá activo)
        var emailDesactivable = $"cliente.desactivar.{Guid.NewGuid():N}@saborlatino.local";
        var passwordDesactivable = GenerarCredencialDePrueba();
        var registroDesactivable = await anonClient.PostAsJsonAsync("/api/cuenta/registro", new RegistroDto
        {
            NombreCompleto = "Cliente A Desactivar",
            Email = emailDesactivable,
            Password = passwordDesactivable,
            Telefono = "88881111"
        });
        Assert.Equal(HttpStatusCode.Created, registroDesactivable.StatusCode);
        var perfilDesactivable = await registroDesactivable.Content.ReadFromJsonAsync<PerfilDto>();
        Assert.NotNull(perfilDesactivable);

        var emailActivoControl = $"cliente.activo.{Guid.NewGuid():N}@saborlatino.local";
        var passwordActivoControl = GenerarCredencialDePrueba();
        var registroActivo = await anonClient.PostAsJsonAsync("/api/cuenta/registro", new RegistroDto
        {
            NombreCompleto = "Cliente Activo Control",
            Email = emailActivoControl,
            Password = passwordActivoControl,
            Telefono = "88882222"
        });
        Assert.Equal(HttpStatusCode.Created, registroActivo.StatusCode);

        // 2. Ambos clientes inician sesión y obtienen sus tokens Bearer antes de la desactivación
        var (accessTokenDesactivable, refreshTokenDesactivable) = await IniciarSesionAsync(anonClient, emailDesactivable, passwordDesactivable);
        var (accessTokenActivo, _) = await IniciarSesionAsync(anonClient, emailActivoControl, passwordActivoControl);

        using var clienteADesactivarHttp = CrearClienteAutenticado(accessTokenDesactivable);
        using var clienteActivoHttp = CrearClienteAutenticado(accessTokenActivo);

        // 3. Llamadas autorizadas previas a la desactivación responden 200 OK
        var yoAntesResponse = await clienteADesactivarHttp.GetAsync("/api/cuenta/yo");
        var miasAntesResponse = await clienteADesactivarHttp.GetAsync("/api/solicitudes/mias");
        Assert.Equal(HttpStatusCode.OK, yoAntesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, miasAntesResponse.StatusCode);

        // 4. El Superadministrador desactiva la cuenta mediante PATCH /api/usuarios/{id}/activo
        var (superAdminToken, _) = await IniciarSesionAsync(
            anonClient,
            BackendTestFactory.TestSuperAdminEmail,
            _factory.TestSuperAdminPassword);
        using var superAdminHttp = CrearClienteAutenticado(superAdminToken);

        var desactivarResponse = await superAdminHttp.PatchAsJsonAsync(
            $"/api/usuarios/{perfilDesactivable.Id}/activo",
            new UsuarioCambiarActivoDto { Activo = false });
        Assert.Equal(HttpStatusCode.OK, desactivarResponse.StatusCode);
        var usuarioDesactivadoDto = await desactivarResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuarioDesactivadoDto);
        Assert.False(usuarioDesactivadoDto.Activo);

        // 5. Nuevas llamadas con el MISMO token Bearer emitido antes de desactivar la cuenta
        var yoDespuesResponse = await clienteADesactivarHttp.GetAsync("/api/cuenta/yo");
        var miasDespuesResponse = await clienteADesactivarHttp.GetAsync("/api/solicitudes/mias");
        var putYoDespuesResponse = await clienteADesactivarHttp.PutAsJsonAsync("/api/cuenta/yo", new ActualizarPerfilDto
        {
            NombreCompleto = "Intento Con Token Antiguo",
            Telefono = "88880000"
        });
        var refreshDespuesResponse = await anonClient.PostAsJsonAsync("/api/identity/refresh", new
        {
            refreshToken = refreshTokenDesactivable
        });

        Assert.True(
            yoDespuesResponse.StatusCode == HttpStatusCode.Unauthorized &&
            miasDespuesResponse.StatusCode == HttpStatusCode.Unauthorized &&
            putYoDespuesResponse.StatusCode == HttpStatusCode.Unauthorized &&
            refreshDespuesResponse.StatusCode == HttpStatusCode.Unauthorized,
            $"Tras desactivar al usuario (Activo=false), el token emitido antes de la desactivación obtuvo: " +
            $"GET /api/cuenta/yo={(int)yoDespuesResponse.StatusCode} ({yoDespuesResponse.StatusCode}), " +
            $"GET /api/solicitudes/mias={(int)miasDespuesResponse.StatusCode} ({miasDespuesResponse.StatusCode}), " +
            $"PUT /api/cuenta/yo={(int)putYoDespuesResponse.StatusCode} ({putYoDespuesResponse.StatusCode}), " +
            $"POST /api/identity/refresh={(int)refreshDespuesResponse.StatusCode} ({refreshDespuesResponse.StatusCode}). " +
            $"Se esperaba 401 (Unauthorized) en todos.");

        // 6. Un nuevo intento de login de la cuenta desactivada también es rechazado con 401
        var loginDesactivadoResponse = await anonClient.PostAsJsonAsync("/api/identity/login", new
        {
            email = emailDesactivable,
            password = passwordDesactivable
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginDesactivadoResponse.StatusCode);

        // 7. Mientras tanto, la cuenta activa legítima conserva acceso normal con su token (200 OK)
        var yoActivoControlResponse = await clienteActivoHttp.GetAsync("/api/cuenta/yo");
        var miasActivoControlResponse = await clienteActivoHttp.GetAsync("/api/solicitudes/mias");
        Assert.Equal(HttpStatusCode.OK, yoActivoControlResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, miasActivoControlResponse.StatusCode);
    }

    [Fact]
    public async Task TokenEmitidoAntesDeDesactivar_NoResucitaAlReactivarCuenta_MientrasNuevoLoginFunciona()
    {
        using var anonClient = _factory.CreateClient();

        // 1. Registrar cliente activo (activo = true) e iniciar sesión para obtener el token original
        var email = $"cliente.ciclo.activo.{Guid.NewGuid():N}@saborlatino.local";
        var password = GenerarCredencialDePrueba();
        var registroResponse = await anonClient.PostAsJsonAsync("/api/cuenta/registro", new RegistroDto
        {
            NombreCompleto = "Cliente Ciclo Desactivar Reactivar",
            Email = email,
            Password = password,
            Telefono = "88885555"
        });
        Assert.Equal(HttpStatusCode.Created, registroResponse.StatusCode);
        var perfil = await registroResponse.Content.ReadFromJsonAsync<PerfilDto>();
        Assert.NotNull(perfil);

        var (tokenOriginal, refreshOriginal) = await IniciarSesionAsync(anonClient, email, password);
        using var clienteConTokenOriginalHttp = CrearClienteAutenticado(tokenOriginal);

        // 2. Estado inicial (activo = true): el token original funciona (200 OK)
        Assert.Equal(HttpStatusCode.OK, (await clienteConTokenOriginalHttp.GetAsync("/api/cuenta/yo")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clienteConTokenOriginalHttp.GetAsync("/api/solicitudes/mias")).StatusCode);

        // 3. El Superadministrador desactiva la cuenta (activo: true -> false)
        var (superAdminToken, _) = await IniciarSesionAsync(
            anonClient,
            BackendTestFactory.TestSuperAdminEmail,
            _factory.TestSuperAdminPassword);
        using var superAdminHttp = CrearClienteAutenticado(superAdminToken);

        var desactivarResponse = await superAdminHttp.PatchAsJsonAsync(
            $"/api/usuarios/{perfil.Id}/activo",
            new UsuarioCambiarActivoDto { Activo = false });
        Assert.Equal(HttpStatusCode.OK, desactivarResponse.StatusCode);
        var dtoDesactivado = await desactivarResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(dtoDesactivado);
        Assert.False(dtoDesactivado.Activo);

        // Mientras está inactivo (activo = false), el token original es rechazado con 401
        Assert.Equal(HttpStatusCode.Unauthorized, (await clienteConTokenOriginalHttp.GetAsync("/api/cuenta/yo")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clienteConTokenOriginalHttp.GetAsync("/api/solicitudes/mias")).StatusCode);

        // 4. El Superadministrador reactiva la cuenta (activo: false -> true)
        var reactivarResponse = await superAdminHttp.PatchAsJsonAsync(
            $"/api/usuarios/{perfil.Id}/activo",
            new UsuarioCambiarActivoDto { Activo = true });
        Assert.Equal(HttpStatusCode.OK, reactivarResponse.StatusCode);
        var dtoReactivado = await reactivarResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(dtoReactivado);
        Assert.True(dtoReactivado.Activo);

        // 5. Tras reactivar (activo: true -> false -> true), el token original emitido ANTES de desactivar
        //    debe continuar revocado (401 Unauthorized) y NO resucitar
        var yoTrasReactivarConTokenAntiguo = await clienteConTokenOriginalHttp.GetAsync("/api/cuenta/yo");
        var miasTrasReactivarConTokenAntiguo = await clienteConTokenOriginalHttp.GetAsync("/api/solicitudes/mias");
        var putYoTrasReactivarConTokenAntiguo = await clienteConTokenOriginalHttp.PutAsJsonAsync("/api/cuenta/yo", new ActualizarPerfilDto
        {
            NombreCompleto = "Intento Resurreccion Token Antiguo",
            Telefono = "88889999"
        });
        var refreshTrasReactivarConTokenAntiguo = await anonClient.PostAsJsonAsync("/api/identity/refresh", new
        {
            refreshToken = refreshOriginal
        });

        Assert.True(
            yoTrasReactivarConTokenAntiguo.StatusCode == HttpStatusCode.Unauthorized &&
            miasTrasReactivarConTokenAntiguo.StatusCode == HttpStatusCode.Unauthorized &&
            putYoTrasReactivarConTokenAntiguo.StatusCode == HttpStatusCode.Unauthorized &&
            refreshTrasReactivarConTokenAntiguo.StatusCode == HttpStatusCode.Unauthorized,
            $"Tras el ciclo activo=true -> false -> true, el token original emitido antes de desactivar resucitó indebidamente: " +
            $"GET /api/cuenta/yo={(int)yoTrasReactivarConTokenAntiguo.StatusCode} ({yoTrasReactivarConTokenAntiguo.StatusCode}), " +
            $"GET /api/solicitudes/mias={(int)miasTrasReactivarConTokenAntiguo.StatusCode} ({miasTrasReactivarConTokenAntiguo.StatusCode}), " +
            $"PUT /api/cuenta/yo={(int)putYoTrasReactivarConTokenAntiguo.StatusCode} ({putYoTrasReactivarConTokenAntiguo.StatusCode}), " +
            $"POST /api/identity/refresh={(int)refreshTrasReactivarConTokenAntiguo.StatusCode} ({refreshTrasReactivarConTokenAntiguo.StatusCode}). " +
            $"Se esperaba 401 (Unauthorized) en todos.");

        // 6. En cambio, un nuevo login de la cuenta reactivada emite un token nuevo que funciona normalmente (200 OK)
        var (tokenNuevo, refreshNuevo) = await IniciarSesionAsync(anonClient, email, password);
        using var clienteConTokenNuevoHttp = CrearClienteAutenticado(tokenNuevo);

        Assert.Equal(HttpStatusCode.OK, (await clienteConTokenNuevoHttp.GetAsync("/api/cuenta/yo")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clienteConTokenNuevoHttp.GetAsync("/api/solicitudes/mias")).StatusCode);

        var refreshNuevoResponse = await anonClient.PostAsJsonAsync("/api/identity/refresh", new
        {
            refreshToken = refreshNuevo
        });
        Assert.Equal(HttpStatusCode.OK, refreshNuevoResponse.StatusCode);

        // Y el token original sigue rechazado con 401
        Assert.Equal(HttpStatusCode.Unauthorized, (await clienteConTokenOriginalHttp.GetAsync("/api/cuenta/yo")).StatusCode);
    }

    [Fact]
    public async Task TokenEmitidoAntesDeCambiarRol_NoConservaPrivilegiosAntiguosYRecibePermisosDelRolActualEnBd()
    {
        using var anonClient = _factory.CreateClient();

        // 1. El Superadministrador crea un usuario de prueba con rol Administrador (sin tocar cuentas sembradas)
        var (superAdminToken, _) = await IniciarSesionAsync(
            anonClient,
            BackendTestFactory.TestSuperAdminEmail,
            _factory.TestSuperAdminPassword);
        using var superAdminHttp = CrearClienteAutenticado(superAdminToken);

        var emailUsuario = $"usuario.cambio.rol.{Guid.NewGuid():N}@saborlatino.local";
        var passwordUsuario = GenerarCredencialDePrueba();

        var crearUsuarioResponse = await superAdminHttp.PostAsJsonAsync("/api/usuarios", new UsuarioCrearDto
        {
            Email = emailUsuario,
            Password = passwordUsuario,
            NombreCompleto = "Usuario Prueba Cambio Rol",
            Telefono = "88883333",
            Rol = Roles.Administrador
        });
        Assert.Equal(HttpStatusCode.Created, crearUsuarioResponse.StatusCode);
        var usuarioCreado = await crearUsuarioResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuarioCreado);
        Assert.Equal(Roles.Administrador, usuarioCreado.Rol);

        // 2. El usuario inicia sesión cuando su rol es Administrador; el token contiene el claim inicial Administrador
        var (tokenInicialAdmin, _) = await IniciarSesionAsync(anonClient, emailUsuario, passwordUsuario);
        using var usuarioConTokenInicialHttp = CrearClienteAutenticado(tokenInicialAdmin);

        // 3. Con rol Administrador activo en BD: accede a /api/usuarios (200) y /api/solicitudes (200), pero no a /api/solicitudes/mias (403)
        var getUsuariosComoAdminAntes = await usuarioConTokenInicialHttp.GetAsync("/api/usuarios");
        var getSolicitudesAdminAntes = await usuarioConTokenInicialHttp.GetAsync("/api/solicitudes");
        var getSolicitudesMiasComoAdminAntes = await usuarioConTokenInicialHttp.GetAsync("/api/solicitudes/mias");
        Assert.Equal(HttpStatusCode.OK, getUsuariosComoAdminAntes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getSolicitudesAdminAntes.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, getSolicitudesMiasComoAdminAntes.StatusCode);

        // 4. El Superadministrador cambia el rol del usuario de Administrador a Cliente (PATCH /api/usuarios/{id}/rol)
        var cambioAClienteResponse = await superAdminHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuarioCreado.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Cliente });
        Assert.Equal(HttpStatusCode.OK, cambioAClienteResponse.StatusCode);
        var usuarioComoCliente = await cambioAClienteResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(usuarioComoCliente);
        Assert.Equal(Roles.Cliente, usuarioComoCliente.Rol);

        // 5. Usando el MISMO token emitido cuando era Administrador:
        //    - Ya NO debe conservar acceso a rutas de Administrador (/api/usuarios ni /api/solicitudes -> 403 Forbidden)
        //    - Sí debe recibir los permisos de su rol actual en BD Cliente (/api/solicitudes/mias -> 200 OK)
        var getUsuariosTrasCambioACliente = await usuarioConTokenInicialHttp.GetAsync("/api/usuarios");
        var getSolicitudesAdminTrasCambioACliente = await usuarioConTokenInicialHttp.GetAsync("/api/solicitudes");
        var getSolicitudesMiasTrasCambioACliente = await usuarioConTokenInicialHttp.GetAsync("/api/solicitudes/mias");

        Assert.True(
            getUsuariosTrasCambioACliente.StatusCode == HttpStatusCode.Forbidden &&
            getSolicitudesAdminTrasCambioACliente.StatusCode == HttpStatusCode.Forbidden &&
            getSolicitudesMiasTrasCambioACliente.StatusCode == HttpStatusCode.OK,
            $"Tras cambiar el rol en BD de Administrador a Cliente, el token antiguo emitido como Administrador obtuvo: " +
            $"GET /api/usuarios={(int)getUsuariosTrasCambioACliente.StatusCode} ({getUsuariosTrasCambioACliente.StatusCode}) [esperado 403], " +
            $"GET /api/solicitudes={(int)getSolicitudesAdminTrasCambioACliente.StatusCode} ({getSolicitudesAdminTrasCambioACliente.StatusCode}) [esperado 403], " +
            $"GET /api/solicitudes/mias={(int)getSolicitudesMiasTrasCambioACliente.StatusCode} ({getSolicitudesMiasTrasCambioACliente.StatusCode}) [esperado 200].");

        // 6. El Superadministrador cambia ahora el rol de Cliente a Trabajador
        var cambioATrabajadorResponse = await superAdminHttp.PatchAsJsonAsync(
            $"/api/usuarios/{usuarioCreado.Id}/rol",
            new UsuarioCambiarRolDto { Rol = Roles.Trabajador });
        Assert.Equal(HttpStatusCode.OK, cambioATrabajadorResponse.StatusCode);

        // 7. Con el mismo token inicial: pierde el permiso exclusivo de Cliente (/api/solicitudes/mias -> 403),
        //    sigue sin permiso de Administrador (/api/usuarios -> 403) y conserva permiso de Trabajador (/api/mis-eventos -> 200)
        var getSolicitudesMiasTrasCambioATrabajador = await usuarioConTokenInicialHttp.GetAsync("/api/solicitudes/mias");
        var getUsuariosTrasCambioATrabajador = await usuarioConTokenInicialHttp.GetAsync("/api/usuarios");
        var getMisEventosComoTrabajador = await usuarioConTokenInicialHttp.GetAsync("/api/mis-eventos");

        Assert.Equal(HttpStatusCode.Forbidden, getSolicitudesMiasTrasCambioATrabajador.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, getUsuariosTrasCambioATrabajador.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getMisEventosComoTrabajador.StatusCode);
    }

    [Fact]
    public async Task ValidacionPrincipal_NoDependeDeSecurityStamp_SincronizaRolesYRechazaInactivoAunqueStampNoCambie()
    {
        using var anonClient = _factory.CreateClient();

        // 1. Crear usuario Administrador por API y obtener su token Bearer
        var (superAdminToken, _) = await IniciarSesionAsync(
            anonClient,
            BackendTestFactory.TestSuperAdminEmail,
            _factory.TestSuperAdminPassword);
        using var superAdminHttp = CrearClienteAutenticado(superAdminToken);

        var email = $"sin.stamp.{Guid.NewGuid():N}@saborlatino.local";
        var password = GenerarCredencialDePrueba();

        var crearResponse = await superAdminHttp.PostAsJsonAsync("/api/usuarios", new UsuarioCrearDto
        {
            Email = email,
            Password = password,
            NombreCompleto = "Usuario Sin Rotar Stamp",
            Telefono = "88884444",
            Rol = Roles.Administrador
        });
        Assert.Equal(HttpStatusCode.Created, crearResponse.StatusCode);
        var creado = await crearResponse.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(creado);

        var (token, _) = await IniciarSesionAsync(anonClient, email, password);
        using var usuarioHttp = CrearClienteAutenticado(token);
        Assert.Equal(HttpStatusCode.OK, (await usuarioHttp.GetAsync("/api/usuarios")).StatusCode);

        // 2. Cambiar el rol en BD directamente en UserRoles SIN modificar SecurityStamp
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stampAntes = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == creado.Id)
                .Select(u => u.SecurityStamp)
                .SingleAsync();

            var rolClienteId = await db.Roles
                .Where(r => r.Name == Roles.Cliente)
                .Select(r => r.Id)
                .SingleAsync();

            var userRolesActuales = await db.UserRoles.Where(ur => ur.UserId == creado.Id).ToListAsync();
            db.UserRoles.RemoveRange(userRolesActuales);
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = creado.Id, RoleId = rolClienteId });
            await db.SaveChangesAsync();

            var stampDespuesRol = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == creado.Id)
                .Select(u => u.SecurityStamp)
                .SingleAsync();
            Assert.Equal(stampAntes, stampDespuesRol);
        }

        // Aunque SecurityStamp sigue intacto, el principal ya no tiene rol Administrador (403) y recibe rol Cliente (200)
        Assert.Equal(HttpStatusCode.Forbidden, (await usuarioHttp.GetAsync("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await usuarioHttp.GetAsync("/api/solicitudes/mias")).StatusCode);

        // 3. Marcar Activo = false directamente en BD SIN modificar SecurityStamp ni LockoutEnd
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var usuarioDb = await db.Users.SingleAsync(u => u.Id == creado.Id);
            var stampAntes = usuarioDb.SecurityStamp;

            usuarioDb.Activo = false;
            await db.SaveChangesAsync();

            Assert.Equal(stampAntes, usuarioDb.SecurityStamp);
        }

        // Aunque SecurityStamp y LockoutEnd no cambiaron, la cuenta inactiva es rechazada con 401
        Assert.Equal(HttpStatusCode.Unauthorized, (await usuarioHttp.GetAsync("/api/solicitudes/mias")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await usuarioHttp.GetAsync("/api/cuenta/yo")).StatusCode);
    }

    private HttpClient CrearClienteAutenticado(string accessToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task<(string AccessToken, string RefreshToken)> IniciarSesionAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/identity/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("accessToken", out var accessElement));
        Assert.True(doc.RootElement.TryGetProperty("refreshToken", out var refreshElement));

        var accessToken = accessElement.GetString();
        var refreshToken = refreshElement.GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));

        return (accessToken!, refreshToken!);
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
