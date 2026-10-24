using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Data;

// Crea el catálogo inicial, los 4 roles y el primer Superadministrador al iniciar la API. No duplica nada.
public static class DbSeeder
{
    public static async Task SembrarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 0. Catálogo inicial (Fase 1)
        await SembrarCatalogoAsync(db, logger);

        // 0b. Parámetros del sistema (Fase 4)
        await SembrarConfiguracionAsync(db);

        // 1. Roles
        foreach (var rol in Roles.Todos)
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        // 2. Superadministrador (los datos vienen de Secretos de usuario)
        var email = config["SuperAdmin:Email"];
        var password = config["SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Falta SuperAdmin:Email o SuperAdmin:Password. No se creó el Superadministrador.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var superAdmin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = "Superadministrador"
        };

        var resultado = await userManager.CreateAsync(superAdmin, password);
        if (!resultado.Succeeded)
        {
            logger.LogError("No se pudo crear el Superadministrador: {Errores}",
                string.Join("; ", resultado.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(superAdmin, Roles.Superadministrador);
        logger.LogInformation("Superadministrador creado: {Email}", email);
    }

    // Servicios, equipo y los 4 paquetes iniciales. Solo se cargan si no hay ningún servicio.
    // Los precios son de ejemplo; el admin los ajusta desde el API.
    // Agrega los parámetros que falten; nunca pisa un valor que el Superadministrador ya cambió
    private static async Task SembrarConfiguracionAsync(ApplicationDbContext db)
    {
        var parametros = new[]
        {
            new Configuracion
            {
                Clave = "Fidelidad.EventosMinimos",
                Valor = "3",
                Descripcion = "Eventos finalizados que necesita un cliente para recibir el descuento por fidelidad."
            },
            new Configuracion
            {
                Clave = "Fidelidad.PorcentajeDescuento",
                Valor = "10",
                Descripcion = "Porcentaje de descuento por fidelidad (de 0 a 100)."
            }
        };

        foreach (var p in parametros)
        {
            if (!await db.Configuraciones.AnyAsync(c => c.Clave == p.Clave))
            {
                db.Configuraciones.Add(p);
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SembrarCatalogoAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.Servicios.AnyAsync())
        {
            return;
        }

        var karaoke = new Servicio
        {
            Nombre = "Karaoke",
            Descripcion = "Karaoke con pantalla, micrófonos y música para bailar."
        };
        var musicaEnVivo = new Servicio
        {
            Nombre = "Música en vivo",
            Descripcion = "Grupos musicales para animar su evento."
        };
        var sonido = new Servicio
        {
            Nombre = "Sonido",
            Descripcion = "Equipo de sonido y técnico para grupos y eventos."
        };

        var parlante = new Equipo { Nombre = "Parlante activo", Tipo = "Audio", CantidadTotal = 6 };
        var subbajo = new Equipo { Nombre = "Subbajo", Tipo = "Audio", CantidadTotal = 4 };
        var microfono = new Equipo { Nombre = "Micrófono inalámbrico", Tipo = "Audio", CantidadTotal = 8 };
        var consola = new Equipo { Nombre = "Consola de sonido", Tipo = "Audio", CantidadTotal = 2 };
        var monitorInEar = new Equipo { Nombre = "Monitor in-ear inalámbrico", Tipo = "Audio", CantidadTotal = 6 };
        var pantalla = new Equipo { Nombre = "Pantalla para karaoke", Tipo = "Video", CantidadTotal = 2 };
        var luces = new Equipo { Nombre = "Luces de ambiente", Tipo = "Iluminación", CantidadTotal = 8 };
        var maquinaHumo = new Equipo { Nombre = "Máquina de humo", Tipo = "Iluminación", CantidadTotal = 2 };

        db.Paquetes.AddRange(
            new Paquete
            {
                Servicio = karaoke,
                Nombre = "DJ Open Format",
                Descripcion = "Precio base. Animación profesional con DJ Open Format, sonido estándar con dos subbajos y dos medios, dos micrófonos inalámbricos, una pantalla e iluminación completa de escenario. El servicio de karaoke se puede incluir como un extra.",
                PrecioBase = 80000m,
                Equipos =
                [
                    new PaqueteEquipo { Equipo = parlante, Cantidad = 2 },
                    new PaqueteEquipo { Equipo = subbajo, Cantidad = 2 },
                    new PaqueteEquipo { Equipo = microfono, Cantidad = 2 },
                    new PaqueteEquipo { Equipo = pantalla, Cantidad = 1 },
                    new PaqueteEquipo { Equipo = luces, Cantidad = 4 }
                ]
            },
            new Paquete
            {
                Servicio = sonido,
                Nombre = "Sonorización para grupos o DJs",
                Descripcion = "Precio base. Servicio profesional de sonorización para grupos musicales o DJs con cobertura hasta para 500 personas. Incluye consola de sonido, monitoreo in-ear inalámbrico, iluminación completa de escenario, máquina de humo y técnico especializado de audio e iluminación.",
                PrecioBase = 60000m,
                Equipos =
                [
                    new PaqueteEquipo { Equipo = parlante, Cantidad = 4 },
                    new PaqueteEquipo { Equipo = subbajo, Cantidad = 2 },
                    new PaqueteEquipo { Equipo = consola, Cantidad = 1 },
                    new PaqueteEquipo { Equipo = monitorInEar, Cantidad = 4 },
                    new PaqueteEquipo { Equipo = luces, Cantidad = 4 },
                    new PaqueteEquipo { Equipo = maquinaHumo, Cantidad = 1 }
                ]
            },
            new Paquete
            {
                Servicio = musicaEnVivo,
                Nombre = "Grupo secuenciado Villa Guanacaste",
                Descripcion = "Precio base. Presentación musical en vivo del grupo secuenciado Villa Guanacaste, con sistema de sonido adaptativo según el recinto e iluminación profesional completa de escenario.",
                PrecioBase = 170000m,
                Equipos =
                [
                    new PaqueteEquipo { Equipo = parlante, Cantidad = 2 },
                    new PaqueteEquipo { Equipo = subbajo, Cantidad = 2 },
                    new PaqueteEquipo { Equipo = microfono, Cantidad = 3 },
                    new PaqueteEquipo { Equipo = consola, Cantidad = 1 },
                    new PaqueteEquipo { Equipo = luces, Cantidad = 4 }
                ]
            },
            new Paquete
            {
                Servicio = musicaEnVivo,
                Nombre = "Marimba Orquesta Son del Gallo (en vivo)",
                Descripcion = "Precio base. Espectáculo musical en vivo con la Marimba Orquesta Son del Gallo (de 6 a 8 músicos en escena). Incluye todo el paquete integral de sonido, monitoreo in-ear inalámbrico, iluminación completa de escenario, máquina de humo, pantalla y soporte técnico completo.",
                PrecioBase = 300000m,
                Equipos =
                [
                    new PaqueteEquipo { Equipo = parlante, Cantidad = 4 },
                    new PaqueteEquipo { Equipo = subbajo, Cantidad = 2 },
                    new PaqueteEquipo { Equipo = microfono, Cantidad = 6 },
                    new PaqueteEquipo { Equipo = consola, Cantidad = 1 },
                    new PaqueteEquipo { Equipo = monitorInEar, Cantidad = 4 },
                    new PaqueteEquipo { Equipo = pantalla, Cantidad = 1 },
                    new PaqueteEquipo { Equipo = luces, Cantidad = 6 },
                    new PaqueteEquipo { Equipo = maquinaHumo, Cantidad = 1 }
                ]
            });

        await db.SaveChangesAsync();
        logger.LogInformation("Catálogo inicial creado: 3 servicios, 8 equipos y 4 paquetes.");
    }
}
