import { Link } from 'react-router-dom';
import type { ContenidoInicio, ImagenGaleria, Paquete, PromocionTemporada, Servicio } from '../api/tipos';
import { Cargando, Mensaje } from '../componentes/Estado';
import { IconoCheck, IconoFlecha } from '../componentes/Iconos';
import { TarjetaPaquete } from '../componentes/TarjetaPaquete';
import { useDatos } from '../hooks/useDatos';
import { usePreferencias } from '../preferencias/PreferenciasContext';
import { formatoColones, formatoFecha } from '../utilidades/formato';
import { FOTO_COTIZAR, FOTO_PORTADA, GALERIA_EJEMPLO } from '../utilidades/fotos';
import { obtenerPromocionParaPaquete } from '../utilidades/promociones';

const CONTENIDO_BASE: ContenidoInicio = {
  etiquetaPortada: 'Bodas · Quince años · Aniversarios · Eventos corporativos',
  etiquetaPortadaEn: 'Weddings · Sweet 15s · Anniversaries · Corporate events',
  tituloPortada: 'Que su fiesta suene en vivo',
  tituloPortadaEn: 'Bring your celebration to life with live sound',
  subtituloPortada:
    'Karaoke interactivo, marimba, discomóvil, orquesta e iluminación escénica en toda Costa Rica. Usted elige el paquete ideal; nosotros llevamos el espectáculo, la ingeniería de sonido y el personal técnico.',
  subtituloPortadaEn:
    'Interactive karaoke, marimba, mobile DJ, live orchestra, and stage lighting across Costa Rica. You choose the package; we bring the show, audio engineering, and technical crew.',
  fotoPortadaUrl: null,
  fotoCotizarUrl: null,
  paso1Titulo: 'Cuéntenos sobre su evento',
  paso1TituloEn: 'Tell us about your event',
  paso1Texto: 'Indique la fecha, el lugar, el aforo estimado y el paquete de entretenimiento que más le interesa.',
  paso1TextoEn: 'Share the date, venue, estimated guest count, and the entertainment package you prefer.',
  paso2Titulo: 'Reciba su cotización formal',
  paso2TituloEn: 'Receive your formal quote',
  paso2Texto:
    'Le enviamos una propuesta detallada en colones con el desglose técnico de audio, iluminación y horas de servicio.',
  paso2TextoEn:
    'We send a clear proposal in colones with the full technical breakdown of sound, lighting, and service hours.',
  paso3Titulo: 'Disfrute la celebración',
  paso3TituloEn: 'Enjoy the celebration',
  paso3Texto:
    'Nuestro equipo técnico llega con anticipación para el montaje y la prueba acústica, garantizando sonido impecable de principio a fin.',
  paso3TextoEn:
    'Our technical crew arrives early for setup and soundcheck, ensuring flawless audio from the very first song.',
  ctaTitulo: 'Aparte su fecha antes de que se agote la agenda',
  ctaTituloEn: 'Reserve your date before the calendar fills up',
  ctaSubtitulo: 'Solicitar su cotización personalizada no tiene ningún costo ni compromiso.',
  ctaSubtituloEn: 'Requesting your personalized quote is completely free and without obligation.',
};

