import { Link } from 'react-router-dom';
import type { PreguntaFrecuente } from '../api/tipos';
import { Cargando, Mensaje } from '../componentes/Estado';
import { useDatos } from '../hooks/useDatos';
import { usePreferencias } from '../preferencias/PreferenciasContext';

export function PreguntasFrecuentes() {
  const { idioma, t } = usePreferencias();
  const { datos, cargando, error } = useDatos<PreguntaFrecuente[]>('/api/preguntas-frecuentes');

  return (
    <section className="seccion contenedor contenedor--angosto">
      <div className="encabezado">
        <p className="antetitulo">{t('Preguntas frecuentes', 'Frequently asked questions')}</p>
        <h1 className="titulo-seccion">{t('Todo lo que debe saber antes de cotizar', 'Everything to know before requesting a quote')}</h1>
        <p className="intro">
          {t(
            'Resolvemos las consultas más habituales sobre horarios de montaje, cobertura geográfica, requerimientos eléctricos y personalización de repertorio.',
            'Find answers to common questions about setup times, geographic coverage, power requirements, and playlist customization.',
          )}
        </p>
      </div>

      {cargando && <Cargando texto={t('Cargando preguntas…', 'Loading questions…')} />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {datos && datos.length === 0 && (
        <Mensaje>
          {t('Todavía no hay preguntas publicadas. Si tiene alguna consulta,', 'There are no published questions yet. If you have any questions,')}{' '}
          <Link to="/solicitar" className="enlace">
            {t('escríbala en el formulario de cotización', 'include it in the quote request form')}
          </Link>{' '}
          {t('y le responderemos junto con su propuesta.', 'and we will answer it along with your proposal.')}
        </Mensaje>
      )}
      {datos && datos.length > 0 && (
        <div className="preguntas">
          {datos.map((p) => {
            const pregunta = idioma === 'en' && p.preguntaEn ? p.preguntaEn : p.pregunta;
            const respuesta = idioma === 'en' && p.respuestaEn ? p.respuestaEn : p.respuesta;
            return (
              <details key={p.id} className="pregunta">
                <summary>{pregunta}</summary>
                <p>{respuesta}</p>
              </details>
            );
          })}
        </div>
      )}
    </section>
  );
}
