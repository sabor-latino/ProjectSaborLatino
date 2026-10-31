using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Catalogo;
using ProjectSaborLatino.DTOs.Solicitudes;
using ProjectSaborLatino.Tests.Infrastructure;
using Xunit;

namespace ProjectSaborLatino.Tests;

public class SolicitudPaqueteTests : IClassFixture<BackendTestFactory>
{
    private readonly BackendTestFactory _factory;

    public SolicitudPaqueteTests(BackendTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CrearSolicitud_RechazaPaqueteCuyoServicioPadreEstaInactivoYPermitePaqueteConServicioActivo()
    {
        using var publicClient = _factory.CreateClient();
        using var adminClient = await CrearClienteSuperAdminAsync();

        // 1. Crear servicio y paquete de prueba activos por HTTP (sin alterar los datos sembrados)
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var crearServicioResponse = await adminClient.PostAsJsonAsync("/api/servicios", new ServicioGuardarDto
        {
            Nombre = $"Servicio Prueba {sufijo}",
            Descripcion = "Servicio temporal para validar regla de paquete con servicio padre inactivo",
            Activo = true
        });
        Assert.Equal(HttpStatusCode.Created, crearServicioResponse.StatusCode);
        var servicioPrueba = await crearServicioResponse.Content.ReadFromJsonAsync<ServicioDto>();
        Assert.NotNull(servicioPrueba);
        Assert.True(servicioPrueba.Activo);

        var crearPaqueteResponse = await adminClient.PostAsJsonAsync("/api/paquetes", new PaqueteGuardarDto
        {
            ServicioId = servicioPrueba.Id,
            Nombre = $"Paquete Prueba {sufijo}",
            Descripcion = "Paquete activo cuyo servicio padre será desactivado",
            PrecioBase = 175000m,
            Activo = true,
            Equipos = []
        });
        Assert.Equal(HttpStatusCode.Created, crearPaqueteResponse.StatusCode);
        var paquetePrueba = await crearPaqueteResponse.Content.ReadFromJsonAsync<PaqueteDto>();
        Assert.NotNull(paquetePrueba);
        Assert.True(paquetePrueba.Activo);
        Assert.Equal(servicioPrueba.Id, paquetePrueba.ServicioId);

        // 2. Mientras el servicio padre está activo, el paquete aparece en el catálogo público y POST /api/solicitudes lo permite
        var paquetePublicoActivoResponse = await publicClient.GetAsync($"/api/paquetes/{paquetePrueba.Id}");
        Assert.Equal(HttpStatusCode.OK, paquetePublicoActivoResponse.StatusCode);

        var correoActivo = $"solicitud.activa.{sufijo}@saborlatino.local";
        var solicitudConServicioActivoResponse = await publicClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = "Cliente Servicio Activo",
            CorreoContacto = correoActivo,
            TelefonoContacto = "88881111",
            PaqueteId = paquetePrueba.Id,
            TipoEvento = "Evento Corporativo",
            FechaEvento = DateTime.Now.AddDays(20),
            Lugar = "San José, Costa Rica",
            CantidadInvitados = 60,
            Comentarios = "Solicitud con servicio y paquete activos"
        });
        Assert.Equal(HttpStatusCode.Created, solicitudConServicioActivoResponse.StatusCode);
        var solicitudActivaCreada = await solicitudConServicioActivoResponse.Content.ReadFromJsonAsync<SolicitudDto>();
        Assert.NotNull(solicitudActivaCreada);
        Assert.Equal(paquetePrueba.Id, solicitudActivaCreada.PaqueteId);

        // 3. Desactivar el servicio padre mediante DELETE /api/servicios/{id} (el paquete conserva Activo = true en BD)
        var desactivarServicioResponse = await adminClient.DeleteAsync($"/api/servicios/{servicioPrueba.Id}");
        Assert.Equal(HttpStatusCode.NoContent, desactivarServicioResponse.StatusCode);

        // El catálogo público oculta tanto el servicio como su paquete
        var servicioPublicoInactivoResponse = await publicClient.GetAsync($"/api/servicios/{servicioPrueba.Id}");
        Assert.Equal(HttpStatusCode.NotFound, servicioPublicoInactivoResponse.StatusCode);