export function Inicio() {
  const { idioma, t } = usePreferencias();
  const contenido = useDatos<ContenidoInicio>('/api/contenido-inicio');
  const promociones = useDatos<PromocionTemporada[]>('/api/promociones');
  const servicios = useDatos<Servicio[]>('/api/servicios');
  const paquetes = useDatos<Paquete[]>('/api/paquetes');
  const galeria = useDatos<ImagenGaleria[]>('/api/galeria');

  const c =
    contenido.datos && !Array.isArray(contenido.datos) && contenido.datos.tituloPortada
      ? contenido.datos
      : CONTENIDO_BASE;

  const etiquetaPortada =
    idioma === 'en' ? c.etiquetaPortadaEn || CONTENIDO_BASE.etiquetaPortadaEn : c.etiquetaPortada || CONTENIDO_BASE.etiquetaPortada;
  const tituloPortada =
    idioma === 'en' ? c.tituloPortadaEn || CONTENIDO_BASE.tituloPortadaEn : c.tituloPortada || CONTENIDO_BASE.tituloPortada;
  const subtituloPortada =
    idioma === 'en'
      ? c.subtituloPortadaEn || CONTENIDO_BASE.subtituloPortadaEn
      : c.subtituloPortada || CONTENIDO_BASE.subtituloPortada;

  const pasos = [
    {
      titulo: idioma === 'en' ? c.paso1TituloEn || CONTENIDO_BASE.paso1TituloEn : c.paso1Titulo || CONTENIDO_BASE.paso1Titulo,
      texto: idioma === 'en' ? c.paso1TextoEn || CONTENIDO_BASE.paso1TextoEn : c.paso1Texto || CONTENIDO_BASE.paso1Texto,
    },
    {
      titulo: idioma === 'en' ? c.paso2TituloEn || CONTENIDO_BASE.paso2TituloEn : c.paso2Titulo || CONTENIDO_BASE.paso2Titulo,
      texto: idioma === 'en' ? c.paso2TextoEn || CONTENIDO_BASE.paso2TextoEn : c.paso2Texto || CONTENIDO_BASE.paso2Texto,
    },
    {
      titulo: idioma === 'en' ? c.paso3TituloEn || CONTENIDO_BASE.paso3TituloEn : c.paso3Titulo || CONTENIDO_BASE.paso3Titulo,
      texto: idioma === 'en' ? c.paso3TextoEn || CONTENIDO_BASE.paso3TextoEn : c.paso3Texto || CONTENIDO_BASE.paso3Texto,
    },
  ];

  const ctaTitulo = idioma === 'en' ? c.ctaTituloEn || CONTENIDO_BASE.ctaTituloEn : c.ctaTitulo || CONTENIDO_BASE.ctaTitulo;
  const ctaSubtitulo =
    idioma === 'en' ? c.ctaSubtituloEn || CONTENIDO_BASE.ctaSubtituloEn : c.ctaSubtitulo || CONTENIDO_BASE.ctaSubtitulo;

  const listaPaquetes = Array.isArray(paquetes.datos) ? paquetes.datos : [];
  const listaServicios = Array.isArray(servicios.datos) ? servicios.datos : [];
  const listaGaleria = Array.isArray(galeria.datos) ? galeria.datos : [];
  const listaPromociones = Array.isArray(promociones.datos)
    ? promociones.datos.filter((p) => p.activo && p.vigente !== false && p.destacadaEnInicio)
    : [];

  const precioMinimo = listaPaquetes.length ? Math.min(...listaPaquetes.map((p) => p.precioBase)) : null;
  const fotosGaleria = listaGaleria.length ? listaGaleria.slice(0, 3) : null;

  return (
    <>
      <section className="portada" aria-labelledby="titulo-portada">
        <img
          className="portada__foto"
          src={c.fotoPortadaUrl || FOTO_PORTADA}
          alt=""
          width={2000}
          height={1333}
          fetchPriority="high"
        />
        <div className="contenedor portada__interior">
          <span className="portada__etiqueta">{etiquetaPortada}</span>
          <h1 id="titulo-portada" className="portada__titulo">
            {tituloPortada}
          </h1>
          <p className="portada__texto">{subtituloPortada}</p>
          <div className="acciones">
            <Link to="/solicitar" className="boton boton--accion">
              {t('Solicitar cotización', 'Request a quote')}
              <IconoFlecha />
            </Link>
            <Link to="/paquetes" className="boton boton--vidrio">
              {t('Ver paquetes y especificaciones', 'View packages & specs')}
            </Link>
          </div>
          {listaPaquetes.length > 0 && precioMinimo !== null && (
            <div className="datos">
              <div className="dato">
                <strong>{listaPaquetes.length}</strong>
                <span>{t('paquetes disponibles', 'packages ready')}</span>
              </div>
              <div className="dato">
                <strong>{formatoColones(precioMinimo)}</strong>
                <span>{t('inversión desde', 'starting at')}</span>
              </div>
              <div className="dato">
                <strong>{t('Montaje', 'Full setup')}</strong>
                <span>{t('e ingeniería incluidos', '& sound engineering included')}</span>
              </div>
            </div>
          )}
        </div>
        <span className="credito">{t('Fotografía de referencia', 'Reference photography')}</span>
      </section>

      {listaPromociones.length > 0 && (
        <section className="seccion contenedor" aria-labelledby="titulo-promociones">
          <div className="encabezado">
            <p className="antetitulo">{t('Ofertas por tiempo limitado', 'Limited-time offers')}</p>
            <h2 id="titulo-promociones" className="titulo-seccion">
              {t('Promociones de Temporada', 'Seasonal Promotions')}
            </h2>
            <p className="intro">
              {t(
                'Reserve su fecha durante la vigencia de nuestras campañas de temporada y obtenga tarifas preferenciales en paquetes seleccionados.',
                'Book your date during our seasonal campaigns and enjoy preferential rates on selected packages.',
              )}
            </p>
          </div>

          <div className="promociones-grid">
            {listaPromociones.map((promo) => {
              const nombrePromo = idioma === 'en' && promo.nombreEn ? promo.nombreEn : promo.nombre;
              const descPromo = idioma === 'en' && promo.descripcionEn ? promo.descripcionEn : promo.descripcion;
              const etiquetaTemp =
                idioma === 'en' && promo.etiquetaTemporadaEn
                  ? promo.etiquetaTemporadaEn
                  : promo.etiquetaTemporada || t('Temporada especial', 'Special season');
              const primerPaquete = promo.paquetes?.[0]?.paqueteId;
              const enlacePromo = primerPaquete
                ? `/solicitar?paquete=${primerPaquete}&promocion=${promo.id}`
                : `/solicitar?promocion=${promo.id}`;

              return (
                <article key={promo.id} className="promo-card">
                  <div className="promo-card__cabecera">
                    <span className="promo-card__etiqueta">{etiquetaTemp}</span>
                    <span className="promo-card__descuento">-{promo.porcentajeDescuento}%</span>
                  </div>
                  <h3>{nombrePromo}</h3>
                  {descPromo && <p className="texto-suave">{descPromo}</p>}
                  {promo.codigoPromocional && (
                    <p className="promo-card__codigo">
                      <span>{t('Código promocional:', 'Promo code:')}</span>{' '}
                      <strong>{promo.codigoPromocional}</strong>
                    </p>
                  )}
                  <p className="texto-suave">
                    {t('Válida hasta el', 'Valid until')} {formatoFecha(promo.fechaFin)}
                  </p>
                  {promo.paquetes && promo.paquetes.length > 0 && (
                    <ul className="promo-card__paquetes" aria-label={t('Paquetes incluidos', 'Included packages')}>
                      {promo.paquetes.map((pk) => {
                        const pkNombre = idioma === 'en' && pk.paqueteNombreEn ? pk.paqueteNombreEn : pk.paqueteNombre;
                        return (
                          <li key={pk.paqueteId} className="promo-card__paquete">
                            <span>{pkNombre}</span>
                            <span>
                              <span className="precio__anterior">{formatoColones(pk.precioBase)}</span>{' '}
                              <strong className="numero">{formatoColones(pk.precioPromocional)}</strong>
                            </span>
                          </li>
                        );
                      })}
                    </ul>
                  )}
                  <div className="acciones">
                    <Link to={enlacePromo} className="boton boton--accion boton--chico">
                      {t('Aprovechar promoción', 'Claim promotion')}
                    </Link>
                  </div>
                </article>
              );
            })}
          </div>
        </section>
      )}

      <section className="seccion contenedor" aria-labelledby="titulo-pasos">
        <div className="encabezado">
          <p className="antetitulo">{t('Cómo funciona', 'How it works')}</p>
          <h2 id="titulo-pasos" className="titulo-seccion">
            {t('Tres pasos y a celebrar', 'Three steps to celebrate')}
          </h2>
        </div>
        <ol className="pasos">
          {pasos.map((paso, i) => (
            <li key={i} className="paso">
              <span className="paso__n" aria-hidden="true">
                {i + 1}
              </span>
              <h3>{paso.titulo}</h3>
              <p>{paso.texto}</p>
            </li>
          ))}
        </ol>
      </section>

      <section className="seccion seccion--suave" aria-labelledby="titulo-paquetes">
        <div className="contenedor">
          <div className="encabezado encabezado--fila">
            <div className="encabezado encabezado--sin-margen">
              <p className="antetitulo">{t('Catálogo de paquetes', 'Package catalog')}</p>
              <h2 id="titulo-paquetes" className="titulo-seccion">
                {t('Elija cómo quiere que suene su evento', 'Choose how you want your event to sound')}
              </h2>
            </div>
            <Link to="/paquetes" className="enlace">
              {t('Ver todos los paquetes →', 'See all packages →')}
            </Link>
          </div>

          {listaServicios.length > 0 && (
            <div
              className="servicios servicios--separados"
              role="navigation"
              aria-label={t('Categorías de servicio', 'Service categories')}
            >
              {listaServicios.map((s) => {
                const nombreServ = idioma === 'en' && s.nombreEn ? s.nombreEn : s.nombre;
                return (
                  <Link
                    key={s.id}
                    to={`/paquetes?servicio=${s.id}`}
                    className="servicio"
                    title={t(`Ver paquetes de ${nombreServ}`, `View ${nombreServ} packages`)}
                  >
                    <h3>{nombreServ}</h3>
                    <span className="servicio__flecha" aria-hidden="true">
                      →
                    </span>
                  </Link>
                );
              })}
            </div>
          )}

          {paquetes.cargando && <Cargando texto={t('Cargando paquetes…', 'Loading packages…')} />}
          {paquetes.error && <Mensaje tipo="error">{paquetes.error}</Mensaje>}
          {paquetes.datos && (
            <div className="paquetes paquetes--tres">
              {listaPaquetes.slice(0, 3).map((p) => (
                <TarjetaPaquete
                  key={p.id}
                  paquete={p}
                  promocion={obtenerPromocionParaPaquete(p, promociones.datos, idioma)}
                />
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="seccion contenedor" aria-labelledby="titulo-galeria">
        <div className="encabezado encabezado--fila">
          <div className="encabezado encabezado--sin-margen">
            <p className="antetitulo">{t('Galería de eventos', 'Event gallery')}</p>
            <h2 id="titulo-galeria" className="titulo-seccion">
              {t('Así se viven nuestras noches', 'Experience our live nights')}
            </h2>
          </div>
          <Link to="/galeria" className="enlace">
            {t('Ver la galería →', 'View gallery →')}
          </Link>
        </div>
        {!fotosGaleria && <span className="nota-ejemplo">{t('Fotos de ejemplo', 'Sample photos')}</span>}
        <div className="galeria">
          {(fotosGaleria ?? GALERIA_EJEMPLO.filter((f) => f.alto === 467).slice(0, 3)).map((img) => (
            <figure key={img.id} className="galeria__item">
              <img src={img.imagenUrl} alt={t(img.titulo, img.tituloEn?.trim() || img.titulo)} width={700} height={467} loading="lazy" decoding="async" />
              <figcaption>{t(img.titulo, img.tituloEn?.trim() || img.titulo)}</figcaption>
            </figure>
          ))}
        </div>
      </section>

      <section className="seccion banda" aria-labelledby="titulo-cotizar">
        <img
          className="banda__foto"
          src={c.fotoCotizarUrl || FOTO_COTIZAR}
          alt=""
          width={1800}
          height={1200}
          loading="lazy"
        />
        <div className="contenedor banda__grid">
          <div className="encabezado encabezado--sin-margen">
            <p className="antetitulo">{t('Solicitar cotización', 'Request a quote')}</p>
            <h2 id="titulo-cotizar" className="titulo-seccion">
              {ctaTitulo}
            </h2>
            <p className="intro">{ctaSubtitulo}</p>
            <ul className="ventajas">
              <li>
                <IconoCheck />
                {t('Cotización transparente en colones costarricenses', 'Transparent pricing in Costa Rican colones')}
              </li>
              <li>
                <IconoCheck />
                {t('Transporte, montaje y prueba acústica incluidos', 'Transport, setup, and soundcheck included')}
              </li>
              <li>
                <IconoCheck />
                {t(
                  'Solicitud registrada y atención personalizada por WhatsApp',
                  'Saved request and personalized assistance on WhatsApp',
                )}
              </li>
            </ul>
          </div>
          <div className="acciones acciones--centro">
            <Link to="/solicitar" className="boton boton--accion">
              {t('Solicitar cotización', 'Request a quote')}
              <IconoFlecha />
            </Link>
            <Link to="/preguntas" className="boton boton--vidrio">
              {t('Ver preguntas frecuentes', 'View FAQ')}
            </Link>
          </div>
        </div>
      </section>
    </>
  );
}
