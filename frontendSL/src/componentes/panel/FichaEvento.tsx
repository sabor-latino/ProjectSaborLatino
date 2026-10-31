import type { Evento } from '../../api/tipos';
import { nombreEstado } from '../../utilidades/estados';
import { formatoFechaHora } from '../../utilidades/formato';

// Detalle de un evento: fechas, equipo, personal e historial de estados
export function FichaEvento({ evento, conHistorial = true }: { evento: Evento; conHistorial?: boolean }) {
  return (
    <div className="ficha-evento">
      <dl className="ficha">
        <dt>Cliente</dt>
        <dd>{evento.nombreContacto}</dd>
        <dt>Paquete</dt>
        <dd>{evento.paqueteNombre ?? 'A la medida'}</dd>
        <dt>Inicio</dt>
        <dd>{formatoFechaHora(evento.fechaInicio)}</dd>
        <dt>Fin</dt>
        <dd>{formatoFechaHora(evento.fechaFin)}</dd>
        <dt>Lugar</dt>
        <dd>{evento.lugar}</dd>
        {evento.notas && (
          <>
            <dt>Notas</dt>
            <dd>{evento.notas}</dd>
          </>
        )}
        {evento.motivoCancelacion && (
          <>
            <dt>Motivo de cancelación</dt>
            <dd>{evento.motivoCancelacion}</dd>
          </>
        )}
      </dl>

      <div className="ficha-evento__columnas">
        <div>
          <h3 className="subtitulo">Equipo</h3>
          {evento.equipos.length === 0 ? (
            <p className="texto-suave">Sin equipo asignado.</p>
          ) : (
            <ul className="etiquetas">
              {evento.equipos.map((e) => (
                <li key={e.equipoId}>
                  {e.cantidad} × {e.equipoNombre}
                </li>
              ))}
            </ul>
          )}
        </div>
        <div>
          <h3 className="subtitulo">Personal</h3>
          {evento.trabajadores.length === 0 ? (
            <p className="texto-suave">Sin personal asignado.</p>
          ) : (
            <ul className="lista-simple">
              {evento.trabajadores.map((t) => (
                <li key={t.empleadoId}>
                  <span>
                    <strong>{t.empleadoNombre}</strong>
                    <span className="texto-suave"> · {t.funcion ?? t.puesto}</span>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {conHistorial && evento.historial.length > 0 && (
        <>
          <h3 className="subtitulo">Historial</h3>
          <ol className="linea-tiempo">
            {evento.historial.map((h, i) => (
              <li key={i}>
                <strong>{nombreEstado(h.estadoNuevo)}</strong>
                <span className="texto-suave">
                  {' '}
                  · {formatoFechaHora(h.fecha)}
                  {h.comentario && ` · ${h.comentario}`}
                </span>
              </li>
            ))}
          </ol>
        </>
      )}
    </div>
  );
}
