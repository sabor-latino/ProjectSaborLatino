/* eslint-disable react-refresh/only-export-components */
import { Link } from 'react-router-dom';
import type { Paquete } from '../api/tipos';
import { usePreferencias } from '../preferencias/PreferenciasContext';
import { formatoColones } from '../utilidades/formato';
import { fotoDePaquete } from '../utilidades/fotos';

export interface PromocionAplicablePaquete {
  id: number;
  etiqueta: string;
  porcentajeDescuento: number;
  precioPromocional: number;
}

export function obtenerInsigniaComercial(
  paquete: Paquete,
  t: (es: string, en: string) => string,
): string {
  const nom = `${paquete.nombre} ${paquete.nombreEn ?? ''}`.toLowerCase();
  const srv = `${paquete.servicioNombre} ${paquete.servicioNombreEn ?? ''}`.toLowerCase();
  const desc = `${paquete.descripcion ?? ''} ${paquete.descripcionEn ?? ''}`.toLowerCase();
  const combinada = `${nom} ${srv} ${desc}`;

  // 1. Sonorización / producción acústica profesional
  if (
    nom.includes('sonoriza') ||
    srv.includes('sonoriza') ||
    srv.includes('sound & production') ||
    (srv.includes('sonido') && !combinada.includes('discomóvil') && !combinada.includes('karaoke'))
  ) {
    return t('⚡ Alta fidelidad', '⚡ High fidelity');
  }

  // 2. Orquestas, grupos musicales y ensambles
  if (
    nom.includes('orquesta') ||
    nom.includes('grupo') ||
    srv.includes('música en vivo') ||
    srv.includes('live music') ||
    combinada.includes('boda') ||
    combinada.includes('wedding')
  ) {
    return t('💍 Recomendado para bodas', '💍 Recommended for weddings');
  }

  // 3. DJ, Karaoke, Discomóvil
  if (
    combinada.includes('dj') ||
    combinada.includes('karaoke') ||
    combinada.includes('discomóvil') ||
    combinada.includes('discomovil')
  ) {
    return t('⭐ Más cotizado', '⭐ Most popular');
  }

  return t('⭐ Más cotizado', '⭐ Most popular');
}

