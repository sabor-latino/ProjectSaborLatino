using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.DTOs.Eventos;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Models;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class EventosDisponibilidadBitacoraTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public EventosDisponibilidadBitacoraTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CrearEventoDesdeCotizacionAceptada_CopiaEquiposDelPaquete_RechazaCotizacionNoAceptadaYCreacionDuplicada()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var adminHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];

        // 1. Crear un servicio, tres equipos (dos que permanecerán activos y uno que se desactivará antes de crear el evento) y un paquete
        var servicio = await CrearServicioAsync(adminHttp, $"Servicio Evento {sufijo}");
        var equipoActivo1 = await CrearEquipoAsync(adminHttp, $"Parlante Evento {sufijo}", "Audio", cantidadTotal: 6);
        var equipoActivo2 = await CrearEquipoAsync(adminHttp, $"Luces Evento {sufijo}", "Iluminación", cantidadTotal: 4);
        var equipoQueSeDesactiva = await CrearEquipoAsync(adminHttp, $"Micrófono Inactivo {sufijo}", "Audio", cantidadTotal: 3);

        var paqueteResp = await adminHttp.PostAsJsonAsync("/api/paquetes", new PaqueteGuardarDto
        {
            ServicioId = servicio.Id,
            Nombre = $"Paquete Evento {sufijo}",
            Descripcion = "Paquete con equipos para verificar copia automática de activos",
            PrecioBase = 320000m,
            Activo = true,
            Equipos =
            [
                new PaqueteEquipoGuardarDto { EquipoId = equipoActivo1.Id, Cantidad = 4 },
                new PaqueteEquipoGuardarDto { EquipoId = equipoActivo2.Id, Cantidad = 3 },
                new PaqueteEquipoGuardarDto { EquipoId = equipoQueSeDesactiva.Id, Cantidad = 2 }
            ]
        });
        Assert.Equal(HttpStatusCode.Created, paqueteResp.StatusCode);
        var paquete = (await paqueteResp.Content.ReadFromJsonAsync<PaqueteDto>())!;

        // Un paquete con equipo inactivo debe rechazarse completo al copiarse.
        var desactivarEquipoResp = await adminHttp.DeleteAsync($"/api/equipos/{equipoQueSeDesactiva.Id}");
        Assert.Equal(HttpStatusCode.NoContent, desactivarEquipoResp.StatusCode);

        // 2. Crear solicitud y cotización (aún en estado Enviada)
        var solResp = await anonClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Contacto Evento {sufijo}",
            CorreoContacto = $"evento.creacion.{sufijo}@saborlatino.local",
            TelefonoContacto = "88886001",
            PaqueteId = paquete.Id,
            TipoEvento = "Boda",
            FechaEvento = DateTime.Now.AddDays(40),
            Lugar = "Santa Ana, Costa Rica",
            CantidadInvitados = 110
        });
        Assert.Equal(HttpStatusCode.Created, solResp.StatusCode);
        var solicitud = (await solResp.Content.ReadFromJsonAsync<SolicitudDto>())!;

        var cotResp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 320000m,
                Descuento = 20000m,
                Detalle = "Cotización para prueba de creación de evento",
                VigenteHasta = DateTime.Now.AddDays(10)
            });
        Assert.Equal(HttpStatusCode.Created, cotResp.StatusCode);
        var cotizacion = (await cotResp.Content.ReadFromJsonAsync<CotizacionDto>())!;

        var fechaInicio = DateTime.Now.Date.AddDays(40).AddHours(14);
        var fechaFin = fechaInicio.AddHours(6);

        // 3. Intentar crear evento antes de aceptar la cotización -> 400 BadRequest
        var crearConCotEnviadaResp = await adminHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cotizacion.Id,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            CopiarEquipoDelPaquete = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, crearConCotEnviadaResp.StatusCode);
        Assert.Contains(
            "Solo se crea un evento desde una cotización Aceptada",
            await crearConCotEnviadaResp.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);

        // 4. Aceptar la cotización y validar que FechaFin <= FechaInicio se rechaza con 400 BadRequest
        var aceptarResp = await adminHttp.PatchAsync($"/api/cotizaciones/{cotizacion.Id}/aceptar", null);
        Assert.Equal(HttpStatusCode.OK, aceptarResp.StatusCode);

        var crearRangoInvalidoResp = await adminHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cotizacion.Id,
            FechaInicio = fechaFin,
            FechaFin = fechaInicio,
            CopiarEquipoDelPaquete = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, crearRangoInvalidoResp.StatusCode);

        var copiaIncompletaResp = await adminHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cotizacion.Id,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            CopiarEquipoDelPaquete = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, copiaIncompletaResp.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await db.Eventos.AnyAsync(e => e.CotizacionId == cotizacion.Id));
            Assert.False(await db.EventosHistorialEstados.AnyAsync(h => h.Evento!.CotizacionId == cotizacion.Id));
        }

        var reactivarResp = await adminHttp.PutAsJsonAsync($"/api/equipos/{equipoQueSeDesactiva.Id}", new EquipoGuardarDto
        {
            Nombre = equipoQueSeDesactiva.Nombre,
            Tipo = equipoQueSeDesactiva.Tipo,
            CantidadTotal = 3,
            Activo = true
        });
        Assert.Equal(HttpStatusCode.OK, reactivarResp.StatusCode);

        // 5. Crear el evento desde la cotización aceptada copiando el equipo del paquete.
        var crearEventoResp = await adminHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cotizacion.Id,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Lugar = null,
            Notas = "Montaje puntual 2 horas antes",
            CopiarEquipoDelPaquete = true
        });
        Assert.Equal(HttpStatusCode.Created, crearEventoResp.StatusCode);
        var evento = await crearEventoResp.Content.ReadFromJsonAsync<EventoDto>();
        Assert.NotNull(evento);
        Assert.Equal(cotizacion.Id, evento.CotizacionId);
        Assert.Equal(solicitud.Id, evento.SolicitudId);
        Assert.Equal(paquete.Id, evento.PaqueteId);
        Assert.Equal("Santa Ana, Costa Rica", evento.Lugar);
        Assert.Equal(nameof(EstadoEvento.Programado), evento.Estado);
        Assert.Equal(3, evento.Equipos.Count);
        Assert.Equal(4, evento.Equipos.Single(e => e.EquipoId == equipoActivo1.Id).Cantidad);
        Assert.Equal(3, evento.Equipos.Single(e => e.EquipoId == equipoActivo2.Id).Cantidad);
        Assert.Equal(2, evento.Equipos.Single(e => e.EquipoId == equipoQueSeDesactiva.Id).Cantidad);
        Assert.Single(evento.Historial);
        Assert.Null(evento.Historial[0].EstadoAnterior);
        Assert.Equal(nameof(EstadoEvento.Programado), evento.Historial[0].EstadoNuevo);

        // Verificar persistencia en base temporal
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var evEnBd = await db.Eventos
                .AsNoTracking()
                .Include(e => e.Equipos)
                .Include(e => e.Historial)
                .SingleAsync(e => e.Id == evento.Id);

            Assert.Equal(EstadoEvento.Programado, evEnBd.Estado);
            Assert.Equal(3, evEnBd.Equipos.Count);
            Assert.Single(evEnBd.Historial);
        }

        // 6. Intentar crear un segundo evento para la misma cotización -> 400 BadRequest ("Ya existe un evento para esa cotización.")
        var crearDuplicadoResp = await adminHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cotizacion.Id,
            FechaInicio = fechaInicio.AddDays(1),
            FechaFin = fechaFin.AddDays(1),
            CopiarEquipoDelPaquete = false
        });
        var cuerpoDuplicado = await crearDuplicadoResp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, crearDuplicadoResp.StatusCode);
        Assert.Contains("Ya existe un evento para esa cotización.", cuerpoDuplicado, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisponibilidadDeEquipos_CrucesHorariosDeEmpleados_YLiberacionDeRecursosAlCancelarEvento()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var adminHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];

        // 1. Preparar un equipo con stock controlado (CantidadTotal = 5) y un empleado activo
        var equipo = await CrearEquipoAsync(adminHttp, $"Consola Stock {sufijo}", "Audio", cantidadTotal: 5);
        var empleado = await CrearEmpleadoAsync(adminHttp, $"Técnico Horario {sufijo}", "Sonidista", usuarioId: null);

        var servicio = await CrearServicioAsync(adminHttp, $"Servicio Stock {sufijo}");
        var paqueteResp = await adminHttp.PostAsJsonAsync("/api/paquetes", new PaqueteGuardarDto
        {
            ServicioId = servicio.Id,
            Nombre = $"Paquete Stock {sufijo}",
            PrecioBase = 150000m,
            Activo = true,
            Equipos = [new PaqueteEquipoGuardarDto { EquipoId = equipo.Id, Cantidad = 3 }]
        });
        Assert.Equal(HttpStatusCode.Created, paqueteResp.StatusCode);
        var paquete = (await paqueteResp.Content.ReadFromJsonAsync<PaqueteDto>())!;

        // 2. Crear Evento 1 en el rango [Día +70 14:00, Día +70 20:00) que reserva 3 de las 5 unidades del equipo
        var baseDia = DateTime.SpecifyKind(DateTime.Now.Date.AddDays(70), DateTimeKind.Unspecified);
        var ev1Inicio = baseDia.AddHours(14);
        var ev1Fin = baseDia.AddHours(20);

        var cot1 = await CrearSolicitudYCotizacionAceptadaAsync(anonClient, adminHttp, sufijo, "ev1", paquete.Id);
        var crearEv1Resp = await adminHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cot1.Id,
            FechaInicio = ev1Inicio,
            FechaFin = ev1Fin,
            CopiarEquipoDelPaquete = true
        });
        Assert.Equal(HttpStatusCode.Created, crearEv1Resp.StatusCode);
        var evento1 = (await crearEv1Resp.Content.ReadFromJsonAsync<EventoDto>())!;
        Assert.Equal(3, evento1.Equipos.Single(e => e.EquipoId == equipo.Id).Cantidad);

        // Asignar el empleado al Evento 1 ([14:00, 20:00))
        var asigTrabEv1Resp = await adminHttp.PutAsJsonAsync(
            $"/api/eventos/{evento1.Id}/trabajadores",
            new List<EventoTrabajadorAsignarDto>
            {
                new() { EmpleadoId = empleado.Id, Funcion = "Operador principal" }
            });
        Assert.Equal(HttpStatusCode.OK, asigTrabEv1Resp.StatusCode);

        // 3. Comprobar GET /api/equipos/disponibilidad en rango solapado vs rangos NO solapados (incluido borde exacto 20:00)
        // Rango inválido (desde >= hasta) -> 400 BadRequest
        var dispInvalidaResp = await adminHttp.GetAsync(
            $"/api/equipos/disponibilidad?desde={Uri.EscapeDataString(ev1Fin.ToString("O"))}&hasta={Uri.EscapeDataString(ev1Inicio.ToString("O"))}");
        Assert.Equal(HttpStatusCode.BadRequest, dispInvalidaResp.StatusCode);

        // Rango solapado [16:00, 22:00) -> Reservada = 3, Disponible = 2
        var solapadoInicio = baseDia.AddHours(16);
        var solapadoFin = baseDia.AddHours(22);
        var dispSolapada = await ObtenerDisponibilidadEquipoAsync(adminHttp, equipo.Id, solapadoInicio, solapadoFin);
        Assert.Equal(5, dispSolapada.CantidadTotal);
        Assert.Equal(3, dispSolapada.Reservada);
        Assert.Equal(2, dispSolapada.Disponible);

        // Rango contiguo NO solapado [20:00, 23:00) (inicio == fin de Evento 1) -> Reservada = 0, Disponible = 5
        var noSolapadoInicio = baseDia.AddHours(20);
        var noSolapadoFin = baseDia.AddHours(23);
        var dispNoSolapada = await ObtenerDisponibilidadEquipoAsync(adminHttp, equipo.Id, noSolapadoInicio, noSolapadoFin);
        Assert.Equal(5, dispNoSolapada.CantidadTotal);
        Assert.Equal(0, dispNoSolapada.Reservada);
        Assert.Equal(5, dispNoSolapada.Disponible);

        // 4. Crear Evento 2 en el horario solapado [16:00, 22:00) (sin copiar equipo al inicio)
        var cot2 = await CrearSolicitudYCotizacionAceptadaAsync(anonClient, adminHttp, sufijo, "ev2", paqueteId: null);
        var crearEv2Resp = await adminHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cot2.Id,
            FechaInicio = solapadoInicio,
            FechaFin = solapadoFin,
            CopiarEquipoDelPaquete = false
        });
        Assert.Equal(HttpStatusCode.Created, crearEv2Resp.StatusCode);
        var evento2 = (await crearEv2Resp.Content.ReadFromJsonAsync<EventoDto>())!;

        // 5. Intentar asignar 3 unidades del equipo al Evento 2 cuando solo quedan 2 disponibles -> 400 BadRequest
        var asigExcesoStockResp = await adminHttp.PutAsJsonAsync(
            $"/api/eventos/{evento2.Id}/equipos",
            new List<EventoEquipoAsignarDto>
            {
                new() { EquipoId = equipo.Id, Cantidad = 3 }
            });
        var cuerpoExcesoStock = await asigExcesoStockResp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, asigExcesoStockResp.StatusCode);
        Assert.Contains("se piden 3 y quedan 2 de 5", cuerpoExcesoStock, StringComparison.Ordinal);

        // Asignar 2 unidades (lo disponible) sí pasa -> 200 OK, dejando Disponible = 0 en el cruce
        var asigStockExactoResp = await adminHttp.PutAsJsonAsync(
            $"/api/eventos/{evento2.Id}/equipos",
            new List<EventoEquipoAsignarDto>
            {
                new() { EquipoId = equipo.Id, Cantidad = 2 }
            });
        Assert.Equal(HttpStatusCode.OK, asigStockExactoResp.StatusCode);

        var dispAgotada = await ObtenerDisponibilidadEquipoAsync(adminHttp, equipo.Id, solapadoInicio, solapadoFin);
        Assert.Equal(5, dispAgotada.Reservada);
        Assert.Equal(0, dispAgotada.Disponible);

        // 6. Intentar asignar el mismo empleado al Evento 2 en horario solapado [16:00, 22:00) -> 400 BadRequest por cruce horario
        var asigTrabCruceResp = await adminHttp.PutAsJsonAsync(
            $"/api/eventos/{evento2.Id}/trabajadores",
            new List<EventoTrabajadorAsignarDto>
            {
                new() { EmpleadoId = empleado.Id, Funcion = "Intento con choque de horario" }
            });
        var cuerpoCruce = await asigTrabCruceResp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, asigTrabCruceResp.StatusCode);
        Assert.Contains("ya está asignado a otro evento en ese horario", cuerpoCruce, StringComparison.Ordinal);

        // 7. Cancelar el Evento 1 (PATCH /api/eventos/{id}/cancelar) -> libera tanto las 3 unidades de equipo como el horario del empleado
        var cancelarEv1Resp = await adminHttp.PatchAsJsonAsync(
            $"/api/eventos/{evento1.Id}/cancelar",
            new EventoCancelarDto { Motivo = "Cancelación por reprogramación del cliente" });
        Assert.Equal(HttpStatusCode.OK, cancelarEv1Resp.StatusCode);
        var ev1Cancelado = await cancelarEv1Resp.Content.ReadFromJsonAsync<EventoDto>();
        Assert.NotNull(ev1Cancelado);
        Assert.Equal(nameof(EstadoEvento.Cancelado), ev1Cancelado.Estado);
        Assert.Equal("Cancelación por reprogramación del cliente", ev1Cancelado.MotivoCancelacion);

        // Segundo intento de cancelar el mismo evento se rechaza con 400 BadRequest
        var cancelarDuplicadoResp = await adminHttp.PatchAsJsonAsync(
            $"/api/eventos/{evento1.Id}/cancelar",
            new EventoCancelarDto { Motivo = "Segundo intento" });
        Assert.Equal(HttpStatusCode.BadRequest, cancelarDuplicadoResp.StatusCode);

        // Al estar Cancelado el Evento 1, la disponibilidad en [16:00, 22:00) ya solo cuenta las 2 unidades de Evento 2 (Disponible = 3)
        var dispTrasCancelar = await ObtenerDisponibilidadEquipoAsync(adminHttp, equipo.Id, solapadoInicio, solapadoFin);
        Assert.Equal(2, dispTrasCancelar.Reservada);
        Assert.Equal(3, dispTrasCancelar.Disponible);

        // Y ahora el Evento 2 sí puede subir su reserva a 4 unidades (200 OK) y asignar al empleado que quedó libre (200 OK)
        var ampliarEquipoEv2Resp = await adminHttp.PutAsJsonAsync(
            $"/api/eventos/{evento2.Id}/equipos",
            new List<EventoEquipoAsignarDto>
            {
                new() { EquipoId = equipo.Id, Cantidad = 4 }
            });
        Assert.Equal(HttpStatusCode.OK, ampliarEquipoEv2Resp.StatusCode);

        var asigTrabLibreResp = await adminHttp.PutAsJsonAsync(
            $"/api/eventos/{evento2.Id}/trabajadores",
            new List<EventoTrabajadorAsignarDto>
            {
                new() { EmpleadoId = empleado.Id, Funcion = "Asignado tras liberarse Evento 1" }
            });
        Assert.Equal(HttpStatusCode.OK, asigTrabLibreResp.StatusCode);
        var ev2Actualizado = await asigTrabLibreResp.Content.ReadFromJsonAsync<EventoDto>();
        Assert.NotNull(ev2Actualizado);
        Assert.Single(ev2Actualizado.Trabajadores);
        Assert.Equal(empleado.Id, ev2Actualizado.Trabajadores[0].EmpleadoId);
    }

    [Fact]
    public async Task PermisosYAlcanceDeTrabajadorAsignadoFrenteANoAsignado_TransicionesDeEstado_YRegistrosEnBitacora()
    {
        using var anonClient = _factory.CreateClient();
        var superToken = await IniciarSesionSuperAdminAsync(anonClient);
        using var superHttp = CrearClienteAutenticado(superToken);

        var sufijo = Guid.NewGuid().ToString("N")[..8];

        // 1. Crear usuarios y actores: Administrador, Trabajador Asignado, Trabajador No Asignado y Cliente Dueño
        var (_, adminToken) = await CrearYAutenticarUsuarioAsync(
            superHttp,
            anonClient,
            $"admin.eventos.{sufijo}@saborlatino.local",
            $"Admin Eventos {sufijo}",
            Roles.Administrador);
        using var adminHttp = CrearClienteAutenticado(adminToken);

        var (trabAsignadoUserId, trabAsignadoToken) = await CrearYAutenticarUsuarioAsync(
            superHttp,
            anonClient,
            $"trab.asignado.{sufijo}@saborlatino.local",
            $"Trabajador Asignado {sufijo}",
            Roles.Trabajador);
        using var trabAsignadoHttp = CrearClienteAutenticado(trabAsignadoToken);

        var (trabNoAsignadoUserId, trabNoAsignadoToken) = await CrearYAutenticarUsuarioAsync(
            superHttp,
            anonClient,
            $"trab.noasignado.{sufijo}@saborlatino.local",
            $"Trabajador No Asignado {sufijo}",
            Roles.Trabajador);
        using var trabNoAsignadoHttp = CrearClienteAutenticado(trabNoAsignadoToken);

        var empleadoAsignado = await CrearEmpleadoAsync(
            superHttp,
            $"Empleado Asignado {sufijo}",
            "Animador",
            trabAsignadoUserId);
        var empleadoNoAsignado = await CrearEmpleadoAsync(
            superHttp,
            $"Empleado No Asignado {sufijo}",
            "DJ",
            trabNoAsignadoUserId);

        var (_, clienteDuenoToken) = await RegistrarYAutenticarClienteAsync(
            anonClient,
            $"cliente.evento.{sufijo}@saborlatino.local",
            $"Cliente Evento {sufijo}");
        using var clienteDuenoHttp = CrearClienteAutenticado(clienteDuenoToken);

        // 2. Cliente Dueño crea solicitud, Admin la cotiza y acepta, y Admin crea el Evento y asigna solo a empleadoAsignado
        var solResp = await clienteDuenoHttp.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Cliente Evento {sufijo}",
            CorreoContacto = $"cliente.evento.{sufijo}@saborlatino.local",
            TelefonoContacto = "88887007",
            TipoEvento = "Gala Anual",
            FechaEvento = DateTime.Now.AddDays(55),
            Lugar = "Belén, Heredia",
            CantidadInvitados = 200
        });
        Assert.Equal(HttpStatusCode.Created, solResp.StatusCode);
        var solicitud = (await solResp.Content.ReadFromJsonAsync<SolicitudDto>())!;

        var cot = await (await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 300000m,
                Descuento = 50000m,
                Detalle = "Cotización para flujo de estados y bitácora",
                VigenteHasta = DateTime.Now.AddDays(10)
            })).Content.ReadFromJsonAsync<CotizacionDto>();
        Assert.NotNull(cot);

        Assert.Equal(HttpStatusCode.OK, (await adminHttp.PatchAsync($"/api/cotizaciones/{cot.Id}/aceptar", null)).StatusCode);

        var inicio = DateTime.Now.Date.AddDays(55).AddHours(18);
        var fin = inicio.AddHours(5);
        var crearEvResp = await adminHttp.PostAsJsonAsync("/api/eventos", new EventoCrearDto
        {
            CotizacionId = cot.Id,
            FechaInicio = inicio,
            FechaFin = fin,
            CopiarEquipoDelPaquete = false
        });
        Assert.Equal(HttpStatusCode.Created, crearEvResp.StatusCode);
        var evento = (await crearEvResp.Content.ReadFromJsonAsync<EventoDto>())!;

        var asigTrabResp = await adminHttp.PutAsJsonAsync(
            $"/api/eventos/{evento.Id}/trabajadores",
            new List<EventoTrabajadorAsignarDto>
            {
                new() { EmpleadoId = empleadoAsignado.Id, Funcion = "Maestro de ceremonias" }
            });
        Assert.Equal(HttpStatusCode.OK, asigTrabResp.StatusCode);

        // 3. Verificar permisos 401/403 y alcance 404 entre TrabajadorNoAsignado, TrabajadorAsignado y ClienteDueño
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.GetAsync($"/api/eventos/{evento.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonClient.PatchAsJsonAsync(
                $"/api/eventos/{evento.Id}/estado",
                new EventoCambiarEstadoDto { Estado = nameof(EstadoEvento.EnMontaje) })).StatusCode);

        // Cliente dueño ve su evento (200 OK en /api/eventos/{id} y /api/mis-eventos), pero recibe 403 al intentar cambiar estado o cancelar
        Assert.Equal(HttpStatusCode.OK, (await clienteDuenoHttp.GetAsync($"/api/eventos/{evento.Id}")).StatusCode);
        var misEventosCliente = await clienteDuenoHttp.GetFromJsonAsync<List<EventoDto>>("/api/mis-eventos");
        Assert.NotNull(misEventosCliente);
        Assert.Contains(misEventosCliente, e => e.Id == evento.Id);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await clienteDuenoHttp.PatchAsJsonAsync(
                $"/api/eventos/{evento.Id}/estado",
                new EventoCambiarEstadoDto { Estado = nameof(EstadoEvento.EnMontaje) })).StatusCode);

        // Trabajador NO asignado recibe 404 en GET /api/eventos/{id}, no lo ve en /api/mis-eventos y recibe 404 en PATCH /api/eventos/{id}/estado
        Assert.Equal(HttpStatusCode.NotFound, (await trabNoAsignadoHttp.GetAsync($"/api/eventos/{evento.Id}")).StatusCode);
        var misEventosNoAsignado = await trabNoAsignadoHttp.GetFromJsonAsync<List<EventoDto>>("/api/mis-eventos");
        Assert.NotNull(misEventosNoAsignado);
        Assert.DoesNotContain(misEventosNoAsignado, e => e.Id == evento.Id);

        var cambioPorNoAsignadoResp = await trabNoAsignadoHttp.PatchAsJsonAsync(
            $"/api/eventos/{evento.Id}/estado",
            new EventoCambiarEstadoDto
            {
                Estado = nameof(EstadoEvento.EnMontaje),
                Comentario = "Intento de trabajador no asignado"
            });
        Assert.Equal(HttpStatusCode.NotFound, cambioPorNoAsignadoResp.StatusCode);
        _ = empleadoNoAsignado;

        // Trabajador asignado sí ve el evento en GET /api/eventos/{id} y en /api/mis-eventos, pero recibe 403 en rutas exclusivas de admin
        Assert.Equal(HttpStatusCode.OK, (await trabAsignadoHttp.GetAsync($"/api/eventos/{evento.Id}")).StatusCode);
        var misEventosAsignado = await trabAsignadoHttp.GetFromJsonAsync<List<EventoDto>>("/api/mis-eventos");
        Assert.NotNull(misEventosAsignado);
        Assert.Contains(misEventosAsignado, e => e.Id == evento.Id);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await trabAsignadoHttp.PatchAsJsonAsync(
                $"/api/eventos/{evento.Id}/cancelar",
                new EventoCancelarDto { Motivo = "No autorizado" })).StatusCode);

        // 4. Transiciones de estado por el Trabajador Asignado: rechaza saltos de estado y avanza Programado -> EnMontaje -> EnServicio -> Finalizado
        var saltoInvalidoResp = await trabAsignadoHttp.PatchAsJsonAsync(
            $"/api/eventos/{evento.Id}/estado",
            new EventoCambiarEstadoDto { Estado = nameof(EstadoEvento.EnServicio) });
        Assert.Equal(HttpStatusCode.BadRequest, saltoInvalidoResp.StatusCode);

        var cancelarPorEstadoResp = await trabAsignadoHttp.PatchAsJsonAsync(
            $"/api/eventos/{evento.Id}/estado",
            new EventoCambiarEstadoDto { Estado = nameof(EstadoEvento.Cancelado) });
        Assert.Equal(HttpStatusCode.BadRequest, cancelarPorEstadoResp.StatusCode);

        var aEnMontajeResp = await trabAsignadoHttp.PatchAsJsonAsync(
            $"/api/eventos/{evento.Id}/estado",
            new EventoCambiarEstadoDto
            {
                Estado = nameof(EstadoEvento.EnMontaje),
                Comentario = "Iniciando montaje en salón"
            });
        Assert.Equal(HttpStatusCode.OK, aEnMontajeResp.StatusCode);
        Assert.Equal(nameof(EstadoEvento.EnMontaje), (await aEnMontajeResp.Content.ReadFromJsonAsync<EventoDto>())?.Estado);

        var aEnServicioResp = await trabAsignadoHttp.PatchAsJsonAsync(
            $"/api/eventos/{evento.Id}/estado",
            new EventoCambiarEstadoDto
            {
                Estado = nameof(EstadoEvento.EnServicio),
                Comentario = "Evento en curso"
            });
        Assert.Equal(HttpStatusCode.OK, aEnServicioResp.StatusCode);
        Assert.Equal(nameof(EstadoEvento.EnServicio), (await aEnServicioResp.Content.ReadFromJsonAsync<EventoDto>())?.Estado);

        var aFinalizadoResp = await trabAsignadoHttp.PatchAsJsonAsync(
            $"/api/eventos/{evento.Id}/estado",
            new EventoCambiarEstadoDto
            {
                Estado = nameof(EstadoEvento.Finalizado),
                Comentario = "Desmontaje completado sin incidencias"
            });
        Assert.Equal(HttpStatusCode.OK, aFinalizadoResp.StatusCode);
        var eventoFinalizado = await aFinalizadoResp.Content.ReadFromJsonAsync<EventoDto>();
        Assert.NotNull(eventoFinalizado);
        Assert.Equal(nameof(EstadoEvento.Finalizado), eventoFinalizado.Estado);
        Assert.Equal(4, eventoFinalizado.Historial.Count);

        // Una vez Finalizado, no permite nuevos cambios de estado, cancelación ni edición de trabajadores/equipos (400 BadRequest)
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await adminHttp.PatchAsJsonAsync(
                $"/api/eventos/{evento.Id}/estado",
                new EventoCambiarEstadoDto { Estado = nameof(EstadoEvento.Finalizado) })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await adminHttp.PatchAsJsonAsync(
                $"/api/eventos/{evento.Id}/cancelar",
                new EventoCancelarDto { Motivo = "Ya terminó" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await adminHttp.PutAsJsonAsync(
                $"/api/eventos/{evento.Id}/trabajadores",
                new List<EventoTrabajadorAsignarDto>())).StatusCode);

        // 5. Verificar GET /api/bitacora: permisos (401 anónimo, 403 Administrador/Trabajador/Cliente) y registros auditados para Superadministrador
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonClient.GetAsync("/api/bitacora")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clienteDuenoHttp.GetAsync("/api/bitacora")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await trabAsignadoHttp.GetAsync("/api/bitacora")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await adminHttp.GetAsync("/api/bitacora")).StatusCode);

        var bitacoraEvento = await superHttp.GetFromJsonAsync<List<BitacoraDto>>("/api/bitacora?entidad=Evento");
        Assert.NotNull(bitacoraEvento);

        var idEventoStr = evento.Id.ToString();
        Assert.Contains(bitacoraEvento, b =>
            b.Entidad == nameof(Evento) &&
            b.EntidadId == idEventoStr &&
            b.Accion == "Crear");

        Assert.Contains(bitacoraEvento, b =>
            b.Entidad == nameof(Evento) &&
            b.EntidadId == idEventoStr &&
            b.Accion == "Modificar" &&
            b.UsuarioId == trabAsignadoUserId &&
            b.Detalle != null &&
            b.Detalle.Contains("Estado: Programado → EnMontaje", StringComparison.Ordinal));

        Assert.Contains(bitacoraEvento, b =>
            b.Entidad == nameof(Evento) &&
            b.EntidadId == idEventoStr &&
            b.Accion == "Modificar" &&
            b.UsuarioId == trabAsignadoUserId &&
            b.Detalle != null &&
            b.Detalle.Contains("Estado: EnServicio → Finalizado", StringComparison.Ordinal));
    }

    private static async Task<ServicioDto> CrearServicioAsync(HttpClient adminHttp, string nombre)
    {
        var resp = await adminHttp.PostAsJsonAsync("/api/servicios", new ServicioGuardarDto
        {
            Nombre = nombre,
            Descripcion = $"Descripción de {nombre}",
            Activo = true
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<ServicioDto>())!;
    }

    private static async Task<EquipoDto> CrearEquipoAsync(HttpClient adminHttp, string nombre, string tipo, int cantidadTotal)
    {
        var resp = await adminHttp.PostAsJsonAsync("/api/equipos", new EquipoGuardarDto
        {
            Nombre = nombre,
            Tipo = tipo,
            CantidadTotal = cantidadTotal,
            Activo = true
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<EquipoDto>())!;
    }

    private static async Task<EmpleadoDto> CrearEmpleadoAsync(HttpClient adminHttp, string nombre, string puesto, string? usuarioId)
    {
        var cedula = $"{RandomNumberGenerator.GetInt32(1, 9)}-0{RandomNumberGenerator.GetInt32(100, 999)}-0{RandomNumberGenerator.GetInt32(100, 999)}";
        var resp = await adminHttp.PostAsJsonAsync("/api/empleados", new EmpleadoGuardarDto
        {
            Cedula = cedula,
            NombreCompleto = nombre,
            Puesto = puesto,
            Telefono = "88887777",
            Activo = true,
            UsuarioId = usuarioId
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<EmpleadoDto>())!;
    }

    private static async Task<CotizacionDto> CrearSolicitudYCotizacionAceptadaAsync(
        HttpClient anonClient,
        HttpClient adminHttp,
        string sufijo,
        string etiqueta,
        int? paqueteId)
    {
        var solResp = await anonClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = $"Contacto {etiqueta} {sufijo}",
            CorreoContacto = $"{etiqueta}.{sufijo}@saborlatino.local",
            TelefonoContacto = "88886666",
            PaqueteId = paqueteId,
            TipoEvento = "Corporativo",
            FechaEvento = DateTime.Now.AddDays(60),
            Lugar = "San José, Costa Rica",
            CantidadInvitados = 90
        });
        Assert.Equal(HttpStatusCode.Created, solResp.StatusCode);
        var solicitud = (await solResp.Content.ReadFromJsonAsync<SolicitudDto>())!;

        var cotResp = await adminHttp.PostAsJsonAsync(
            $"/api/solicitudes/{solicitud.Id}/cotizaciones",
            new CotizacionCrearDto
            {
                Monto = 200000m,
                Descuento = 0m,
                Detalle = $"Cotización {etiqueta}",
                VigenteHasta = DateTime.Now.AddDays(15)
            });
        Assert.Equal(HttpStatusCode.Created, cotResp.StatusCode);
        var cot = (await cotResp.Content.ReadFromJsonAsync<CotizacionDto>())!;

        var aceptarResp = await adminHttp.PatchAsync($"/api/cotizaciones/{cot.Id}/aceptar", null);
        Assert.Equal(HttpStatusCode.OK, aceptarResp.StatusCode);
        return (await aceptarResp.Content.ReadFromJsonAsync<CotizacionDto>())!;
    }

    private static async Task<DisponibilidadEquipoDto> ObtenerDisponibilidadEquipoAsync(
        HttpClient adminHttp,
        int equipoId,
        DateTime desde,
        DateTime hasta)
    {
        var url = $"/api/equipos/disponibilidad?desde={Uri.EscapeDataString(desde.ToString("s"))}&hasta={Uri.EscapeDataString(hasta.ToString("s"))}";
        var lista = await adminHttp.GetFromJsonAsync<List<DisponibilidadEquipoDto>>(url);
        Assert.NotNull(lista);
        return Assert.Single(lista, x => x.EquipoId == equipoId);
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