        var paquetePublicoOcultoResponse = await publicClient.GetAsync($"/api/paquetes/{paquetePrueba.Id}");
        Assert.Equal(HttpStatusCode.NotFound, paquetePublicoOcultoResponse.StatusCode);

        var listaPaquetesPublicos = await publicClient.GetFromJsonAsync<List<PaqueteDto>>("/api/paquetes");
        Assert.NotNull(listaPaquetesPublicos);
        Assert.DoesNotContain(listaPaquetesPublicos, p => p.Id == paquetePrueba.Id);

        // Para admin con ?incluirInactivos=true el paquete sigue teniendo Activo = true en su fila
        var paqueteAdminResponse = await adminClient.GetFromJsonAsync<PaqueteDto>(
            $"/api/paquetes/{paquetePrueba.Id}?incluirInactivos=true");
        Assert.NotNull(paqueteAdminResponse);
        Assert.True(paqueteAdminResponse.Activo);

        // 4. Intentar crear una solicitud referenciando el paquete cuyo servicio padre está inactivo
        var correoRechazado = $"solicitud.inactiva.{sufijo}@saborlatino.local";
        var solicitudServicioInactivoResponse = await publicClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = "Visitante Paquete Oculto",
            CorreoContacto = correoRechazado,
            TelefonoContacto = "88882222",
            PaqueteId = paquetePrueba.Id,
            TipoEvento = "Fiesta Privada",
            FechaEvento = DateTime.Now.AddDays(25),
            Lugar = "Cartago, Costa Rica",
            CantidadInvitados = 40,
            Comentarios = "No debe aceptarse porque el servicio padre está inactivo"
        });

        var cuerpoRespuesta = await solicitudServicioInactivoResponse.Content.ReadAsStringAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var solicitudIndebidaEnDb = await db.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CorreoContacto == correoRechazado);

        Assert.True(
            solicitudServicioInactivoResponse.StatusCode == HttpStatusCode.BadRequest && solicitudIndebidaEnDb is null,
            $"POST /api/solicitudes con PaqueteId={paquetePrueba.Id} (Paquete.Activo=true, Servicio.Activo=false) " +
            $"devolvió {(int)solicitudServicioInactivoResponse.StatusCode} ({solicitudServicioInactivoResponse.StatusCode}). " +
            $"SolicitudPersistidaEnDb={solicitudIndebidaEnDb is not null} (SolicitudId={solicitudIndebidaEnDb?.Id.ToString() ?? "null"}). " +
            $"Cuerpo={cuerpoRespuesta}");

        Assert.Equal(HttpStatusCode.BadRequest, solicitudServicioInactivoResponse.StatusCode);
        Assert.Contains($"No existe un paquete activo con Id {paquetePrueba.Id}.", cuerpoRespuesta, StringComparison.Ordinal);
        Assert.Null(solicitudIndebidaEnDb);

        // 5. Otro paquete del catálogo público con servicio activo sigue siendo aceptado normalmente
        Assert.NotEmpty(listaPaquetesPublicos);
        var paqueteCatalogoActivo = listaPaquetesPublicos[0];
        var correoCatalogoActivo = $"solicitud.catalogo.{sufijo}@saborlatino.local";
        var solicitudCatalogoActivoResponse = await publicClient.PostAsJsonAsync("/api/solicitudes", new SolicitudCrearDto
        {
            NombreContacto = "Cliente Paquete Catálogo Activo",
            CorreoContacto = correoCatalogoActivo,
            TelefonoContacto = "88883333",
            PaqueteId = paqueteCatalogoActivo.Id,
            TipoEvento = "Cumpleaños",
            FechaEvento = DateTime.Now.AddDays(35),
            Lugar = "Alajuela, Costa Rica",
            CantidadInvitados = 50
        });
        Assert.Equal(HttpStatusCode.Created, solicitudCatalogoActivoResponse.StatusCode);
    }

    private async Task<HttpClient> CrearClienteSuperAdminAsync()
    {
        using var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/identity/login", new
        {
            email = BackendTestFactory.TestSuperAdminEmail,
            password = _factory.TestSuperAdminPassword
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var doc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return adminClient;
    }
}