// Tarjeta de un paquete: foto grande, equipo incluido, ficha técnica desplegable, promoción y botón para cotizar
export function TarjetaPaquete({
  paquete,
  promocion,
}: {
  paquete: Paquete;
  promocion?: PromocionAplicablePaquete;
}) {
  const { idioma, t } = usePreferencias();

  const nombre = idioma === 'en' && paquete.nombreEn ? paquete.nombreEn : paquete.nombre;
  const descripcion = idioma === 'en' && paquete.descripcionEn ? paquete.descripcionEn : paquete.descripcion;
  const servicioNombre =
    idioma === 'en' && paquete.servicioNombreEn ? paquete.servicioNombreEn : paquete.servicioNombre;
  const fichaTecnica = idioma === 'en' && paquete.fichaTecnicaEn ? paquete.fichaTecnicaEn : paquete.fichaTecnica;
  const insigniaComercial = obtenerInsigniaComercial(paquete, t);

  const tieneFichaTecnica = Boolean(
    paquete.potenciaSonido ||
      paquete.coberturaAforo ||
      paquete.duracionMontaje ||
      paquete.requerimientosElectricos ||
      paquete.espacioMinimo ||
      fichaTecnica ||
      paquete.equipos.some((e) => e.especificacionTecnica || e.especificacionTecnicaEn),
  );

  const rutaSolicitar = promocion
    ? `/solicitar?paquete=${paquete.id}&promocion=${promocion.id}`
    : `/solicitar?paquete=${paquete.id}`;

  const tienePantalla =
    paquete.equipos.some((e) => `${e.equipoNombre} ${e.especificacionTecnica || ''}`.toLowerCase().includes('pantalla')) ||
    servicioNombre.toLowerCase().includes('karaoke') ||
    nombre.toLowerCase().includes('karaoke');

  return (
    <article className="paquete">
      <div className="paquete__foto">
        <img src={fotoDePaquete(paquete)} alt="" width={800} height={600} loading="lazy" decoding="async" />
        <span className="paquete__servicio">{servicioNombre}</span>
        {promocion && (
          <span className="paquete__insignia-promo">
            {promocion.etiqueta} · -{promocion.porcentajeDescuento}%
          </span>
        )}
      </div>

      <div className="paquete__cuerpo">
        <span className="paquete__insignia-comercial">{insigniaComercial}</span>
        <h3>{nombre}</h3>

        <div className="paquete__destacados-tecnicos" role="list" aria-label={t('Aspectos técnicos destacados', 'Key technical highlights')}>
          {paquete.potenciaSonido && (
            <span className="insignia-tecnica insignia-tecnica--potencia" title={t('Potencia acústica profesional del paquete', 'Professional acoustic power rating')}>
              <span aria-hidden="true">⚡</span>
              <span>{paquete.potenciaSonido}</span>
            </span>
          )}
          {tienePantalla && (
            <span className="insignia-tecnica insignia-tecnica--pantalla" title={t('Pantalla de karaoke LED de 55 pulgadas Full HD en pedestal ajustable incluida', '55-inch Full HD LED karaoke screen on adjustable stand included')}>
              <span aria-hidden="true">📺</span>
              <span>{t('Pantalla LED 55" Full HD', '55" Full HD LED Screen')}</span>
            </span>
          )}
        </div>

        <div className="paquete__horas" aria-label={t('Horas de música incluidas', 'Included music hours')}>
          <span className="paquete__horas-icono" aria-hidden="true">⏱️</span>
          <span>{t('4 horas de música incluidas · Hora adicional disponible', '4 hours of music included · Additional hour available')}</span>
        </div>

        {descripcion && <p className="paquete__descripcion">{descripcion}</p>}

        {paquete.equipos.length > 0 && (
          <ul className="equipo" aria-label={t('Equipo incluido', 'Included equipment')}>
            {paquete.equipos.map((e) => {
              const eqNombre = idioma === 'en' && e.equipoNombreEn ? e.equipoNombreEn : e.equipoNombre;
              const eqSpec =
                idioma === 'en' && e.especificacionTecnicaEn
                  ? e.especificacionTecnicaEn
                  : e.especificacionTecnica;
              return (
                <li key={e.equipoId} title={eqSpec ?? undefined}>
                  {e.cantidad} × {eqNombre}
                </li>
              );
            })}
          </ul>
        )}

        {tieneFichaTecnica && (
          <details className="paquete__ficha">
            <summary>{t('Especificaciones técnicas y montaje', 'Technical specifications & setup')}</summary>
            <dl className="paquete__ficha-lista">
              {paquete.potenciaSonido && (
                <div className="paquete__ficha-item">
                  <dt>{t('Potencia acústica', 'Sound power')}</dt>
                  <dd>{paquete.potenciaSonido}</dd>
                </div>
              )}
              {paquete.coberturaAforo && (
                <div className="paquete__ficha-item">
                  <dt>{t('Cobertura sugerida', 'Suggested capacity')}</dt>
                  <dd>{paquete.coberturaAforo}</dd>
                </div>
              )}
              {paquete.duracionMontaje && (
                <div className="paquete__ficha-item">
                  <dt>{t('Montaje y prueba', 'Setup & soundcheck')}</dt>
                  <dd>{paquete.duracionMontaje}</dd>
                </div>
              )}
              {paquete.requerimientosElectricos && (
                <div className="paquete__ficha-item">
                  <dt>{t('Energía requerida', 'Power requirements')}</dt>
                  <dd>{paquete.requerimientosElectricos}</dd>
                </div>
              )}
              {paquete.espacioMinimo && (
                <div className="paquete__ficha-item">
                  <dt>{t('Espacio mínimo', 'Minimum space')}</dt>
                  <dd>{paquete.espacioMinimo}</dd>
                </div>
              )}
              {paquete.equipos.some((e) => e.especificacionTecnica || e.especificacionTecnicaEn) && (
                <div className="paquete__ficha-item">
                  <dt>{t('Detalle técnico de equipos', 'Equipment technical details')}</dt>
                  <dd>
                    {paquete.equipos
                      .filter((e) => e.especificacionTecnica || e.especificacionTecnicaEn)
                      .map((e) => {
                        const eqNom = idioma === 'en' && e.equipoNombreEn ? e.equipoNombreEn : e.equipoNombre;
                        const eqSp =
                          idioma === 'en' && e.especificacionTecnicaEn
                            ? e.especificacionTecnicaEn
                            : e.especificacionTecnica;
                        return `${e.cantidad}× ${eqNom} (${eqSp})`;
                      })
                      .join('; ')}
                  </dd>
                </div>
              )}
            </dl>
            {fichaTecnica && <p className="paquete__ficha-nota">{fichaTecnica}</p>}
          </details>
        )}

        <div className="paquete__pie">
          <p className="precio">
            <span>{promocion ? t('Precio de temporada', 'Seasonal price') : t('Desde', 'From')}</span>
            {promocion && <span className="precio__anterior">{formatoColones(paquete.precioBase)}</span>}
            <strong>
              {formatoColones(promocion ? promocion.precioPromocional : paquete.precioBase)}
            </strong>
          </p>
          <Link
            to={rutaSolicitar}
            className="boton boton--accion boton--chico"
            aria-label={t(`Cotizar ${nombre}`, `Quote ${nombre}`)}
          >
            {t('Cotizar', 'Get quote')}
          </Link>
        </div>
      </div>
    </article>
  );
}
