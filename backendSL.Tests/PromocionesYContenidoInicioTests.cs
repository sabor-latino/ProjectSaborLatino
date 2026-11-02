using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class PromocionesYContenidoInicioTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public PromocionesYContenidoInicioTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CatalogoBilingueYFichasTecnicas_SemillaYCrudDeServiciosEquiposPaquetesYPreguntasFrecuentes()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var adminHttp = CrearClienteAutenticado(superToken);

        // 1. Verificar datos bilingües y fichas técnicas sembradas en servicios, paquetes y preguntas frecuentes
        var servicios = await anonClient.GetFromJsonAsync<List<ServicioDto>>("/api/servicios");
        Assert.NotNull(servicios);
        Assert.Equal(3, servicios.Count);
        Assert.All(servicios, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.NombreEn));
            Assert.False(string.IsNullOrWhiteSpace(s.DescripcionEn));
        });

        var paquetes = await anonClient.GetFromJsonAsync<List<PaqueteDto>>("/api/paquetes");
        Assert.NotNull(paquetes);
        Assert.Equal(4, paquetes.Count);
        Assert.All(paquetes, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.ServicioNombreEn));
            Assert.False(string.IsNullOrWhiteSpace(p.NombreEn));
            Assert.False(string.IsNullOrWhiteSpace(p.DescripcionEn));
            Assert.False(string.IsNullOrWhiteSpace(p.PotenciaSonido));
            Assert.False(string.IsNullOrWhiteSpace(p.CoberturaAforo));
            Assert.False(string.IsNullOrWhiteSpace(p.DuracionMontaje));
            Assert.False(string.IsNullOrWhiteSpace(p.RequerimientosElectricos));
            Assert.False(string.IsNullOrWhiteSpace(p.EspacioMinimo));
            Assert.False(string.IsNullOrWhiteSpace(p.FichaTecnica));
            Assert.False(string.IsNullOrWhiteSpace(p.FichaTecnicaEn));
            Assert.All(p.Equipos, eq =>
            {
                Assert.False(string.IsNullOrWhiteSpace(eq.EquipoNombreEn));
                Assert.False(string.IsNullOrWhiteSpace(eq.EspecificacionTecnica));
                Assert.False(string.IsNullOrWhiteSpace(eq.EspecificacionTecnicaEn));
            });
        });

        var preguntas = await anonClient.GetFromJsonAsync<List<PreguntaFrecuenteDto>>("/api/preguntas-frecuentes");
        Assert.NotNull(preguntas);
        Assert.True(preguntas.Count >= 5);
        Assert.All(preguntas, pf =>
        {
            Assert.False(string.IsNullOrWhiteSpace(pf.PreguntaEn));
            Assert.False(string.IsNullOrWhiteSpace(pf.RespuestaEn));
        });

        // 2. Crear y actualizar Servicio, Equipo, Paquete y PreguntaFrecuente con campos técnicos y bilingües
        var sufijo = Guid.NewGuid().ToString("N")[..8];

        var crearServicioResp = await adminHttp.PostAsJsonAsync("/api/servicios", new ServicioGuardarDto
        {
            Nombre = $"Iluminación Escénica {sufijo}",
            NombreEn = $"Stage Lighting {sufijo}",
            Descripcion = "Diseño de iluminación robótica y arquitectónica.",
            DescripcionEn = "Robotic and architectural lighting design.",
            Activo = true
        });
        Assert.Equal(HttpStatusCode.Created, crearServicioResp.StatusCode);
        var servicioCreado = (await crearServicioResp.Content.ReadFromJsonAsync<ServicioDto>())!;
        Assert.Equal($"Stage Lighting {sufijo}", servicioCreado.NombreEn);
        Assert.Equal("Robotic and architectural lighting design.", servicioCreado.DescripcionEn);

        var crearEquipoResp = await adminHttp.PostAsJsonAsync("/api/equipos", new EquipoGuardarDto
        {
            Nombre = $"Cabeza Móvil Beam {sufijo}",
            NombreEn = $"Beam Moving Head {sufijo}",
            Tipo = "Iluminación",
            EspecificacionTecnica = "230W 7R, rueda de 14 colores + 17 gobos, DMX512 16 canales.",
            EspecificacionTecnicaEn = "230W 7R, 14-color wheel + 17 gobos, 16-channel DMX512.",
            CantidadTotal = 4,
            Activo = true
        });
        Assert.Equal(HttpStatusCode.Created, crearEquipoResp.StatusCode);
        var equipoCreado = (await crearEquipoResp.Content.ReadFromJsonAsync<EquipoDto>())!;
        Assert.Equal($"Beam Moving Head {sufijo}", equipoCreado.NombreEn);
        Assert.Contains("DMX512", equipoCreado.EspecificacionTecnica);
        Assert.Contains("16-channel", equipoCreado.EspecificacionTecnicaEn);

        var crearPaqueteResp = await adminHttp.PostAsJsonAsync("/api/paquetes", new PaqueteGuardarDto
        {
            ServicioId = servicioCreado.Id,
            Nombre = $"Show Lumínico {sufijo}",
            NombreEn = $"Light Show {sufijo}",
            Descripcion = "Espectáculo sincronizado DMX.",
            DescripcionEn = "DMX synchronized light show.",
            PrecioBase = 95000m,
            PotenciaSonido = "N/A (iluminación dedicada 1200 W)",
            CoberturaAforo = "Hasta 300 personas",
            DuracionMontaje = "75 minutos",
            RequerimientosElectricos = "1 circuito 110V / 20A",
            EspacioMinimo = "3 m x 2 m",
            FichaTecnica = "4 cabezas móviles Beam sobre estructura truss de aluminio.",
            FichaTecnicaEn = "4 Beam moving heads mounted on aluminum truss structure.",
            Activo = true,
            Equipos = [new PaqueteEquipoGuardarDto { EquipoId = equipoCreado.Id, Cantidad = 4 }]
        });
        Assert.Equal(HttpStatusCode.Created, crearPaqueteResp.StatusCode);
        var paqueteCreado = (await crearPaqueteResp.Content.ReadFromJsonAsync<PaqueteDto>())!;
        Assert.Equal($"Light Show {sufijo}", paqueteCreado.NombreEn);
        Assert.Equal($"Stage Lighting {sufijo}", paqueteCreado.ServicioNombreEn);
        Assert.Equal("Hasta 300 personas", paqueteCreado.CoberturaAforo);
        Assert.Equal("75 minutos", paqueteCreado.DuracionMontaje);
        Assert.Equal("1 circuito 110V / 20A", paqueteCreado.RequerimientosElectricos);
        Assert.Equal("3 m x 2 m", paqueteCreado.EspacioMinimo);
        Assert.Single(paqueteCreado.Equipos);
        Assert.Equal($"Beam Moving Head {sufijo}", paqueteCreado.Equipos[0].EquipoNombreEn);
        Assert.Contains("DMX512", paqueteCreado.Equipos[0].EspecificacionTecnica);
        Assert.Contains("16-channel", paqueteCreado.Equipos[0].EspecificacionTecnicaEn);

        var crearFaqResp = await adminHttp.PostAsJsonAsync("/api/preguntas-frecuentes", new PreguntaFrecuenteGuardarDto
        {
            Pregunta = $"¿Incluyen controlador DMX {sufijo}?",
            PreguntaEn = $"Do you include a DMX controller {sufijo}?",
            Respuesta = "Sí, con operador de luces.",
            RespuestaEn = "Yes, with a lighting operator.",
            Orden = 99,
            Activo = true
        });
        Assert.Equal(HttpStatusCode.Created, crearFaqResp.StatusCode);
        var faqCreada = (await crearFaqResp.Content.ReadFromJsonAsync<PreguntaFrecuenteDto>())!;
        Assert.Equal($"Do you include a DMX controller {sufijo}?", faqCreada.PreguntaEn);
        Assert.Equal("Yes, with a lighting operator.", faqCreada.RespuestaEn);
    }

    [Fact]
    public async Task PromocionesTemporada_SemillaCrudValidacionesPermisosYSugerenciaEnFidelidad()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var superHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (clienteId, clienteToken) = await RegistrarYAutenticarClienteAsync(
            anonClient,
            $"cliente.promo.{sufijo}@saborlatino.local",
            $"Cliente Promo {sufijo}");
        using var clienteHttp = CrearClienteAutenticado(clienteToken);

        var adminToken = await CrearYAutenticarUsuarioPorRolAsync(
            superHttp,
            anonClient,
            $"admin.promo.{sufijo}@saborlatino.local",
            $"Admin Promo {sufijo}",
            Roles.Administrador);
        using var adminHttp = CrearClienteAutenticado(adminToken);

        var trabajadorToken = await CrearYAutenticarUsuarioPorRolAsync(
            superHttp,
            anonClient,
            $"trabajador.promo.{sufijo}@saborlatino.local",
            $"Trabajador Promo {sufijo}",
            Roles.Trabajador);
        using var trabajadorHttp = CrearClienteAutenticado(trabajadorToken);

        // 1. Verificar promociones sembradas públicamente
        var promosSembradas = await anonClient.GetFromJsonAsync<List<PromocionTemporadaDto>>("/api/promociones");
        Assert.NotNull(promosSembradas);
        Assert.True(promosSembradas.Count >= 2);
        Assert.Contains(promosSembradas, p => p.CodigoPromocional == "FIESTA15" && p.PorcentajeDescuento == 15m && p.Vigente && p.Paquetes.Count >= 2);
        Assert.Contains(promosSembradas, p => p.CodigoPromocional == "BODAS10" && p.PorcentajeDescuento == 10m && p.Vigente && p.Paquetes.Count >= 2);

        // 2. Permisos: 401 sin token y 403 con rol Cliente o Trabajador
        var paquetes = (await anonClient.GetFromJsonAsync<List<PaqueteDto>>("/api/paquetes"))!;
        var paqueteDj = paquetes.First(p => p.Nombre == "DJ Open Format");

        var dtoValido = new PromocionTemporadaGuardarDto
        {
            Nombre = $"Promo Verano {sufijo}",
            NombreEn = $"Summer Promo {sufijo}",
            Descripcion = "Promoción especial de verano en eventos al aire libre.",
            DescripcionEn = "Special summer promotion for outdoor events.",
            EtiquetaTemporada = "Verano",
            EtiquetaTemporadaEn = "Summer",
            PorcentajeDescuento = 25m,
            CodigoPromocional = $"VERANO25_{sufijo}",
            FechaInicio = DateTime.UtcNow.AddDays(-2),
            FechaFin = DateTime.UtcNow.AddDays(60),
            DestacadaEnInicio = true,
            Activo = true,
            PaqueteIds = [paqueteDj.Id]
        };

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.PostAsJsonAsync("/api/promociones", dtoValido)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteHttp.PostAsJsonAsync("/api/promociones", dtoValido)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabajadorHttp.PostAsJsonAsync("/api/promociones", dtoValido)).StatusCode);

        // 3. Validaciones de negocio: FechaFin <= FechaInicio y Porcentaje fuera de rango
        var dtoFechasInvalidas = new PromocionTemporadaGuardarDto
        {
            Nombre = "Promo Fechas Inválidas",
            PorcentajeDescuento = 15m,
            FechaInicio = DateTime.UtcNow.AddDays(10),
            FechaFin = DateTime.UtcNow.AddDays(5),
            PaqueteIds = [paqueteDj.Id]
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await adminHttp.PostAsJsonAsync("/api/promociones", dtoFechasInvalidas)).StatusCode);

        var dtoPorcentajeInvalido = new PromocionTemporadaGuardarDto
        {
            Nombre = "Promo Porcentaje Inválido",
            PorcentajeDescuento = 95m,
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow.AddDays(10),
            PaqueteIds = [paqueteDj.Id]
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await adminHttp.PostAsJsonAsync("/api/promociones", dtoPorcentajeInvalido)).StatusCode);

        // 4. Crear promoción válida con Administrador y verificar cálculo de PrecioPromocional
        var crearResp = await adminHttp.PostAsJsonAsync("/api/promociones", dtoValido);
        Assert.Equal(HttpStatusCode.Created, crearResp.StatusCode);
        var promoCreada = (await crearResp.Content.ReadFromJsonAsync<PromocionTemporadaDto>())!;
        Assert.Equal($"Promo Verano {sufijo}", promoCreada.Nombre);
        Assert.Equal(25m, promoCreada.PorcentajeDescuento);
        Assert.True(promoCreada.Vigente);
        var itemPaquete = Assert.Single(promoCreada.Paquetes);
        Assert.Equal(paqueteDj.Id, itemPaquete.PaqueteId);
        Assert.Equal(80000m, itemPaquete.PrecioBase);
        Assert.Equal(60000m, itemPaquete.PrecioPromocional);

        // 5. Verificar sugerencia de descuento de temporada en GET /api/solicitudes/{id}/fidelidad
        var crearSolResp = await clienteHttp.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Cliente Promo {sufijo}",
            CorreoContacto = $"cliente.promo.{sufijo}@saborlatino.local",
            TelefonoContacto = "88887777",
            PaqueteId = paqueteDj.Id,
            TipoEvento = "Fiesta de Verano",
            FechaEvento = DateTime.Now.AddDays(20),
            Lugar = "San José, Costa Rica",
            CantidadInvitados = 100
        });
        Assert.Equal(HttpStatusCode.Created, crearSolResp.StatusCode);
        var solicitud = (await crearSolResp.Content.ReadFromJsonAsync<SolicitudDto>())!;

        var fidelidad = await adminHttp.GetFromJsonAsync<FidelidadDto>($"/api/solicitudes/{solicitud.Id}/fidelidad");
        Assert.NotNull(fidelidad);
        Assert.Equal(solicitud.Id, fidelidad.SolicitudId);
        Assert.Equal(clienteId, fidelidad.ClienteUsuarioId);
        Assert.False(fidelidad.Aplica);
        Assert.Equal(0m, fidelidad.DescuentoSugerido);
        // La promoción de 25% supera a la sembrada de 15% para el paquete DJ Open Format
        Assert.Equal($"Promo Verano {sufijo}", fidelidad.PromocionTemporadaNombre);
        Assert.Equal(25m, fidelidad.PorcentajePromocionTemporada);
        Assert.Equal(20000m, fidelidad.DescuentoPromocionSugerido);

        // 6. Actualizar y desactivar promoción (DELETE hace soft delete Activo = false)
        dtoValido.PorcentajeDescuento = 20m;
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.PutAsJsonAsync($"/api/promociones/{promoCreada.Id}", dtoValido)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteHttp.PutAsJsonAsync($"/api/promociones/{promoCreada.Id}", dtoValido)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabajadorHttp.PutAsJsonAsync($"/api/promociones/{promoCreada.Id}", dtoValido)).StatusCode);

        var actualizarResp = await adminHttp.PutAsJsonAsync($"/api/promociones/{promoCreada.Id}", dtoValido);
        Assert.Equal(HttpStatusCode.OK, actualizarResp.StatusCode);
        var promoActualizada = (await actualizarResp.Content.ReadFromJsonAsync<PromocionTemporadaDto>())!;
        Assert.Equal(20m, promoActualizada.PorcentajeDescuento);
        Assert.Equal(64000m, Assert.Single(promoActualizada.Paquetes).PrecioPromocional);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.DeleteAsync($"/api/promociones/{promoCreada.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteHttp.DeleteAsync($"/api/promociones/{promoCreada.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabajadorHttp.DeleteAsync($"/api/promociones/{promoCreada.Id}")).StatusCode);

        var desactivarResp = await adminHttp.DeleteAsync($"/api/promociones/{promoCreada.Id}");
        Assert.Equal(HttpStatusCode.NoContent, desactivarResp.StatusCode);

        // Un visitante anónimo no ve la promoción desactivada ni siquiera pasando ?incluirInactivas=true
        var consultaAnonInactivas = await anonClient.GetAsync($"/api/promociones/{promoCreada.Id}?incluirInactivas=true");
        Assert.Equal(HttpStatusCode.NotFound, consultaAnonInactivas.StatusCode);

        // El Administrador sí puede consultarla con ?incluirInactivas=true
        var consultaAdminInactivas = await adminHttp.GetFromJsonAsync<PromocionTemporadaDto>($"/api/promociones/{promoCreada.Id}?incluirInactivas=true");
        Assert.NotNull(consultaAdminInactivas);
        Assert.False(consultaAdminInactivas.Activo);
    }

    [Fact]
    public async Task ContenidoInicio_LecturaAnonimaPredeterminada_EdicionPorAdminYSuperAdmin_YRegistroEnBitacora()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var superHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (_, clienteToken) = await RegistrarYAutenticarClienteAsync(
            anonClient,
            $"cliente.inicio.{sufijo}@saborlatino.local",
            $"Cliente Inicio {sufijo}");
        using var clienteHttp = CrearClienteAutenticado(clienteToken);

        var adminToken = await CrearYAutenticarUsuarioPorRolAsync(
            superHttp,
            anonClient,
            $"admin.inicio.{sufijo}@saborlatino.local",
            $"Admin Inicio {sufijo}",
            Roles.Administrador);
        using var adminHttp = CrearClienteAutenticado(adminToken);

        var trabajadorToken = await CrearYAutenticarUsuarioPorRolAsync(
            superHttp,
            anonClient,
            $"trabajador.inicio.{sufijo}@saborlatino.local",
            $"Trabajador Inicio {sufijo}",
            Roles.Trabajador);
        using var trabajadorHttp = CrearClienteAutenticado(trabajadorToken);

        // 1. Lectura anónima devuelve valores profesionales en ES y EN sin fallar aunque no existan filas Inicio.* aún
        var inicial = await anonClient.GetFromJsonAsync<ContenidoInicioDto>("/api/contenido-inicio");
        Assert.NotNull(inicial);
        Assert.False(string.IsNullOrWhiteSpace(inicial.TituloPortada));
        Assert.False(string.IsNullOrWhiteSpace(inicial.TituloPortadaEn));
        Assert.False(string.IsNullOrWhiteSpace(inicial.Paso1Titulo));
        Assert.False(string.IsNullOrWhiteSpace(inicial.Paso1TituloEn));
        Assert.False(string.IsNullOrWhiteSpace(inicial.CtaTitulo));
        Assert.False(string.IsNullOrWhiteSpace(inicial.CtaTituloEn));

        // 2. Permisos: 401 sin token y 403 con rol Cliente o Trabajador
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.PutAsJsonAsync("/api/contenido-inicio", inicial)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteHttp.PutAsJsonAsync("/api/contenido-inicio", inicial)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabajadorHttp.PutAsJsonAsync("/api/contenido-inicio", inicial)).StatusCode);

        // 3. Validación: texto obligatorio vacío devuelve 400 BadRequest
        var invalido = new ContenidoInicioDto
        {
            EtiquetaPortada = "",
            EtiquetaPortadaEn = "Tag",
            TituloPortada = "Título",
            TituloPortadaEn = "Title"
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await adminHttp.PutAsJsonAsync("/api/contenido-inicio", invalido)).StatusCode);

        // 4. Edición por Administrador persiste cambios y registra auditoría en Bitacora
        inicial.TituloPortada = $"Portada Actualizada {sufijo}";
        inicial.TituloPortadaEn = $"Updated Hero Title {sufijo}";
        inicial.FotoPortadaUrl = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819";
        inicial.FotoCotizarUrl = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745";

        var putAdminResp = await adminHttp.PutAsJsonAsync("/api/contenido-inicio", inicial);
        Assert.Equal(HttpStatusCode.OK, putAdminResp.StatusCode);
        var actualizado = (await putAdminResp.Content.ReadFromJsonAsync<ContenidoInicioDto>())!;
        Assert.Equal($"Portada Actualizada {sufijo}", actualizado.TituloPortada);
        Assert.Equal($"Updated Hero Title {sufijo}", actualizado.TituloPortadaEn);
        Assert.Equal("https://images.unsplash.com/photo-1514525253161-7a46d19cd819", actualizado.FotoPortadaUrl);

        // Lectura pública refleja inmediatamente el cambio
        var leidoAnon = await anonClient.GetFromJsonAsync<ContenidoInicioDto>("/api/contenido-inicio");
        Assert.NotNull(leidoAnon);
        Assert.Equal($"Portada Actualizada {sufijo}", leidoAnon.TituloPortada);
        Assert.Equal($"Updated Hero Title {sufijo}", leidoAnon.TituloPortadaEn);

        // Superadministrador también puede actualizarlo (upsert sobre filas ya creadas)
        actualizado.CtaTitulo = $"Cotice Hoy {sufijo}";
        actualizado.CtaTituloEn = $"Get a Quote Today {sufijo}";
        var putSuperResp = await superHttp.PutAsJsonAsync("/api/contenido-inicio", actualizado);
        Assert.Equal(HttpStatusCode.OK, putSuperResp.StatusCode);

        // Verificar que la Bitacora registró la auditoría de Configuracion para Inicio.TituloPortada / Inicio.CtaTitulo
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Bitacora.AnyAsync(b => b.Entidad == nameof(Configuracion) && b.EntidadId == "Inicio.TituloPortada"));
        Assert.True(await db.Bitacora.AnyAsync(b => b.Entidad == nameof(Configuracion) && b.EntidadId == "Inicio.CtaTitulo" && b.Accion == "Modificar"));
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
