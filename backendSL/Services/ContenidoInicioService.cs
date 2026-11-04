using Microsoft.EntityFrameworkCore;
using ProjectSaborLatino.Data;
using ProjectSaborLatino.DTOs.Sistema;
using ProjectSaborLatino.Models;

namespace ProjectSaborLatino.Services;

public interface IContenidoInicioService
{
    Task<ContenidoInicioDto> ObtenerAsync();
    Task<ContenidoInicioDto> ActualizarAsync(ContenidoInicioDto dto);
}

public class ContenidoInicioService : IContenidoInicioService
{
    private const string Prefijo = "Inicio.";

    private readonly ApplicationDbContext _db;

    public ContenidoInicioService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ContenidoInicioDto> ObtenerAsync()
    {
        var filas = await _db.Configuraciones
            .AsNoTracking()
            .Where(c => c.Clave.StartsWith(Prefijo))
            .ToDictionaryAsync(c => c.Clave, c => c.Valor);

        var predeterminado = ObtenerPredeterminado();

        string Leer(string clave, string defecto) =>
            filas.TryGetValue(clave, out var v) && !string.IsNullOrWhiteSpace(v) ? v : defecto;

        string? LeerOpcional(string clave, string? defecto) =>
            filas.TryGetValue(clave, out var v) ? (string.IsNullOrWhiteSpace(v) ? null : v) : defecto;

        return new ContenidoInicioDto
        {
            EtiquetaPortada = Leer("Inicio.EtiquetaPortada", predeterminado.EtiquetaPortada),
            EtiquetaPortadaEn = Leer("Inicio.EtiquetaPortadaEn", predeterminado.EtiquetaPortadaEn),
            TituloPortada = Leer("Inicio.TituloPortada", predeterminado.TituloPortada),
            TituloPortadaEn = Leer("Inicio.TituloPortadaEn", predeterminado.TituloPortadaEn),
            SubtituloPortada = Leer("Inicio.SubtituloPortada", predeterminado.SubtituloPortada),
            SubtituloPortadaEn = Leer("Inicio.SubtituloPortadaEn", predeterminado.SubtituloPortadaEn),
            Paso1Titulo = Leer("Inicio.Paso1Titulo", predeterminado.Paso1Titulo),
            Paso1TituloEn = Leer("Inicio.Paso1TituloEn", predeterminado.Paso1TituloEn),
            Paso1Texto = Leer("Inicio.Paso1Texto", predeterminado.Paso1Texto),
            Paso1TextoEn = Leer("Inicio.Paso1TextoEn", predeterminado.Paso1TextoEn),
            Paso2Titulo = Leer("Inicio.Paso2Titulo", predeterminado.Paso2Titulo),
            Paso2TituloEn = Leer("Inicio.Paso2TituloEn", predeterminado.Paso2TituloEn),
            Paso2Texto = Leer("Inicio.Paso2Texto", predeterminado.Paso2Texto),
            Paso2TextoEn = Leer("Inicio.Paso2TextoEn", predeterminado.Paso2TextoEn),
            Paso3Titulo = Leer("Inicio.Paso3Titulo", predeterminado.Paso3Titulo),
            Paso3TituloEn = Leer("Inicio.Paso3TituloEn", predeterminado.Paso3TituloEn),
            Paso3Texto = Leer("Inicio.Paso3Texto", predeterminado.Paso3Texto),
            Paso3TextoEn = Leer("Inicio.Paso3TextoEn", predeterminado.Paso3TextoEn),
            CtaTitulo = Leer("Inicio.CtaTitulo", predeterminado.CtaTitulo),
            CtaTituloEn = Leer("Inicio.CtaTituloEn", predeterminado.CtaTituloEn),
            CtaSubtitulo = Leer("Inicio.CtaSubtitulo", predeterminado.CtaSubtitulo),
            CtaSubtituloEn = Leer("Inicio.CtaSubtituloEn", predeterminado.CtaSubtituloEn),
            FotoPortadaUrl = LeerOpcional("Inicio.FotoPortadaUrl", predeterminado.FotoPortadaUrl),
            FotoCotizarUrl = LeerOpcional("Inicio.FotoCotizarUrl", predeterminado.FotoCotizarUrl),
            RedSocialFacebook = LeerOpcional("Inicio.RedSocialFacebook", predeterminado.RedSocialFacebook),
            RedSocialInstagram = LeerOpcional("Inicio.RedSocialInstagram", predeterminado.RedSocialInstagram),
            RedSocialTikTok = LeerOpcional("Inicio.RedSocialTikTok", predeterminado.RedSocialTikTok),
            RedSocialYouTube = LeerOpcional("Inicio.RedSocialYouTube", predeterminado.RedSocialYouTube),
            RedSocialWhatsApp = LeerOpcional("Inicio.RedSocialWhatsApp", predeterminado.RedSocialWhatsApp)
        };
    }

