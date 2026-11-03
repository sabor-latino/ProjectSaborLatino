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
        <h3>{nombre}</h3>
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
