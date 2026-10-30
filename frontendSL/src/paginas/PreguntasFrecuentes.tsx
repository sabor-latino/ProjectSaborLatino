import { Link } from 'react-router-dom';
import type { PreguntaFrecuente } from '../api/tipos';
import { Cargando, Mensaje } from '../componentes/Estado';
import { useDatos } from '../hooks/useDatos';

export function PreguntasFrecuentes() {
  const { datos, cargando, error } = useDatos<PreguntaFrecuente[]>('/api/preguntas-frecuentes');

  return (
    <section className="seccion contenedor contenedor--angosto">
      <div className="encabezado">
        <p className="antetitulo">Preguntas frecuentes</p>
        <h1 className="titulo-seccion">Antes de cotizar</h1>
      </div>

      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {datos && datos.length === 0 && (
        <Mensaje>
          Todavía no hay preguntas publicadas. Si tiene una duda,{' '}
          <Link to="/solicitar" className="enlace">
            escríbala en el formulario de cotización
          </Link>{' '}
          y se la respondemos con su cotización.
        </Mensaje>
      )}
      {datos && datos.length > 0 && (
        <div className="preguntas">
          {datos.map((p) => (
            // <details> abre y cierra la respuesta sin JavaScript
            <details key={p.id} className="pregunta">
              <summary>{p.pregunta}</summary>
              <p>{p.respuesta}</p>
            </details>
          ))}
        </div>
      )}
    </section>
  );
}
