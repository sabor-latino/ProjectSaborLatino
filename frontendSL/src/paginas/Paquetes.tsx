import { Link, useSearchParams } from 'react-router-dom';
import type { Paquete, PromocionTemporada, Servicio } from '../api/tipos';
import { Cargando, Mensaje } from '../componentes/Estado';
import { TarjetaPaquete } from '../componentes/TarjetaPaquete';
import { useDatos } from '../hooks/useDatos';
import { usePreferencias } from '../preferencias/PreferenciasContext';
import { formatoFecha } from '../utilidades/formato';
import { obtenerPromocionParaPaquete } from '../utilidades/promociones';

export function Paquetes() {
  const { idioma, t } = usePreferencias();
  // El filtro se guarda en la URL (?servicio=2), así se puede compartir el enlace
  const [parametros, setParametros] = useSearchParams();
  const servicioId = parametros.get('servicio') ?? '';

  const servicios = useDatos<Servicio[]>('/api/servicios');
  const paquetes = useDatos<Paquete[]>(servicioId ? `/api/paquetes?servicioId=${servicioId}` : '/api/paquetes');
  const promociones = useDatos<PromocionTemporada[]>('/api/promociones');

  const cambiarServicio = (valor: string) => {
    setParametros(valor ? { servicio: valor } : {});
  };

  const listaServicios = Array.isArray(servicios.datos) ? servicios.datos : [];
  const opciones = [
    { id: '', nombre: t('Todos', 'All') },
    ...listaServicios.map((s) => ({
      id: String(s.id),
      nombre: t(s.nombre, s.nombreEn),
    })),
  ];

  const promocionesVigentes = Array.isArray(promociones.datos)
    ? promociones.datos.filter((p) => p.activo && p.vigente !== false)
    : [];

  return (
    <section className="seccion contenedor">
      <div className="encabezado">
        <p className="antetitulo">{t('Catálogo de paquetes', 'Package catalog')}</p>
        <h1 className="titulo-seccion">
          {t('Elija cómo quiere que suene su celebración', 'Choose how you want your celebration to sound')}
        </h1>
        <p className="intro">
          {t(
            'Cada paquete incluye transporte dentro del Gran Área Metropolitana, montaje profesional, prueba acústica e ingeniería de sonido en vivo. Solicite su cotización formal según la fecha, el lugar y la duración de su evento.',
            'Every package includes transport within the Greater Metropolitan Area, professional setup, soundcheck, and live audio engineering. Request a formal quote tailored to your event date, venue, and duration.',
          )}
        </p>
      </div>

      {promocionesVigentes.map((promo) => {
        const nombrePromo = t(promo.nombre, promo.nombreEn);
        const etiquetaPromo = promo.etiquetaTemporada ? t(promo.etiquetaTemporada, promo.etiquetaTemporadaEn || undefined) : t('Promoción de temporada', 'Seasonal promotion');
        const primerPaquete = promo.paquetes?.[0]?.paqueteId;
        const enlacePromo = primerPaquete
          ? `/solicitar?paquete=${primerPaquete}&promocion=${promo.id}`
          : `/solicitar?promocion=${promo.id}`;

        return (
          <div key={promo.id} className="promo-banner" role="region" aria-label={nombrePromo}>
            <div>
              <span className="promo-card__etiqueta">{etiquetaPromo}</span>
              <h2 className="subtitulo">
                {nombrePromo} · -{promo.porcentajeDescuento}%
              </h2>
              <p className="texto-suave">
                {t('Vigencia hasta el', 'Valid until')} {formatoFecha(promo.fechaFin)}
                {promo.codigoPromocional ? ` · ${t('Código:', 'Code:')} ${promo.codigoPromocional}` : ''}
              </p>
            </div>
            <Link to={enlacePromo} className="boton boton--accion boton--chico">
              {t('Aprovechar promoción', 'Claim promotion')}
            </Link>
          </div>
        );
      })}

      <div className="filtros" role="group" aria-label={t('Filtrar paquetes por servicio', 'Filter packages by service')}>
        {opciones.map((o) => (
          <button
            key={o.id || 'todos'}
            type="button"
            className="chip"
            aria-pressed={servicioId === o.id}
            onClick={() => cambiarServicio(o.id)}
          >
            {o.nombre}
          </button>
        ))}
      </div>

      <div aria-live="polite">
        {paquetes.cargando && <Cargando texto={t('Cargando paquetes…', 'Loading packages…')} />}
        {paquetes.error && <Mensaje tipo="error">{paquetes.error}</Mensaje>}
        {paquetes.datos && paquetes.datos.length === 0 && (
          <Mensaje>
            {t(
              'No hay paquetes disponibles para este servicio por ahora.',
              'There are no packages available for this service right now.',
            )}
          </Mensaje>
        )}
        {paquetes.datos && paquetes.datos.length > 0 && (
          <div className="paquetes">
            {paquetes.datos.map((p) => (
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
  );
}
