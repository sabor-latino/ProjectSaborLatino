using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.DTOs.Cuenta;
using ProjectSaborLatino.DTOs.Eventos;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class ModificacionEventosYUsuariosTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public ModificacionEventosYUsuariosTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ModificarEvento_ActualizaDatos_ValidaFechas_VerificaDisponibilidadYRechazaFinalizado()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var superHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];

        // 1. Crear servicio, equipo (5 unidades) y paquete con 3 unidades reservadas
        var servResp = await superHttp.PostAsJsonAsync("/api/servicios", new ServicioGuardarDto
        {
            Nombre = $"Servicio Mod Evento {sufijo}",
            Descripcion = "Servicio para prueba de modificación de evento",
            Activo = true
        });
        Assert.Equal(HttpStatusCode.Created, servResp.StatusCode);
        var servicio = (await servResp.Content.ReadFromJsonAsync<ServicioDto>())!;

        var equipResp = await superHttp.PostAsJsonAsync("/api/equipos", new EquipoGuardarDto
        {
            Nombre = $"Bafle Mod Evento {sufijo}",
            Tipo = "Audio",
            CantidadTotal = 5,
            Activo = true
        });
        Assert.Equal(HttpStatusCode.Created, equipResp.StatusCode);
        var equipo = (await equipResp.Content.ReadFromJsonAsync<EquipoDto>())!;

        var paqResp = await superHttp.PostAsJsonAsync("/api/paquetes", new PaqueteGuardarDto
        {
            ServicioId = servicio.Id,
            Nombre = $"Paquete Mod Evento {sufijo}",
            Descripcion = "Paquete con equipo",
            PrecioBase = 150000m,
            Activo = true,
            Equipos = [new PaqueteEquipoGuardarDto { EquipoId = equipo.Id, Cantidad = 3 }]
        });
        Assert.Equal(HttpStatusCode.Created, paqResp.StatusCode);
        var paquete = (await paqResp.Content.ReadFromJsonAsync<PaqueteDto>())!;

        // 2. Crear solicitud, cotización aceptada y evento #1 (en día +30)
        var solResp = await anonClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Cliente Evento {sufijo}",
            CorreoContacto = $"mod.evento.{sufijo}@saborlatino.local",
            TelefonoContacto = "88884444",
            PaqueteId = paquete.Id,
            TipoEvento = "Cumpleaños",
            FechaEvento = DateTime.Now.AddDays(30),
            Lugar = "San José Centro",
            CantidadInvitados = 80
        });
        Assert.Equal(HttpStatusCode.Created, solResp.StatusCode);
        var solicitud = (await solResp.Content.ReadFromJsonAsync<SolicitudDto>())!;

        var cotResp = await superHttp.PostAsJsonAsync($"/api/solicitudes/{solicitud.Id}/cotizaciones", new CotizacionCrearDto
        {
            Monto = 150000m,
            Descuento = 0m,
            Detalle = "Cotización evento 1",
            VigenteHasta = DateTime.Now.AddDays(10)
        });
        Assert.Equal(HttpStatusCode.Created, cotResp.StatusCode);
        var cotizacion = (await cotResp.Content.ReadFromJsonAsync<CotizacionDto>())!;
        Assert.Equal(HttpStatusCode.OK, (await superHttp.PatchAsync($"/api/cotizaciones/{cotizacion.Id}/aceptar", null)).StatusCode);

        var inicioOriginal = DateTime.Now.Date.AddDays(30).AddHours(14);
        var finOriginal = inicioOriginal.AddHours(5);
        var evResp = await superHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cotizacion.Id,
            FechaInicio = inicioOriginal,
            FechaFin = finOriginal,
            Lugar = "San José Centro",
            Notas = "Notas iniciales",
            CopiarEquipoDelPaquete = true
        });
        Assert.Equal(HttpStatusCode.Created, evResp.StatusCode);
        var evento = (await evResp.Content.ReadFromJsonAsync<EventoDto>())!;

        // 3. Crear solicitud, cotización aceptada y evento #2 (en día +40) que también reserva 3 unidades
        var sol2Resp = await anonClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Cliente Evento 2 {sufijo}",
            CorreoContacto = $"mod.evento2.{sufijo}@saborlatino.local",
            TelefonoContacto = "88885555",
            PaqueteId = paquete.Id,
            TipoEvento = "Boda",
            FechaEvento = DateTime.Now.AddDays(40),
            Lugar = "Cartago",
            CantidadInvitados = 100
        });
        Assert.Equal(HttpStatusCode.Created, sol2Resp.StatusCode);
        var sol2 = (await sol2Resp.Content.ReadFromJsonAsync<SolicitudDto>())!;

        var cot2Resp = await superHttp.PostAsJsonAsync($"/api/solicitudes/{sol2.Id}/cotizaciones", new CotizacionCrearDto
        {
            Monto = 150000m,
            Descuento = 0m,
            Detalle = "Cotización evento 2",
            VigenteHasta = DateTime.Now.AddDays(10)
        });
        Assert.Equal(HttpStatusCode.Created, cot2Resp.StatusCode);
        var cot2 = (await cot2Resp.Content.ReadFromJsonAsync<CotizacionDto>())!;
        Assert.Equal(HttpStatusCode.OK, (await superHttp.PatchAsync($"/api/cotizaciones/{cot2.Id}/aceptar", null)).StatusCode);

        var inicioEv2 = DateTime.Now.Date.AddDays(40).AddHours(14);
        var finEv2 = inicioEv2.AddHours(5);
        var ev2Resp = await superHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cot2.Id,
            FechaInicio = inicioEv2,
            FechaFin = finEv2,
            Lugar = "Cartago",
            CopiarEquipoDelPaquete = true
        });
        Assert.Equal(HttpStatusCode.Created, ev2Resp.StatusCode);

        // 4. Validar validaciones de fecha al modificar evento 1:
        // A) FechaFin <= FechaInicio -> 400
        var modFechasInvertidas = await superHttp.PutAsJsonAsync($"/api/eventos/{evento.Id}", new EventoModificarDto
        {
            FechaInicio = DateTime.Now.AddDays(32),
            FechaFin = DateTime.Now.AddDays(31)
        });
        Assert.Equal(HttpStatusCode.BadRequest, modFechasInvertidas.StatusCode);

        // B) FechaInicio en el pasado -> 400
        var modFechaPasada = await superHttp.PutAsJsonAsync($"/api/eventos/{evento.Id}", new EventoModificarDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-2),
            FechaFin = DateTime.UtcNow.AddDays(-1)
        });
        Assert.Equal(HttpStatusCode.BadRequest, modFechaPasada.StatusCode);

        // 5. Validar conflicto de inventario (overbooking) si se mueven fechas a coincidir con evento #2:
        // Evento 1 pide 3 unidades, Evento 2 ya tiene 3 en día +40, total disponible es 5 -> 3 + 3 = 6 > 5 -> 400 BadRequest
        var modConflictoEquipo = await superHttp.PutAsJsonAsync($"/api/eventos/{evento.Id}", new EventoModificarDto
        {
            FechaInicio = inicioEv2.AddHours(1),
            FechaFin = finEv2.AddHours(1)
        });
        Assert.Equal(HttpStatusCode.BadRequest, modConflictoEquipo.StatusCode);
        var errorInventario = await modConflictoEquipo.Content.ReadAsStringAsync();
        Assert.Contains("No hay suficiente", errorInventario);

        // 6. Modificación exitosa: fechas a día +32, nuevo lugar y nuevas notas
        var nuevoInicio = DateTime.Now.Date.AddDays(32).AddHours(16);
        var nuevoFin = nuevoInicio.AddHours(6);
        var modExitosa = await superHttp.PutAsJsonAsync($"/api/eventos/{evento.Id}", new EventoModificarDto
        {
            FechaInicio = nuevoInicio,
            FechaFin = nuevoFin,
            Lugar = "Salón Principal Las Palmas",
            Notas = "Revisión técnica realizada"
        });
        Assert.Equal(HttpStatusCode.OK, modExitosa.StatusCode);
        var eventoActualizado = await modExitosa.Content.ReadFromJsonAsync<EventoDto>();
        Assert.NotNull(eventoActualizado);
        Assert.Equal(nuevoInicio, eventoActualizado.FechaInicio);
        Assert.Equal(nuevoFin, eventoActualizado.FechaFin);
        Assert.Equal("Salón Principal Las Palmas", eventoActualizado.Lugar);
        Assert.Equal("Revisión técnica realizada", eventoActualizado.Notas);

        // 7. Autorización: Anónimo da 401; Cliente / Trabajador da 403
        var (clienteId, clienteToken) = await RegistrarYAutenticarClienteAsync(anonClient, $"cliente.mod.{sufijo}@saborlatino.local", $"Cliente {sufijo}");
        _ = clienteId;
        using var clienteHttp = CrearClienteAutenticado(clienteToken);

        var (trabajadorId, trabajadorToken) = await CrearYAutenticarUsuarioAsync(
            superHttp, anonClient, $"trab.mod.{sufijo}@saborlatino.local", $"Trabajador {sufijo}", Roles.Trabajador);
        _ = trabajadorId;
        using var trabHttp = CrearClienteAutenticado(trabajadorToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.PutAsJsonAsync($"/api/eventos/{evento.Id}", new EventoModificarDto { Lugar = "Hacker" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteHttp.PutAsJsonAsync($"/api/eventos/{evento.Id}", new EventoModificarDto { Lugar = "Hacker" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabHttp.PutAsJsonAsync($"/api/eventos/{evento.Id}", new EventoModificarDto { Lugar = "Hacker" })).StatusCode);

        // 8. No se puede modificar un evento Cancelado o Finalizado
        var cancelResp = await superHttp.PatchAsJsonAsync($"/api/eventos/{evento.Id}/cancelar", new EventoCancelarDto { Motivo = "Cliente canceló" });
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);

        var modCancelado = await superHttp.PutAsJsonAsync($"/api/eventos/{evento.Id}", new EventoModificarDto
        {
            Lugar = "Nuevo Intento En Cancelado"
        });
        Assert.Equal(HttpStatusCode.BadRequest, modCancelado.StatusCode);
    }

    [Fact]
    public async Task ModificarPerfilUsuario_AdminYSuperadmin_ActualizaDatos_RespetaAlcanceYRegistraBitacora()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var superHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];

        // 1. Superadmin crea un Administrador y un Trabajador
        var (_, adminToken) = await CrearYAutenticarUsuarioAsync(
            superHttp, anonClient, $"admin.perfil.{sufijo}@saborlatino.local", $"Admin Perfil {sufijo}", Roles.Administrador);
        using var adminHttp = CrearClienteAutenticado(adminToken);

        var (trabId, _) = await CrearYAutenticarUsuarioAsync(
            superHttp, anonClient, $"trab.perfil.{sufijo}@saborlatino.local", $"Trab Original {sufijo}", Roles.Trabajador);

        var (clienteId, _) = await RegistrarYAutenticarClienteAsync(
            anonClient, $"cliente.perfil.{sufijo}@saborlatino.local", $"Cliente Original {sufijo}");

        // 2. Superadmin modifica el perfil del Trabajador (ruta PUT /api/usuarios/{id}/perfil)
        var modTrabResp = await superHttp.PutAsJsonAsync($"/api/usuarios/{trabId}/perfil", new UsuarioModificarPerfilDto
        {
            NombreCompleto = $"Trabajador Renombrado {sufijo}",
            Telefono = "88990011"
        });
        Assert.Equal(HttpStatusCode.OK, modTrabResp.StatusCode);
        var trabActualizado = await modTrabResp.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(trabActualizado);
        Assert.Equal($"Trabajador Renombrado {sufijo}", trabActualizado.NombreCompleto);
        Assert.Equal("88990011", trabActualizado.Telefono);

        // 3. Administrador modifica el perfil del Cliente (ruta alternativa PUT /api/usuarios/{id})
        var modCliResp = await adminHttp.PutAsJsonAsync($"/api/usuarios/{clienteId}", new UsuarioModificarPerfilDto
        {
            NombreCompleto = $"Cliente Editado Por Admin {sufijo}",
            Telefono = "87776655"
        });
        Assert.Equal(HttpStatusCode.OK, modCliResp.StatusCode);
        var cliActualizado = await modCliResp.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.NotNull(cliActualizado);
        Assert.Equal($"Cliente Editado Por Admin {sufijo}", cliActualizado.NombreCompleto);
        Assert.Equal("87776655", cliActualizado.Telefono);

        // 4. Regla de alcance: Administrador NO puede modificar a Superadministrador (404 NotFound)
        var perfilSuperResp = await superHttp.GetAsync("/api/cuenta/yo");
        var superPerfil = await perfilSuperResp.Content.ReadFromJsonAsync<PerfilDto>();
        Assert.NotNull(superPerfil);

        var intentoAdminEnSuper = await adminHttp.PutAsJsonAsync($"/api/usuarios/{superPerfil.Id}/perfil", new UsuarioModificarPerfilDto
        {
            NombreCompleto = "Superadmin hackeado",
            Telefono = "00000000"
        });
        Assert.Equal(HttpStatusCode.NotFound, intentoAdminEnSuper.StatusCode);

        // 5. Validación: NombreCompleto obligatorio (400 BadRequest)
        var modVacio = await superHttp.PutAsJsonAsync($"/api/usuarios/{trabId}/perfil", new UsuarioModificarPerfilDto
        {
            NombreCompleto = "   ",
            Telefono = "88888888"
        });
        Assert.Equal(HttpStatusCode.BadRequest, modVacio.StatusCode);

        // 6. Autorización: Anónimo -> 401; Cliente -> 403
        var (_, tokenClienteAutenticado) = await RegistrarYAutenticarClienteAsync(
            anonClient, $"cliente.noauth.{sufijo}@saborlatino.local", "Cliente No Auth");
        using var clienteAuthHttp = CrearClienteAutenticado(tokenClienteAutenticado);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.PutAsJsonAsync($"/api/usuarios/{trabId}/perfil", new UsuarioModificarPerfilDto { NombreCompleto = "Test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteAuthHttp.PutAsJsonAsync($"/api/usuarios/{trabId}/perfil", new UsuarioModificarPerfilDto { NombreCompleto = "Test" })).StatusCode);

        // 7. Registro en Bitácora verificado por Superadministrador
        var bitacoraUsuarios = await superHttp.GetFromJsonAsync<List<BitacoraDto>>("/api/bitacora?entidad=Usuario");
        Assert.NotNull(bitacoraUsuarios);
        Assert.Contains(bitacoraUsuarios, b =>
            b.Entidad == "Usuario" &&
            b.Accion == "ModificarPerfil" &&
            b.EntidadId == trabId &&
            b.Detalle != null &&
            b.Detalle.Contains($"Trabajador Renombrado {sufijo}"));
    }

    [Fact]
    public async Task ModificarPropioPerfil_ActualizaDatos_RechazaAnonimoYValidaCampos()
    {
        using var anonClient = _factory.CreateClient();
        var sufijo = Guid.NewGuid().ToString("N")[..8];

        var (clienteId, clienteToken) = await RegistrarYAutenticarClienteAsync(
            anonClient, $"cliente.propio.{sufijo}@saborlatino.local", $"Cliente Original {sufijo}");
        using var clienteHttp = CrearClienteAutenticado(clienteToken);

        // 1. Modificación de propio perfil con PUT /api/cuenta/yo exitosa
        var putResp = await clienteHttp.PutAsJsonAsync("/api/cuenta/yo", new ActualizarPerfilDto
        {
            NombreCompleto = $"Cliente AutoEditado {sufijo}",
            Telefono = "87654321"
        });
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
        var perfilActualizado = await putResp.Content.ReadFromJsonAsync<PerfilDto>();
        Assert.NotNull(perfilActualizado);
        Assert.Equal(clienteId, perfilActualizado.Id);
        Assert.Equal($"Cliente AutoEditado {sufijo}", perfilActualizado.NombreCompleto);
        Assert.Equal("87654321", perfilActualizado.Telefono);

        // 2. Persistencia en GET /api/cuenta/yo
        var getResp = await clienteHttp.GetAsync("/api/cuenta/yo");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var perfilObtenido = await getResp.Content.ReadFromJsonAsync<PerfilDto>();
        Assert.NotNull(perfilObtenido);
        Assert.Equal($"Cliente AutoEditado {sufijo}", perfilObtenido.NombreCompleto);
        Assert.Equal("87654321", perfilObtenido.Telefono);

        // 3. Validación de campos: NombreCompleto obligatorio (400)
        var putInvalidoResp = await clienteHttp.PutAsJsonAsync("/api/cuenta/yo", new ActualizarPerfilDto
        {
            NombreCompleto = "",
            Telefono = "87654321"
        });
        Assert.Equal(HttpStatusCode.BadRequest, putInvalidoResp.StatusCode);

        // 4. Anónimo rechazado con 401 Unauthorized
        var putAnonResp = await anonClient.PutAsJsonAsync("/api/cuenta/yo", new ActualizarPerfilDto
        {
            NombreCompleto = "Intruso",
            Telefono = "00000000"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, putAnonResp.StatusCode);
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
        var token = doc.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
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
        var userId = regDoc.RootElement.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(userId));

        var loginResp = await anonClient.PostAsJsonAsync("/api/identity/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        using var loginDoc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        var token = loginDoc.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return (userId!, token!);
    }

    private static async Task<(string UserId, string AccessToken)> CrearYAutenticarUsuarioAsync(
        HttpClient superHttp,
        HttpClient anonClient,
        string email,
        string nombreCompleto,
        string rol)
    {
        var password = GenerarCredencialDePrueba();
        var crearResp = await superHttp.PostAsJsonAsync("/api/usuarios", new UsuarioCrearDto
        {
            Email = email,
            Password = password,
            NombreCompleto = nombreCompleto,
            Telefono = "88889999",
            Rol = rol
        });
        Assert.Equal(HttpStatusCode.Created, crearResp.StatusCode);
        var usuario = (await crearResp.Content.ReadFromJsonAsync<UsuarioDto>())!;

        var loginResp = await anonClient.PostAsJsonAsync("/api/identity/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        using var loginDoc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        var token = loginDoc.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return (usuario.Id, token!);
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