    public async Task<ContenidoInicioDto> ActualizarAsync(ContenidoInicioDto dto)
    {
        Validar(dto);

        var pares = new (string Clave, string Valor, string Descripcion)[]
        {
            ("Inicio.EtiquetaPortada", dto.EtiquetaPortada.Trim(), "Etiqueta superior de la portada (ES)."),
            ("Inicio.EtiquetaPortadaEn", dto.EtiquetaPortadaEn.Trim(), "Etiqueta superior de la portada (EN)."),
            ("Inicio.TituloPortada", dto.TituloPortada.Trim(), "Título principal de la portada (ES)."),
            ("Inicio.TituloPortadaEn", dto.TituloPortadaEn.Trim(), "Título principal de la portada (EN)."),
            ("Inicio.SubtituloPortada", dto.SubtituloPortada.Trim(), "Subtítulo descriptivo de la portada (ES)."),
            ("Inicio.SubtituloPortadaEn", dto.SubtituloPortadaEn.Trim(), "Subtítulo descriptivo de la portada (EN)."),
            ("Inicio.Paso1Titulo", dto.Paso1Titulo.Trim(), "Título del paso 1 en inicio (ES)."),
            ("Inicio.Paso1TituloEn", dto.Paso1TituloEn.Trim(), "Título del paso 1 en inicio (EN)."),
            ("Inicio.Paso1Texto", dto.Paso1Texto.Trim(), "Descripción del paso 1 en inicio (ES)."),
            ("Inicio.Paso1TextoEn", dto.Paso1TextoEn.Trim(), "Descripción del paso 1 en inicio (EN)."),
            ("Inicio.Paso2Titulo", dto.Paso2Titulo.Trim(), "Título del paso 2 en inicio (ES)."),
            ("Inicio.Paso2TituloEn", dto.Paso2TituloEn.Trim(), "Título del paso 2 en inicio (EN)."),
            ("Inicio.Paso2Texto", dto.Paso2Texto.Trim(), "Descripción del paso 2 en inicio (ES)."),
            ("Inicio.Paso2TextoEn", dto.Paso2TextoEn.Trim(), "Descripción del paso 2 en inicio (EN)."),
            ("Inicio.Paso3Titulo", dto.Paso3Titulo.Trim(), "Título del paso 3 en inicio (ES)."),
            ("Inicio.Paso3TituloEn", dto.Paso3TituloEn.Trim(), "Título del paso 3 en inicio (EN)."),
            ("Inicio.Paso3Texto", dto.Paso3Texto.Trim(), "Descripción del paso 3 en inicio (ES)."),
            ("Inicio.Paso3TextoEn", dto.Paso3TextoEn.Trim(), "Descripción del paso 3 en inicio (EN)."),
            ("Inicio.CtaTitulo", dto.CtaTitulo.Trim(), "Título del llamado a la acción en inicio (ES)."),
            ("Inicio.CtaTituloEn", dto.CtaTituloEn.Trim(), "Título del llamado a la acción en inicio (EN)."),
            ("Inicio.CtaSubtitulo", dto.CtaSubtitulo.Trim(), "Subtítulo del llamado a la acción en inicio (ES)."),
            ("Inicio.CtaSubtituloEn", dto.CtaSubtituloEn.Trim(), "Subtítulo del llamado a la acción en inicio (EN)."),
            ("Inicio.FotoPortadaUrl", dto.FotoPortadaUrl?.Trim() ?? string.Empty, "URL de la fotografía principal de portada."),
            ("Inicio.FotoCotizarUrl", dto.FotoCotizarUrl?.Trim() ?? string.Empty, "URL de la fotografía de la sección de cotización."),
            ("Inicio.RedSocialFacebook", dto.RedSocialFacebook?.Trim() ?? string.Empty, "URL oficial de Facebook de Sabor Latino."),
            ("Inicio.RedSocialInstagram", dto.RedSocialInstagram?.Trim() ?? string.Empty, "URL oficial de Instagram de Sabor Latino."),
            ("Inicio.RedSocialTikTok", dto.RedSocialTikTok?.Trim() ?? string.Empty, "URL oficial de TikTok de Sabor Latino."),
            ("Inicio.RedSocialYouTube", dto.RedSocialYouTube?.Trim() ?? string.Empty, "URL oficial de YouTube de Sabor Latino."),
            ("Inicio.RedSocialWhatsApp", dto.RedSocialWhatsApp?.Trim() ?? string.Empty, "Número de WhatsApp oficial para atención directa.")
        };

        var existentes = await _db.Configuraciones
            .Where(c => c.Clave.StartsWith(Prefijo))
            .ToDictionaryAsync(c => c.Clave);

        foreach (var (clave, valor, descripcion) in pares)
        {
            if (existentes.TryGetValue(clave, out var entidad))
            {
                entidad.Valor = valor;
                entidad.Descripcion = descripcion;
            }
            else
            {
                _db.Configuraciones.Add(new Configuracion
                {
                    Clave = clave,
                    Valor = valor,
                    Descripcion = descripcion
                });
            }
        }

        await _db.SaveChangesAsync();
        return await ObtenerAsync();
    }

