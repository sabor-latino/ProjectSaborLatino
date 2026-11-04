using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
        // Azure SQL puede devolver 40613 al reactivarse. Reintentar solo la apertura
        // inicial evita cambiar la estrategia de las transacciones del negocio.
        var apertura = new SqlServerRetryingExecutionStrategy(
            db, maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(15), errorNumbersToAdd: null);
        await apertura.ExecuteAsync(() => db.GetService<IRelationalConnection>().OpenAsync(CancellationToken.None));

        // 0. Catálogo inicial, preguntas frecuentes y promociones de temporada (Fase 1 y 2)
        await SembrarCatalogoAsync(db, logger);
        await SembrarPreguntasFrecuentesAsync(db);
        await SembrarPromocionesTemporadaAsync(db);

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
            },
            new Configuracion
            {
                Clave = "Finanzas.AnticipoPorcentaje",
                Valor = "50",
                Descripcion = "Porcentaje de anticipo estándar requerido para reservar la fecha del evento (0 a 100)."
            },
            new Configuracion
            {
                Clave = "Finanzas.VigenciaDiasEstandar",
                Valor = "15",
                Descripcion = "Días naturales de vigencia estándar predeterminados para nuevas cotizaciones."
            },
            new Configuracion
            {
                Clave = "Legal.PoliticaCancelacion",
                Valor = "Cancelaciones con más de 15 días de anticipación tienen derecho a reintegro del 70% del anticipo. Cancelaciones con menos de 15 días no son reembolsables debido al bloqueo de fecha y equipos.",
                Descripcion = "Política comercial de cancelaciones y reembolsos."
            },
            new Configuracion
            {
                Clave = "Legal.RequisitoToldo",
                Valor = "Para eventos al aire libre o jardín, se requiere de forma obligatoria tarima techada e impermeable para salvaguardar los equipos electrónicos de sonido e iluminación.",
                Descripcion = "Requerimiento de protección climática para eventos exteriores."
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
            NombreEn = "Karaoke & DJ",
            Descripcion = "Karaoke con pantalla, micrófonos y música para bailar.",
            DescripcionEn = "Interactive karaoke with high-definition screen, wireless microphones, and Open Format dance music."
        };
        var musicaEnVivo = new Servicio
        {
            Nombre = "Música en vivo",
            NombreEn = "Live Music",
            Descripcion = "Grupos musicales para animar su evento.",
            DescripcionEn = "Professional live musical ensembles and orchestras to energize your celebration."
        };
        var sonido = new Servicio
        {
            Nombre = "Sonido",
            NombreEn = "Sound & Production",
            Descripcion = "Equipo de sonido y técnico para grupos y eventos.",
            DescripcionEn = "Pro-audio reinforcement, stage lighting, and dedicated sound engineering for bands and events."
        };

        var parlante = new Equipo
        {
            Nombre = "Parlante activo",
            NombreEn = "Active Full-Range Speaker",
            Tipo = "Audio",
            EspecificacionTecnica = "15\" dos vías bi-amplificado, 1200 W RMS, DSP integrado, 134 dB SPL máx.",
            EspecificacionTecnicaEn = "15\" 2-way bi-amped full-range cabinet, 1200 W RMS, built-in DSP, 134 dB max SPL.",
            CantidadTotal = 6
        };
        var subbajo = new Equipo
        {
            Nombre = "Subbajo",
            NombreEn = "Active Subwoofer",
            Tipo = "Audio",
            EspecificacionTecnica = "18\" activo de excursión larga, 1600 W RMS, respuesta 35 Hz – 120 Hz, 136 dB SPL.",
            EspecificacionTecnicaEn = "18\" long-excursion active subwoofer, 1600 W RMS, 35 Hz – 120 Hz frequency response, 136 dB SPL.",
            CantidadTotal = 4
        };
        var microfono = new Equipo
        {
            Nombre = "Micrófono inalámbrico",
            NombreEn = "Wireless Vocal Microphone",
            Tipo = "Audio",
            EspecificacionTecnica = "Cápsula cardioide dinámica UHF True Diversity, rango operativo de 90 m.",
            EspecificacionTecnicaEn = "UHF True Diversity dynamic cardioid capsule, 90 m operating range.",
            CantidadTotal = 8
        };
        var consola = new Equipo
        {
            Nombre = "Consola de sonido",
            NombreEn = "Digital Mixing Console",
            Tipo = "Audio",
            EspecificacionTecnica = "Mezcladora digital de 32 canales con faders motorizados, preamplificadores clase A y procesamiento DSP.",
            EspecificacionTecnicaEn = "32-channel digital mixer with motorized faders, Class-A preamps, and onboard DSP processing.",
            CantidadTotal = 2
        };
        var monitorInEar = new Equipo
        {
            Nombre = "Monitor in-ear inalámbrico",
            NombreEn = "Wireless In-Ear Monitor",
            Tipo = "Audio",
            EspecificacionTecnica = "Sistema UHF estéreo de monitoreo personal con receptor bodypack metálico y auriculares de aislamiento.",
            EspecificacionTecnicaEn = "Stereo UHF personal monitoring system with metal bodypack receiver and isolation earphones.",
            CantidadTotal = 6
        };
        var pantalla = new Equipo
        {
            Nombre = "Pantalla para karaoke",
            NombreEn = "HD Video Display Screen",
            Tipo = "Video",
            EspecificacionTecnica = "Pantalla LED Full HD de 55\" en pedestal ajustable con entrada HDMI de baja latencia.",
            EspecificacionTecnicaEn = "55\" Full HD LED display on heavy-duty adjustable stand with low-latency HDMI feed.",
            CantidadTotal = 2
        };
        var luces = new Equipo
        {
            Nombre = "Luces de ambiente",
            NombreEn = "Stage & Ambient LED Lighting",
            Tipo = "Iluminación",
            EspecificacionTecnica = "Par LED RGBWA+UV de 18x18 W y cabezas móviles Beam/Wash controladas vía protocolo DMX512.",
            EspecificacionTecnicaEn = "18x18 W RGBWA+UV LED pars and Beam/Wash moving heads controlled via DMX512 protocol.",
            CantidadTotal = 8
        };
        var maquinaHumo = new Equipo
        {
            Nombre = "Máquina de humo",
            NombreEn = "Stage Fog & Haze Machine",
            Tipo = "Iluminación",
            EspecificacionTecnica = "Generador atmosférico de 1500 W con fluido base agua hipoalergénico y control inalámbrico/DMX.",
            EspecificacionTecnicaEn = "1500 W atmospheric fogger using water-based hypoallergenic fluid with wireless/DMX control.",
            CantidadTotal = 2
        };

        db.Paquetes.AddRange(
            new Paquete
            {
                Servicio = karaoke,
                Nombre = "DJ Open Format",
                NombreEn = "Open Format DJ & Party System",
                Descripcion = "Precio base. Animación profesional con DJ Open Format, sonido estándar con dos subbajos y dos medios, dos micrófonos inalámbricos, una pantalla e iluminación completa de escenario. El servicio de karaoke se puede incluir como un extra.",
                DescripcionEn = "Base price. Professional Open Format DJ entertainment, balanced PA system with two subwoofers and two full-range tops, two wireless microphones, one HD screen, and complete stage lighting. Karaoke service can be added as an extra.",
                PrecioBase = 80000m,
                PotenciaSonido = "5600 W RMS (sistema 2.2 activo)",
                CoberturaAforo = "Hasta 180 personas en salón o terraza",
                DuracionMontaje = "60 minutos antes del inicio",
                RequerimientosElectricos = "1 circuito dedicado de 110V / 20A con polo a tierra a menos de 15 m",
                EspacioMinimo = "3 m de frente x 2 m de fondo",
                FichaTecnica = "Configuración 2.2 con dos parlantes activos de 15\" (1200 W RMS c/u) y dos subbajos de 18\" (1600 W RMS c/u), controlador DJ profesional, dos micrófonos UHF para animación o brindis, pantalla LED de 55\" y puente de 4 luminarias DMX.",
                FichaTecnicaEn = "2.2 active PA configuration with two 15\" tops (1200 W RMS each) and two 18\" subwoofers (1600 W RMS each), pro DJ controller, dual UHF wireless mics for hosting/toasts, 55\" LED screen, and 4-fixture DMX lighting truss.",
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
                NombreEn = "Live Band & DJ Sound Reinforcement",
                Descripcion = "Precio base. Servicio profesional de sonorización para grupos musicales o DJs con cobertura hasta para 500 personas. Incluye consola de sonido, monitoreo in-ear inalámbrico, iluminación completa de escenario, máquina de humo y técnico especializado de audio e iluminación.",
                DescripcionEn = "Base price. Professional sound reinforcement service for live bands or guest DJs covering up to 500 guests. Includes digital mixing console, wireless in-ear monitoring, full stage lighting, atmospheric fog machine, and dedicated audio/lighting engineer.",
                PrecioBase = 60000m,
                PotenciaSonido = "8000 W RMS (4 medios + 2 subbajos)",
                CoberturaAforo = "Hasta 500 personas en recinto cerrado o abierto",
                DuracionMontaje = "90 a 120 minutos (incluye prueba de sonido)",
                RequerimientosElectricos = "2 circuitos independientes de 110V / 20A (audio e iluminación separados)",
                EspacioMinimo = "4 m de frente x 3 m de fondo",
                FichaTecnica = "PA principal de cuatro cabinas activas de 15\" y dos subbajos de 18\", consola digital de 32 canales, cuatro sistemas de monitoreo in-ear inalámbrico UHF para músicos, cuatro barras/cabezas LED DMX, cámara de humo de 1500 W e ingeniero FOH/monitores durante todo el evento.",
                FichaTecnicaEn = "Main PA with four 15\" active cabinets and two 18\" subwoofers, 32-channel digital console, four UHF wireless in-ear monitor mixes for performers, four DMX LED fixtures, 1500 W fog machine, and FOH/monitor engineer throughout the event.",
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
                NombreEn = "Villa Guanacaste Live Sequenced Ensemble",
                Descripcion = "Precio base. Presentación musical en vivo del grupo secuenciado Villa Guanacaste, con sistema de sonido adaptativo según el recinto e iluminación profesional completa de escenario.",
                DescripcionEn = "Base price. Live musical performance by the Villa Guanacaste sequenced ensemble, complete with venue-adaptive sound system and full professional stage lighting.",
                PrecioBase = 170000m,
                PotenciaSonido = "5600 W RMS con mezcla digital en vivo",
                CoberturaAforo = "Hasta 250 personas",
                DuracionMontaje = "90 minutos antes del evento",
                RequerimientosElectricos = "2 tomas de 110V / 20A estabilizadas con conexión a tierra",
                EspacioMinimo = "4 m de frente x 3 m de fondo para músicos y PA",
                FichaTecnica = "Repertorio tropical, bailable y costarricense en vivo con secuencias multipista de alta definición, consola digital de 32 canales, tres micrófonos inalámbricos UHF, dos medios activos de 15\", dos subbajos de 18\" y cuatro luminarias DMX.",
                FichaTecnicaEn = "Live tropical, dance, and Costa Rican repertoire backed by high-definition multitrack sequences, 32-channel digital mixer, three UHF wireless microphones, two 15\" active tops, two 18\" subwoofers, and four DMX stage lights.",
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
                NombreEn = "Son del Gallo Live Marimba Orchestra",
                Descripcion = "Precio base. Espectáculo musical en vivo con la Marimba Orquesta Son del Gallo (de 6 a 8 músicos en escena). Incluye todo el paquete integral de sonido, monitoreo in-ear inalámbrico, iluminación completa de escenario, máquina de humo, pantalla y soporte técnico completo.",
                DescripcionEn = "Base price. Full live concert show featuring the Son del Gallo Marimba Orchestra (6 to 8 musicians on stage). Includes complete sound reinforcement, wireless in-ear monitoring, full stage lighting, fog machine, video screen, and full technical crew.",
                PrecioBase = 300000m,
                PotenciaSonido = "8000 W RMS de alto desempeño para orquesta",
                CoberturaAforo = "Hasta 600 personas en salón, jardín o toldo",
                DuracionMontaje = "120 a 150 minutos (montaje orquestal y line check)",
                RequerimientosElectricos = "2 circuitos dedicados de 110V / 20A o acometida 220V bifásica con tierra física",
                EspacioMinimo = "6 m de frente x 4 m de fondo (tarima o piso firme techado)",
                FichaTecnica = "Producción integral para 6 a 8 músicos en escena con marimba de concierto, percusión latina, vientos y voces. Incluye cuatro parlantes activos de 15\", dos subbajos de 18\", consola digital de 32 canales, seis micrófonos inalámbricos, cuatro sistemas in-ear, pantalla Full HD de 55\", seis luminarias DMX y máquina de humo.",
                FichaTecnicaEn = "Full production for 6 to 8 stage musicians featuring concert marimba, Latin percussion, brass, and vocals. Includes four 15\" active speakers, two 18\" subwoofers, 32-channel digital console, six wireless microphones, four in-ear monitor systems, 55\" Full HD screen, six DMX lights, and fog machine.",
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

    private static async Task SembrarPreguntasFrecuentesAsync(ApplicationDbContext db)
    {
        if (await db.PreguntasFrecuentes.AnyAsync())
        {
            return;
        }

        db.PreguntasFrecuentes.AddRange(
            new PreguntaFrecuente
            {
                Orden = 1,
                Pregunta = "¿Qué requerimientos eléctricos y de espacio necesitan en el lugar del evento?",
                PreguntaEn = "What electrical and space requirements do you need at the event venue?",
                Respuesta = "Para formatos DJ y grupos medianos requerimos al menos un circuito estable de 110V / 20A con conexión a tierra a menos de 15 metros del escenario y un área mínima de 3x2 m. Para la Marimba Orquesta o sonorizaciones grandes recomendamos dos circuitos independientes de 110V / 20A y un espacio techado de 6x4 m.",
                RespuestaEn = "For DJ setups and medium ensembles we require at least one grounded 110V / 20A circuit within 15 meters of the stage and a 3x2 m area. For the Marimba Orchestra or large productions we recommend two independent 110V / 20A circuits and a covered 6x4 m stage area.",
                Activo = true
            },
            new PreguntaFrecuente
            {
                Orden = 2,
                Pregunta = "¿Con cuánta anticipación llega el equipo técnico para el montaje y prueba de sonido?",
                PreguntaEn = "How far in advance does the technical crew arrive for setup and soundcheck?",
                Respuesta = "Nuestro personal técnico ingresa entre 60 y 150 minutos antes de la hora de inicio según la complejidad del paquete contratado, garantizando que el montaje y la prueba de sonido concluyan antes de la llegada de sus invitados.",
                RespuestaEn = "Our technical crew arrives between 60 and 150 minutes prior to start time depending on package complexity, ensuring setup and soundcheck are completed before your guests arrive.",
                Activo = true
            },
            new PreguntaFrecuente
            {
                Orden = 3,
                Pregunta = "¿Los paquetes incluyen transporte, montaje y operador de audio?",
                PreguntaEn = "Do packages include transportation, setup, and an audio engineer?",
                Respuesta = "Sí. Todos nuestros paquetes incluyen el montaje, desmontaje y operación técnica durante el evento. El costo base cubre el Gran Área Metropolitana (GAM); para eventos fuera del GAM se calcula el rubro logístico en la cotización formal.",
                RespuestaEn = "Yes. Every package includes setup, teardown, and live technical operation during your event. Base pricing covers the Greater Metropolitan Area (GAM); for venues outside the GAM, travel logistics are itemized in your formal quote.",
                Activo = true
            },
            new PreguntaFrecuente
            {
                Orden = 4,
                Pregunta = "¿Cómo funcionan las promociones de temporada y el descuento de cliente frecuente?",
                PreguntaEn = "How do seasonal promotions and the frequent client loyalty discount work?",
                Respuesta = "Durante el año activamos campañas de temporada con descuentos porcentuales directos sobre paquetes seleccionados. Además, los clientes registrados que acumulan el mínimo de eventos finalizados reciben automáticamente nuestra tarifa preferencial de fidelidad al cotizar.",
                RespuestaEn = "Throughout the year we run seasonal campaigns with direct percentage discounts on selected packages. In addition, registered clients who reach the minimum number of completed events automatically qualify for our loyalty discount when requesting a quote.",
                Activo = true
            },
            new PreguntaFrecuente
            {
                Orden = 5,
                Pregunta = "¿Se puede personalizar el repertorio musical o agregar karaoke a cualquier paquete?",
                PreguntaEn = "Can we customize the musical repertoire or add karaoke to any package?",
                Respuesta = "Por supuesto. Coordinamos previamente los géneros musicales, canciones especiales para protocolo o baile principal y podemos integrar el sistema de karaoke con pantalla Full HD y micrófonos inalámbricos a su celebración.",
                RespuestaEn = "Absolutely. We coordinate musical genres and special songs for protocol or first dances in advance, and we can integrate our Full HD karaoke system with wireless microphones into your celebration.",
                Activo = true
            });

        await db.SaveChangesAsync();
    }

    private static async Task SembrarPromocionesTemporadaAsync(ApplicationDbContext db)
    {
        if (await db.PromocionesTemporada.AnyAsync())
        {
            return;
        }

        var paquetes = await db.Paquetes
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Nombre, p => p.Id);

        var inicio = DateTime.UtcNow.AddDays(-15);
        var fin = DateTime.UtcNow.AddMonths(4);

        var promoFinDeAnio = new PromocionTemporada
        {
            Nombre = "Temporada de Fin de Año y Graduaciones",
            NombreEn = "Year-End & Graduation Season",
            Descripcion = "Descuento especial de temporada en producción musical en vivo y DJ Open Format para fiestas empresariales, bailes de graduación y celebraciones de fin de año.",
            DescripcionEn = "Special seasonal discount on live musical production and Open Format DJ packages for corporate parties, proms, and year-end celebrations.",
            EtiquetaTemporada = "Fin de Año y Graduaciones",
            EtiquetaTemporadaEn = "Year-End & Graduations",
            PorcentajeDescuento = 15m,
            CodigoPromocional = "FIESTA15",
            FechaInicio = inicio,
            FechaFin = fin,
            DestacadaEnInicio = true,
            Activo = true
        };

        if (paquetes.TryGetValue("DJ Open Format", out var djId))
        {
            promoFinDeAnio.Paquetes.Add(new PromocionTemporadaPaquete { PaqueteId = djId });
        }
        if (paquetes.TryGetValue("Marimba Orquesta Son del Gallo (en vivo)", out var orquestaId))
        {
            promoFinDeAnio.Paquetes.Add(new PromocionTemporadaPaquete { PaqueteId = orquestaId });
        }

        var promoBodas = new PromocionTemporada
        {
            Nombre = "Especial Bodas y Recepciones",
            NombreEn = "Weddings & Receptions Special",
            Descripcion = "Tarifa preferencial en música en vivo con Villa Guanacaste y sonorización profesional para bodas civiles, religiosas y recepciones familiares.",
            DescripcionEn = "Preferential seasonal rate on live music with Villa Guanacaste and professional sound reinforcement for weddings and family receptions.",
            EtiquetaTemporada = "Temporada de Bodas",
            EtiquetaTemporadaEn = "Wedding Season",
            PorcentajeDescuento = 10m,
            CodigoPromocional = "BODAS10",
            FechaInicio = inicio,
            FechaFin = fin,
            DestacadaEnInicio = true,
            Activo = true
        };

        if (paquetes.TryGetValue("Grupo secuenciado Villa Guanacaste", out var villaId))
        {
            promoBodas.Paquetes.Add(new PromocionTemporadaPaquete { PaqueteId = villaId });
        }
        if (paquetes.TryGetValue("Sonorización para grupos o DJs", out var sonorizacionId))
        {
            promoBodas.Paquetes.Add(new PromocionTemporadaPaquete { PaqueteId = sonorizacionId });
        }

        db.PromocionesTemporada.AddRange(promoFinDeAnio, promoBodas);
        await db.SaveChangesAsync();
    }
}
