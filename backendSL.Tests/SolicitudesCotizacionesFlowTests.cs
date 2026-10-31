using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class SolicitudesCotizacionesFlowTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public SolicitudesCotizacionesFlowTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FlujoCompletoAceptacion_VisitanteYClienteCreanSolicitud_AdminCotizaDosVecesYAceptaUna_OtraQuedaRechazadaYSolicitudAceptada_IntentosDuplicadosSeRechazan()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var adminHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];

        // 1. Visitante anónimo crea una solicitud (queda con ClienteUsuarioId == null y Estado == Pendiente)
        var correoVisitante = $"visitante.flujo.{sufijo}@saborlatino.local";
        var crearSolVisitanteResp = await anonClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Visitante {sufijo}",
            CorreoContacto = correoVisitante,
            TelefonoContacto = "88881100",
            TipoEvento = "Boda Civil",
            FechaEvento = DateTime.Now.AddDays(30),
            Lugar = "Heredia, Costa Rica",
            CantidadInvitados = 80,
            Comentarios = "Solicitud enviada sin iniciar sesión"
        });
        Assert.Equal(HttpStatusCode.Created, crearSolVisitanteResp.StatusCode);
        var solVisitante = await crearSolVisitanteResp.Content.ReadFromJsonAsync<SolicitudDto>();
        Assert.NotNull(solVisitante);
        Assert.Null(solVisitante.ClienteUsuarioId);
        Assert.Equal(nameof(EstadoSolicitud.Pendiente), solVisitante.Estado);

        // Fecha de evento en el pasado se rechaza con 400 BadRequest
        var fechaPasadaResp = await anonClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Visitante Fecha Pasada {sufijo}",
            CorreoContacto = correoVisitante,
            TipoEvento = "Cumpleaños",
            FechaEvento = DateTime.Now.AddDays(-2),
            Lugar = "San José, Costa Rica",
            CantidadInvitados = 25
        });
        Assert.Equal(HttpStatusCode.BadRequest, fechaPasadaResp.StatusCode);

        // 2. Cliente registrado e identificado crea su propia solicitud (queda vinculada a su ClienteUsuarioId)
        var (clienteDuenoId, clienteDuenoToken) = await RegistrarYAutenticarClienteAsync(
            anonClient,
            $"cliente.dueno.{sufijo}@saborlatino.local",
            $"Cliente Dueño {sufijo}");
        using var clienteDuenoHttp = CrearClienteAutenticado(clienteDuenoToken);

        var crearSolClienteResp = await clienteDuenoHttp.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Cliente Dueño {sufijo}",
            CorreoContacto = $"cliente.dueno.{sufijo}@saborlatino.local",
            TelefonoContacto = "88882200",
            TipoEvento = "Aniversario",
            FechaEvento = DateTime.Now.AddDays(45),
            Lugar = "Escazú, Costa Rica",
            CantidadInvitados = 120,
            Comentarios = "Solicitud de cliente autenticado"
        });
        Assert.Equal(HttpStatusCode.Created, crearSolClienteResp.StatusCode);
        var solicitud = await crearSolClienteResp.Content.ReadFromJsonAsync<SolicitudDto>();
        Assert.NotNull(solicitud);
        Assert.Equal(clienteDuenoId, solicitud.ClienteUsuarioId);
        Assert.Equal(nameof(EstadoSolicitud.Pendiente), solicitud.Estado);
        Assert.Empty(solicitud.Cotizaciones);

        // 3. Admin crea dos cotizaciones sobre la solicitud del cliente (y se validan entradas inválidas)
        var descuentoMayorQueMontoResp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 100000m,
                Descuento = 150000m,
                Detalle = "Descuento inválido mayor que monto",
                VigenteHasta = DateTime.Now.AddDays(10)
            });
        Assert.Equal(HttpStatusCode.BadRequest, descuentoMayorQueMontoResp.StatusCode);

        var vigenciaPasadaResp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 100000m,
                Descuento = 0m,
                Detalle = "Vigencia en el pasado",
                VigenteHasta = DateTime.Now.AddDays(-1)
            });
        Assert.Equal(HttpStatusCode.BadRequest, vigenciaPasadaResp.StatusCode);

        var crearCot1Resp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 300000m,
                Descuento = 30000m,
                Detalle = "Opción Premium con sonido e iluminación",
                VigenteHasta = DateTime.Now.AddDays(10)
            });
        Assert.Equal(HttpStatusCode.Created, crearCot1Resp.StatusCode);
        var cot1 = await crearCot1Resp.Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cot1);
        Assert.Equal(solicitud.Id, cot1.SolicitudId);
        Assert.Equal(270000m, cot1.Total);
        Assert.Equal(nameof(EstadoCotizacion.Enviada), cot1.Estado);

        // Al crear la primera cotización, la solicitud pasa a Cotizada
        var solTrasCot1 = await clienteDuenoHttp.GetFromJsonAsync<SolicitudDto>($"/api/solicitudes/{solicitud.Id}");
        Assert.NotNull(solTrasCot1);
        Assert.Equal(nameof(EstadoSolicitud.Cotizada), solTrasCot1.Estado);
        Assert.Single(solTrasCot1.Cotizaciones);

        var crearCot2Resp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 220000m,
                Descuento = 20000m,
                Detalle = "Opción Estándar de sonido",
                VigenteHasta = DateTime.Now.AddDays(12)
            });
        Assert.Equal(HttpStatusCode.Created, crearCot2Resp.StatusCode);
        var cot2 = await crearCot2Resp.Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cot2);
        Assert.Equal(200000m, cot2.Total);
        Assert.Equal(nameof(EstadoCotizacion.Enviada), cot2.Estado);

        // 4. Admin acepta la primera cotización vigente -> la primera queda Aceptada, la segunda pasa a Rechazada y la solicitud a Aceptada
        var aceptarCot1Resp = await adminHttp.PatchAsync($"/api/cotizaciones/{cot1.Id}/aceptar", content: null);
        Assert.Equal(HttpStatusCode.OK, aceptarCot1Resp.StatusCode);
        var cot1AceptadaDto = await aceptarCot1Resp.Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cot1AceptadaDto);
        Assert.Equal(nameof(EstadoCotizacion.Aceptada), cot1AceptadaDto.Estado);

        // Comprobar estado observable por HTTP para el cliente dueño
        var solTrasAceptarDto = await clienteDuenoHttp.GetFromJsonAsync<SolicitudDto>($"/api/solicitudes/{solicitud.Id}");
        Assert.NotNull(solTrasAceptarDto);
        Assert.Equal(nameof(EstadoSolicitud.Aceptada), solTrasAceptarDto.Estado);
        Assert.Equal(2, solTrasAceptarDto.Cotizaciones.Count);
        Assert.Equal(
            nameof(EstadoCotizacion.Aceptada),
            solTrasAceptarDto.Cotizaciones.Single(c => c.Id == cot1.Id).Estado);
        Assert.Equal(
            nameof(EstadoCotizacion.Rechazada),
            solTrasAceptarDto.Cotizaciones.Single(c => c.Id == cot2.Id).Estado);

        // Comprobar persistencia real en base de datos temporal
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var solEnBd = await db.Solicitudes
                .AsNoTracking()
                .Include(s => s.Cotizaciones)
                .SingleAsync(s => s.Id == solicitud.Id);

            Assert.Equal(EstadoSolicitud.Aceptada, solEnBd.Estado);
            Assert.Equal(EstadoCotizacion.Aceptada, solEnBd.Cotizaciones.Single(c => c.Id == cot1.Id).Estado);
            Assert.Equal(EstadoCotizacion.Rechazada, solEnBd.Cotizaciones.Single(c => c.Id == cot2.Id).Estado);
        }

        // 5. Intentos duplicados o posteriores sobre solicitud/cotización ya cerrada se rechazan con 400 BadRequest
        var aceptarDuplicadoResp = await adminHttp.PatchAsync($"/api/cotizaciones/{cot1.Id}/aceptar", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, aceptarDuplicadoResp.StatusCode);

        var aceptarOtraRechazadaResp = await adminHttp.PatchAsync($"/api/cotizaciones/{cot2.Id}/aceptar", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, aceptarOtraRechazadaResp.StatusCode);

        var rechazarYaAceptadaClienteResp = await clienteDuenoHttp.PatchAsync($"/api/cotizaciones/{cot1.Id}/rechazar", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, rechazarYaAceptadaClienteResp.StatusCode);

        var rechazarYaAceptadaAdminResp = await adminHttp.PatchAsync($"/api/cotizaciones/{cot1.Id}/rechazar", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, rechazarYaAceptadaAdminResp.StatusCode);

        var cotizarSolicitudAceptadaResp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 180000m,
                Descuento = 0m,
                Detalle = "No debe permitirse cotizar solicitud ya aceptada",
                VigenteHasta = DateTime.Now.AddDays(5)
            });
        Assert.Equal(HttpStatusCode.BadRequest, cotizarSolicitudAceptadaResp.StatusCode);

        var cancelarSolicitudAceptadaResp = await adminHttp.PatchAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/estado",
            new SolicitudCambiarEstadoDto { Estado = nameof(EstadoSolicitud.Cancelada) });
        Assert.Equal(HttpStatusCode.BadRequest, cancelarSolicitudAceptadaResp.StatusCode);
    }

    [Fact]
    public async Task AutorizacionYAislamientoEntreClientes_401SinToken_403ConRolInsuficiente_Y404ParaClienteAjenoEnConsultaYRechazo()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var adminHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];

        // Preparar Cliente A (dueño), Cliente B (ajeno) y un usuario Trabajador
        var (clienteAId, clienteAToken) = await RegistrarYAutenticarClienteAsync(
            anonClient,
            $"cliente.a.{sufijo}@saborlatino.local",
            $"Cliente A {sufijo}");
        using var clienteAHttp = CrearClienteAutenticado(clienteAToken);

        var (_, clienteBToken) = await RegistrarYAutenticarClienteAsync(
            anonClient,
            $"cliente.b.{sufijo}@saborlatino.local",
            $"Cliente B {sufijo}");
        using var clienteBHttp = CrearClienteAutenticado(clienteBToken);

        var trabajadorToken = await CrearYAutenticarUsuarioPorRolAsync(
            adminHttp,
            anonClient,
            $"trabajador.permisos.{sufijo}@saborlatino.local",
            $"Trabajador {sufijo}",
            Roles.Trabajador);
        using var trabajadorHttp = CrearClienteAutenticado(trabajadorToken);

        // Cliente A crea su solicitud y Admin le crea una cotización
        var crearSolAResp = await clienteAHttp.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Cliente A {sufijo}",
            CorreoContacto = $"cliente.a.{sufijo}@saborlatino.local",
            TelefonoContacto = "88883300",
            TipoEvento = "Fiesta Empresarial",
            FechaEvento = DateTime.Now.AddDays(25),
            Lugar = "San Pedro, Costa Rica",
            CantidadInvitados = 70
        });
        Assert.Equal(HttpStatusCode.Created, crearSolAResp.StatusCode);
        var solicitudA = (await crearSolAResp.Content.ReadFromJsonAsync<SolicitudDto>())!;
        Assert.Equal(clienteAId, solicitudA.ClienteUsuarioId);

        var crearCotAResp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitudA.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 195000m,
                Descuento = 15000m,
                Detalle = "Cotización para Cliente A",
                VigenteHasta = DateTime.Now.AddDays(15)
            });
        Assert.Equal(HttpStatusCode.Created, crearCotAResp.StatusCode);
        var cotizacionA = (await crearCotAResp.Content.ReadFromJsonAsync<CotizacionDto>())!;

        // Visitante sin cuenta crea otra solicitud y Admin la cotiza
        var crearSolVisitanteResp = await anonClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Visitante Aislado {sufijo}",
            CorreoContacto = $"visitante.aislado.{sufijo}@saborlatino.local",
            TipoEvento = "Graduación",
            FechaEvento = DateTime.Now.AddDays(40),
            Lugar = "Cartago, Costa Rica",
            CantidadInvitados = 90
        });
        Assert.Equal(HttpStatusCode.Created, crearSolVisitanteResp.StatusCode);
        var solicitudVisitante = (await crearSolVisitanteResp.Content.ReadFromJsonAsync<SolicitudDto>())!;

        var crearCotVisitanteResp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitudVisitante.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 210000m,
                Descuento = 0m,
                Detalle = "Cotización para Visitante",
                VigenteHasta = DateTime.Now.AddDays(15)
            });
        Assert.Equal(HttpStatusCode.Created, crearCotVisitanteResp.StatusCode);
        var cotizacionVisitante = (await crearCotVisitanteResp.Content.ReadFromJsonAsync<CotizacionDto>())!;

        // 1. Peticiones sin token -> 401 Unauthorized en todos los endpoints protegidos
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.GetAsync("/api/solicitudes")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.GetAsync("/api/solicitudes/mias")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.GetAsync($"/api/solicitudes/{solicitudA.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonClient.PatchAsJsonAsync(
                $"/api/solicitudes/{solicitudA.Id}/estado",
                new SolicitudCambiarEstadoDto { Estado = nameof(EstadoSolicitud.Cancelada) })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonClient.PostAsJsonAsync(
                $"/api/solicitudes/{solicitudA.Id}/cotizaciones",
                new CotizacionCrearDto { Monto = 100000m, Descuento = 0m, VigenteHasta = DateTime.Now.AddDays(5) })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.PatchAsync($"/api/cotizaciones/{cotizacionA.Id}/aceptar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.PatchAsync($"/api/cotizaciones/{cotizacionA.Id}/rechazar", null)).StatusCode);

        // 2. Peticiones con rol insuficiente -> 403 Forbidden
        // Trabajador no tiene acceso a ninguna ruta protegida de solicitudes/cotizaciones
        Assert.Equal(HttpStatusCode.Forbidden, (await trabajadorHttp.GetAsync("/api/solicitudes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabajadorHttp.GetAsync("/api/solicitudes/mias")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabajadorHttp.GetAsync($"/api/solicitudes/{solicitudA.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await trabajadorHttp.PatchAsJsonAsync(
                $"/api/solicitudes/{solicitudA.Id}/estado",
                new SolicitudCambiarEstadoDto { Estado = nameof(EstadoSolicitud.Cancelada) })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await trabajadorHttp.PostAsJsonAsync(
                $"/api/solicitudes/{solicitudA.Id}/cotizaciones",
                new CotizacionCrearDto { Monto = 100000m, Descuento = 0m, VigenteHasta = DateTime.Now.AddDays(5) })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabajadorHttp.PatchAsync($"/api/cotizaciones/{cotizacionA.Id}/aceptar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabajadorHttp.PatchAsync($"/api/cotizaciones/{cotizacionA.Id}/rechazar", null)).StatusCode);

        // Cliente no puede invocar endpoints exclusivos de administración
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteAHttp.GetAsync("/api/solicitudes")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await clienteAHttp.PatchAsJsonAsync(
                $"/api/solicitudes/{solicitudA.Id}/estado",
                new SolicitudCambiarEstadoDto { Estado = nameof(EstadoSolicitud.Cancelada) })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await clienteAHttp.PostAsJsonAsync(
                $"/api/solicitudes/{solicitudA.Id}/cotizaciones",
                new CotizacionCrearDto { Monto = 100000m, Descuento = 0m, VigenteHasta = DateTime.Now.AddDays(5) })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteAHttp.PatchAsync($"/api/cotizaciones/{cotizacionA.Id}/aceptar", null)).StatusCode);

        // 3. Cliente ajeno (Cliente B) recibe 404 NotFound al consultar o rechazar solicitudes/cotizaciones de Cliente A o de Visitante
        Assert.Equal(HttpStatusCode.NotFound, (await clienteBHttp.GetAsync($"/api/solicitudes/{solicitudA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteBHttp.GetAsync($"/api/solicitudes/{solicitudVisitante.Id}")).StatusCode);

        var miasClienteB = await clienteBHttp.GetFromJsonAsync<List<SolicitudDto>>("/api/solicitudes/mias");
        Assert.NotNull(miasClienteB);
        Assert.DoesNotContain(miasClienteB, s => s.Id == solicitudA.Id || s.Id == solicitudVisitante.Id);

        var rechazoAjenoClienteAResp = await clienteBHttp.PatchAsync($"/api/cotizaciones/{cotizacionA.Id}/rechazar", null);
        Assert.Equal(HttpStatusCode.NotFound, rechazoAjenoClienteAResp.StatusCode);

        var rechazoAjenoVisitanteResp = await clienteBHttp.PatchAsync($"/api/cotizaciones/{cotizacionVisitante.Id}/rechazar", null);
        Assert.Equal(HttpStatusCode.NotFound, rechazoAjenoVisitanteResp.StatusCode);

        // Verificar que las cotizaciones siguen intactas en estado Enviada tras los intentos de Cliente B
        var solAVerificada = await clienteAHttp.GetFromJsonAsync<SolicitudDto>($"/api/solicitudes/{solicitudA.Id}");
        Assert.NotNull(solAVerificada);
        Assert.Equal(nameof(EstadoSolicitud.Cotizada), solAVerificada.Estado);
        Assert.Equal(nameof(EstadoCotizacion.Enviada), solAVerificada.Cotizaciones.Single().Estado);
    }

    [Fact]
    public async Task RechazoDeCotizacionesPorClienteDueno_CambiaCotizacionARechazada_BloqueaRechazoDuplicado_YDocumentaEstadoSolicitudAlRechazarUltima()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var adminHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var (_, clienteToken) = await RegistrarYAutenticarClienteAsync(
            anonClient,
            $"cliente.rechazo.{sufijo}@saborlatino.local",
            $"Cliente Rechazo {sufijo}");
        using var clienteHttp = CrearClienteAutenticado(clienteToken);

        // 1. Cliente dueño crea solicitud
        var crearSolResp = await clienteHttp.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Cliente Rechazo {sufijo}",
            CorreoContacto = $"cliente.rechazo.{sufijo}@saborlatino.local",
            TelefonoContacto = "88884400",
            TipoEvento = "Quince Años",
            FechaEvento = DateTime.Now.AddDays(35),
            Lugar = "Alajuela, Costa Rica",
            CantidadInvitados = 100
        });
        Assert.Equal(HttpStatusCode.Created, crearSolResp.StatusCode);
        var solicitud = (await crearSolResp.Content.ReadFromJsonAsync<SolicitudDto>())!;

        // 2. Admin envía dos cotizaciones
        var cot1 = await (await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 280000m,
                Descuento = 10000m,
                Detalle = "Primera propuesta",
                VigenteHasta = DateTime.Now.AddDays(10)
            })).Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cot1);

        var cot2 = await (await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 240000m,
                Descuento = 0m,
                Detalle = "Segunda propuesta",
                VigenteHasta = DateTime.Now.AddDays(10)
            })).Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cot2);

        // 3. El cliente dueño rechaza la primera cotización -> 200 OK y pasa a Rechazada
        var rechazarCot1Resp = await clienteHttp.PatchAsync($"/api/cotizaciones/{cot1.Id}/rechazar", null);
        Assert.Equal(HttpStatusCode.OK, rechazarCot1Resp.StatusCode);
        var cot1Rechazada = await rechazarCot1Resp.Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cot1Rechazada);
        Assert.Equal(nameof(EstadoCotizacion.Rechazada), cot1Rechazada.Estado);

        // Intento duplicado de rechazar la misma cotización devuelve 400 BadRequest
        var rechazarCot1DuplicadoResp = await clienteHttp.PatchAsync($"/api/cotizaciones/{cot1.Id}/rechazar", null);
        Assert.Equal(HttpStatusCode.BadRequest, rechazarCot1DuplicadoResp.StatusCode);

        // 4. El cliente dueño rechaza la segunda (y última) cotización enviada
        var rechazarCot2Resp = await clienteHttp.PatchAsync($"/api/cotizaciones/{cot2.Id}/rechazar", null);
        Assert.Equal(HttpStatusCode.OK, rechazarCot2Resp.StatusCode);
        var cot2Rechazada = await rechazarCot2Resp.Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cot2Rechazada);
        Assert.Equal(nameof(EstadoCotizacion.Rechazada), cot2Rechazada.Estado);

        // 5. Inspección del comportamiento actual: aunque todas las cotizaciones están Rechazadas,
        // CotizacionService.RechazarAsync mantiene Solicitud.Estado en "Cotizada" (no la regresa a "Pendiente" ni la pasa a "Rechazada")
        var solicitudTrasRechazarTodas = await clienteHttp.GetFromJsonAsync<SolicitudDto>($"/api/solicitudes/{solicitud.Id}");
        Assert.NotNull(solicitudTrasRechazarTodas);
        Assert.Equal(nameof(EstadoSolicitud.Cotizada), solicitudTrasRechazarTodas.Estado);
        Assert.All(solicitudTrasRechazarTodas.Cotizaciones, c => Assert.Equal(nameof(EstadoCotizacion.Rechazada), c.Estado));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var solEnBd = await db.Solicitudes
                .AsNoTracking()
                .Include(s => s.Cotizaciones)
                .SingleAsync(s => s.Id == solicitud.Id);

            Assert.Equal(EstadoSolicitud.Cotizada, solEnBd.Estado);
            Assert.All(solEnBd.Cotizaciones, c => Assert.Equal(EstadoCotizacion.Rechazada, c.Estado));
        }

        // En el filtro administrativo por estado, sigue figurando bajo ?estado=Cotizada y no bajo ?estado=Pendiente
        var cotizadasAdmin = await adminHttp.GetFromJsonAsync<List<SolicitudDto>>("/api/solicitudes?estado=Cotizada");
        Assert.NotNull(cotizadasAdmin);
        Assert.Contains(cotizadasAdmin, s => s.Id == solicitud.Id);

        var pendientesAdmin = await adminHttp.GetFromJsonAsync<List<SolicitudDto>>("/api/solicitudes?estado=Pendiente");
        Assert.NotNull(pendientesAdmin);
        Assert.DoesNotContain(pendientesAdmin, s => s.Id == solicitud.Id);

        // 6. El admin puede cerrar explícitamente la solicitud (PATCH /api/solicitudes/{id}/estado -> Rechazada),
        // tras lo cual ya no se permite emitir nuevas cotizaciones (400 BadRequest)
        var cerrarSolResp = await adminHttp.PatchAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/estado",
            new SolicitudCambiarEstadoDto { Estado = nameof(EstadoSolicitud.Rechazada) });
        Assert.Equal(HttpStatusCode.OK, cerrarSolResp.StatusCode);
        var solCerrada = await cerrarSolResp.Content.ReadFromJsonAsync<SolicitudDto>();
        Assert.NotNull(solCerrada);
        Assert.Equal(nameof(EstadoSolicitud.Rechazada), solCerrada.Estado);

        var cotizarSolRechazadaResp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 200000m,
                Descuento = 0m,
                Detalle = "Intento sobre solicitud ya rechazada",
                VigenteHasta = DateTime.Now.AddDays(5)
            });
        Assert.Equal(HttpStatusCode.BadRequest, cotizarSolRechazadaResp.StatusCode);
    }

    [Fact]
    public async Task VencimientoDeCotizacionEnBaseTemporal_AceptarDevuelve400PeroPersisteEstadoVencidaEnBd_YPermiteAceptarOtraCotizacionVigente()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var adminHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var crearSolResp = await anonClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Cliente Vencimiento {sufijo}",
            CorreoContacto = $"vencimiento.{sufijo}@saborlatino.local",
            TelefonoContacto = "88885500",
            TipoEvento = "Cena de Gala",
            FechaEvento = DateTime.Now.AddDays(50),
            Lugar = "San José, Costa Rica",
            CantidadInvitados = 150
        });
        Assert.Equal(HttpStatusCode.Created, crearSolResp.StatusCode);
        var solicitud = (await crearSolResp.Content.ReadFromJsonAsync<SolicitudDto>())!;

        // Crear dos cotizaciones inicialmente vigentes por HTTP
        var cotPorVencer = await (await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 310000m,
                Descuento = 10000m,
                Detalle = "Cotización que vencerá antes de ser aceptada",
                VigenteHasta = DateTime.Now.AddDays(5)
            })).Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cotPorVencer);

        var cotVigente = await (await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 295000m,
                Descuento = 5000m,
                Detalle = "Cotización alternativa vigente",
                VigenteHasta = DateTime.Now.AddDays(15)
            })).Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cotVigente);

        // Preparar vencimiento exclusivamente en la base temporal aislada
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var nombreBd = db.Database.GetDbConnection().Database;
            Assert.Equal(_factory.DatabaseName, nombreBd);
            Assert.StartsWith("SaborLatinoDb_Test_", nombreBd, StringComparison.Ordinal);
            Assert.NotEqual("SaborLatinoDb", nombreBd, StringComparer.OrdinalIgnoreCase);

            var entidadCot = await db.Cotizaciones.SingleAsync(c => c.Id == cotPorVencer.Id);
            entidadCot.VigenteHasta = DateTime.Now.AddDays(-1);
            await db.SaveChangesAsync();
        }

        // 1. Intentar aceptar la cotización vencida -> devuelve 400 BadRequest
        var aceptarVencidaResp = await adminHttp.PatchAsync($"/api/cotizaciones/{cotPorVencer.Id}/aceptar", null);
        var cuerpoVencida = await aceptarVencidaResp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, aceptarVencidaResp.StatusCode);
        Assert.Contains("La cotización está vencida y no se puede aceptar.", cuerpoVencida, StringComparison.Ordinal);

        // 2. Verificar que CotizacionService.AceptarAsync persistió EstadoCotizacion.Vencida en BD y mantuvo la solicitud en Cotizada
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cotEnBd = await db.Cotizaciones.AsNoTracking().SingleAsync(c => c.Id == cotPorVencer.Id);
            var solEnBd = await db.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitud.Id);

            Assert.Equal(EstadoCotizacion.Vencida, cotEnBd.Estado);
            Assert.Equal(EstadoSolicitud.Cotizada, solEnBd.Estado);
        }

        var solPorHttp = await adminHttp.GetFromJsonAsync<SolicitudDto>($"/api/solicitudes/{solicitud.Id}");
        Assert.NotNull(solPorHttp);
        Assert.Equal(nameof(EstadoSolicitud.Cotizada), solPorHttp.Estado);
        Assert.Equal(
            nameof(EstadoCotizacion.Vencida),
            solPorHttp.Cotizaciones.Single(c => c.Id == cotPorVencer.Id).Estado);
        Assert.Equal(
            nameof(EstadoCotizacion.Enviada),
            solPorHttp.Cotizaciones.Single(c => c.Id == cotVigente.Id).Estado);

        // 3. Un segundo intento sobre la misma cotización ya marcada Vencida falla por estado != Enviada
        var segundoIntentoVencidaResp = await adminHttp.PatchAsync($"/api/cotizaciones/{cotPorVencer.Id}/aceptar", null);
        var cuerpoSegundoIntento = await segundoIntentoVencidaResp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, segundoIntentoVencidaResp.StatusCode);
        Assert.Contains("Solo se puede aceptar una cotización Enviada (esta está Vencida).", cuerpoSegundoIntento, StringComparison.Ordinal);

        // 4. Aceptar la segunda cotización vigente pasa la solicitud a Aceptada y conserva la primera como Vencida
        var aceptarVigenteResp = await adminHttp.PatchAsync($"/api/cotizaciones/{cotVigente.Id}/aceptar", null);
        Assert.Equal(HttpStatusCode.OK, aceptarVigenteResp.StatusCode);

        var solFinal = await adminHttp.GetFromJsonAsync<SolicitudDto>($"/api/solicitudes/{solicitud.Id}");
        Assert.NotNull(solFinal);
        Assert.Equal(nameof(EstadoSolicitud.Aceptada), solFinal.Estado);
        Assert.Equal(
            nameof(EstadoCotizacion.Vencida),
            solFinal.Cotizaciones.Single(c => c.Id == cotPorVencer.Id).Estado);
        Assert.Equal(
            nameof(EstadoCotizacion.Aceptada),
            solFinal.Cotizaciones.Single(c => c.Id == cotVigente.Id).Estado);
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
        var token = loginDoc.RootElement.GetProperty("accessToken").GetString();
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