    private static void Validar(ContenidoInicioDto dto)
    {
        ValidarCampoObligatorio(dto.EtiquetaPortada, nameof(dto.EtiquetaPortada), 120);
        ValidarCampoObligatorio(dto.EtiquetaPortadaEn, nameof(dto.EtiquetaPortadaEn), 120);
        ValidarCampoObligatorio(dto.TituloPortada, nameof(dto.TituloPortada), 250);
        ValidarCampoObligatorio(dto.TituloPortadaEn, nameof(dto.TituloPortadaEn), 250);
        ValidarCampoObligatorio(dto.SubtituloPortada, nameof(dto.SubtituloPortada), 500);
        ValidarCampoObligatorio(dto.SubtituloPortadaEn, nameof(dto.SubtituloPortadaEn), 500);
        ValidarCampoObligatorio(dto.Paso1Titulo, nameof(dto.Paso1Titulo), 150);
        ValidarCampoObligatorio(dto.Paso1TituloEn, nameof(dto.Paso1TituloEn), 150);
        ValidarCampoObligatorio(dto.Paso1Texto, nameof(dto.Paso1Texto), 500);
        ValidarCampoObligatorio(dto.Paso1TextoEn, nameof(dto.Paso1TextoEn), 500);
        ValidarCampoObligatorio(dto.Paso2Titulo, nameof(dto.Paso2Titulo), 150);
        ValidarCampoObligatorio(dto.Paso2TituloEn, nameof(dto.Paso2TituloEn), 150);
        ValidarCampoObligatorio(dto.Paso2Texto, nameof(dto.Paso2Texto), 500);
        ValidarCampoObligatorio(dto.Paso2TextoEn, nameof(dto.Paso2TextoEn), 500);
        ValidarCampoObligatorio(dto.Paso3Titulo, nameof(dto.Paso3Titulo), 150);
        ValidarCampoObligatorio(dto.Paso3TituloEn, nameof(dto.Paso3TituloEn), 150);
        ValidarCampoObligatorio(dto.Paso3Texto, nameof(dto.Paso3Texto), 500);
        ValidarCampoObligatorio(dto.Paso3TextoEn, nameof(dto.Paso3TextoEn), 500);
        ValidarCampoObligatorio(dto.CtaTitulo, nameof(dto.CtaTitulo), 200);
        ValidarCampoObligatorio(dto.CtaTituloEn, nameof(dto.CtaTituloEn), 200);
        ValidarCampoObligatorio(dto.CtaSubtitulo, nameof(dto.CtaSubtitulo), 500);
        ValidarCampoObligatorio(dto.CtaSubtituloEn, nameof(dto.CtaSubtituloEn), 500);
    }

    private static void ValidarCampoObligatorio(string valor, string nombre, int maxLongitud)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DatosInvalidosException($"El campo {nombre} es obligatorio.");
        }

        if (valor.Trim().Length > maxLongitud)
        {
            throw new DatosInvalidosException($"El campo {nombre} no puede exceder {maxLongitud} caracteres.");
        }
    }

    private static ContenidoInicioDto ObtenerPredeterminado() => new()
    {
        EtiquetaPortada = "Producción musical y técnica en Costa Rica",
        EtiquetaPortadaEn = "Live Music & Technical Production in Costa Rica",
        TituloPortada = "La energía de la música en vivo y el sonido profesional para su evento",
        TituloPortadaEn = "The energy of live music and professional sound for your event",
        SubtituloPortada = "Orquesta en vivo, grupos musicales, DJ Open Format, karaoke y sonorización integral con especificaciones técnicas claras para bodas, graduaciones y eventos corporativos.",
        SubtituloPortadaEn = "Live orchestra, musical ensembles, Open Format DJ, karaoke, and full sound & lighting production with clear technical specs for weddings, graduations, and corporate events.",
        Paso1Titulo = "1. Explore el catálogo y sus fichas técnicas",
        Paso1TituloEn = "1. Explore the catalog and technical sheets",
        Paso1Texto = "Compare paquetes, potencia de audio, cobertura de aforo, tiempos de montaje y lista detallada de equipos incluidos.",
        Paso1TextoEn = "Compare packages, sound power, guest capacity, setup times, and the detailed list of included equipment.",
        Paso2Titulo = "2. Solicite su cotización en minutos",
        Paso2TituloEn = "2. Request your quote in minutes",
        Paso2Texto = "Indique la fecha, lugar, cantidad de invitados y el paquete de su interés; aplicamos promociones de temporada y descuentos de fidelidad.",
        Paso2TextoEn = "Share your event date, venue, guest count, and preferred package; we apply seasonal promotions and loyalty discounts.",
        Paso3Titulo = "3. Confirmación y montaje puntual",
        Paso3TituloEn = "3. Confirmation and punctual setup",
        Paso3Texto = "Coordinamos nuestro equipo técnico y musical con antelación para que usted solo se dedique a disfrutar de la celebración.",
        Paso3TextoEn = "We coordinate our technical and musical crew in advance so you can simply enjoy your celebration.",
        CtaTitulo = "¿Listo para ponerle ritmo a su próximo evento?",
        CtaTituloEn = "Ready to bring rhythm to your next event?",
        CtaSubtitulo = "Envíenos los datos de su actividad y reciba una propuesta formal adaptada a su recinto y presupuesto.",
        CtaSubtituloEn = "Send us your event details and receive a formal proposal tailored to your venue and budget.",
        FotoPortadaUrl = null,
        FotoCotizarUrl = null,
        RedSocialFacebook = "https://facebook.com/saborlatinocr",
        RedSocialInstagram = "https://instagram.com/saborlatinocr",
        RedSocialTikTok = "https://tiktok.com/@saborlatinocr",
        RedSocialYouTube = "https://youtube.com/@saborlatinocr",
        RedSocialWhatsApp = "50685262096"
    };
}
