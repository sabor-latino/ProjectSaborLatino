import { useState } from 'react';
import { api } from '../../api/cliente';
import type { Disponibilidad, Empleado, Evento } from '../../api/tipos';
import { Cargando, Mensaje } from '../../componentes/Estado';
import { FichaEvento } from '../../componentes/panel/FichaEvento';
import { Dialogo, FormularioAccion, Insignia, TituloPanel } from '../../componentes/panel/Piezas';
import { useDatos } from '../../hooks/useDatos';
import { ESTADOS_EVENTO, nombreEstado, siguienteEstadoEvento } from '../../utilidades/estados';
import { formatoFechaHora } from '../../utilidades/formato';

type Accion = { tipo: 'equipo' | 'personal' | 'estado' | 'cancelar'; evento: Evento } | null;

// Agenda de eventos: el admin asigna equipo y personal y avanza el estado
export function Eventos() {
  const [estado, setEstado] = useState('');
  const { datos, cargando, error, recargar } = useDatos<Evento[]>(estado ? `/api/eventos?estado=${estado}` : '/api/eventos');
  const [abierto, setAbierto] = useState<number | null>(null);
  const [accion, setAccion] = useState<Accion>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  const terminar = (texto: string) => {
    setAccion(null);
    setAviso(texto);
    recargar();
  };

  return (
    <section>
      <TituloPanel titulo="Eventos" />

      <div className="filtros" role="group" aria-label="Filtrar por estado">
        {['', ...ESTADOS_EVENTO].map((e) => (
          <button key={e || 'todos'} type="button" className="chip" aria-pressed={estado === e} onClick={() => setEstado(e)}>
            {e ? nombreEstado(e) : 'Todos'}
          </button>
        ))}
      </div>

      {aviso && <Mensaje tipo="exito">{aviso}</Mensaje>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {datos && datos.length === 0 && (
        <Mensaje>No hay eventos. Se crean desde Solicitudes, cuando una cotización está aceptada.</Mensaje>
      )}

      <div className="lista-tarjetas">
        {datos?.map((ev) => {
          const siguiente = siguienteEstadoEvento(ev.estado);
          const activo = ev.estado !== 'Cancelado' && ev.estado !== 'Finalizado';
          return (
            <article key={ev.id} className="tarjeta-panel">
              <button
                type="button"
                className="tarjeta-panel__cabeza"
                aria-expanded={abierto === ev.id}
                onClick={() => setAbierto(abierto === ev.id ? null : ev.id)}
              >
                <span>
                  <strong>
                    #{ev.id} · {ev.paqueteNombre ?? 'Evento a la medida'}
                  </strong>
                  <span className="texto-suave">
                    {ev.nombreContacto} · {formatoFechaHora(ev.fechaInicio)} · {ev.lugar}
                  </span>
                </span>
                <Insignia estado={ev.estado} />
              </button>

              {abierto === ev.id && (
                <div className="tarjeta-panel__cuerpo">
                  <FichaEvento evento={ev} />
                  {activo && (
                    <div className="acciones acciones--chicas">
                      {siguiente && (
                        <button
                          type="button"
                          className="boton boton--accion boton--chico"
                          onClick={() => setAccion({ tipo: 'estado', evento: ev })}
                        >
                          Pasar a {nombreEstado(siguiente)}
                        </button>
                      )}
                      <button type="button" className="boton boton--borde boton--chico" onClick={() => setAccion({ tipo: 'equipo', evento: ev })}>
                        Asignar equipo
                      </button>
                      <button type="button" className="boton boton--borde boton--chico" onClick={() => setAccion({ tipo: 'personal', evento: ev })}>
                        Asignar personal
                      </button>
                      <button type="button" className="boton boton--texto boton--chico" onClick={() => setAccion({ tipo: 'cancelar', evento: ev })}>
                        Cancelar evento
                      </button>
                    </div>
                  )}
                </div>
              )}
            </article>
          );
        })}
      </div>

      <Dialogo titulo="Asignar equipo" abierto={accion?.tipo === 'equipo'} alCerrar={() => setAccion(null)}>
        {accion?.tipo === 'equipo' && <AsignarEquipo evento={accion.evento} alTerminar={() => terminar('Equipo asignado.')} />}
      </Dialogo>
      <Dialogo titulo="Asignar personal" abierto={accion?.tipo === 'personal'} alCerrar={() => setAccion(null)}>
        {accion?.tipo === 'personal' && <AsignarPersonal evento={accion.evento} alTerminar={() => terminar('Personal asignado.')} />}
      </Dialogo>
      <Dialogo titulo="Cambiar estado" abierto={accion?.tipo === 'estado'} alCerrar={() => setAccion(null)}>
        {accion?.tipo === 'estado' && <CambiarEstado evento={accion.evento} alTerminar={(t) => terminar(t)} />}
      </Dialogo>
      <Dialogo titulo="Cancelar evento" abierto={accion?.tipo === 'cancelar'} alCerrar={() => setAccion(null)}>
        {accion?.tipo === 'cancelar' && <CancelarEvento evento={accion.evento} alTerminar={() => terminar('Evento cancelado.')} />}
      </Dialogo>
    </section>
  );
}

// También lo usa el panel del trabajador
export function CambiarEstado({ evento, alTerminar }: { evento: Evento; alTerminar: (texto: string) => void }) {
  const siguiente = siguienteEstadoEvento(evento.estado);
  const [comentario, setComentario] = useState('');
  if (!siguiente) return null;

  return (
    <FormularioAccion
      textoBoton={`Pasar a ${nombreEstado(siguiente)}`}
      alEnviar={async () => {
        await api.patch(`/api/eventos/${evento.id}/estado`, { estado: siguiente, comentario: comentario.trim() || null });
        alTerminar(`El evento pasó a ${nombreEstado(siguiente)}.`);
      }}
    >
      <p>
        De <strong>{nombreEstado(evento.estado)}</strong> a <strong>{nombreEstado(siguiente)}</strong>.
      </p>
      <div className="campo">
        <label htmlFor="estado-comentario">Comentario</label>
        <textarea
          id="estado-comentario"
          name="comentario"
          maxLength={500}
          placeholder="Por ejemplo: equipo instalado y probado…"
          value={comentario}
          onChange={(e) => setComentario(e.target.value)}
        />
      </div>
    </FormularioAccion>
  );
}

function CancelarEvento({ evento, alTerminar }: { evento: Evento; alTerminar: () => void }) {
  const [motivo, setMotivo] = useState('');
  return (
    <FormularioAccion
      textoBoton="Cancelar evento"
      peligro
      alEnviar={async () => {
        await api.patch(`/api/eventos/${evento.id}/cancelar`, { motivo: motivo.trim() });
        alTerminar();
      }}
    >
      <p>El equipo reservado queda libre para otros eventos. Esto no se puede deshacer.</p>
      <div className="campo">
        <label htmlFor="cancelar-motivo">Motivo *</label>
        <textarea id="cancelar-motivo" name="motivo" required maxLength={500} value={motivo} onChange={(e) => setMotivo(e.target.value)} />
      </div>
    </FormularioAccion>
  );
}

function AsignarEquipo({ evento, alTerminar }: { evento: Evento; alTerminar: () => void }) {
  const { datos, cargando, error } = useDatos<Disponibilidad[]>(
    `/api/equipos/disponibilidad?desde=${encodeURIComponent(evento.fechaInicio)}&hasta=${encodeURIComponent(evento.fechaFin)}`,
  );
  const [cantidades, setCantidades] = useState<Record<number, number>>(() =>
    Object.fromEntries(evento.equipos.map((e) => [e.equipoId, e.cantidad])),
  );

  if (cargando) return <Cargando />;
  if (error) return <Mensaje tipo="error">{error}</Mensaje>;

  return (
    <FormularioAccion
      textoBoton="Guardar equipo"
      alEnviar={async () => {
        const lista = Object.entries(cantidades)
          .filter(([, c]) => c > 0)
          .map(([id, c]) => ({ equipoId: Number(id), cantidad: c }));
        await api.put(`/api/eventos/${evento.id}/equipos`, lista);
        alTerminar();
      }}
    >
      <p className="texto-suave">Disponible para {formatoFechaHora(evento.fechaInicio)}, contando lo que ya tiene este evento.</p>
      <div className="tabla-envoltura">
        <table className="tabla tabla--compacta">
          <thead>
            <tr>
              <th scope="col">Equipo</th>
              <th scope="col">Disponible</th>
              <th scope="col">Asignar</th>
            </tr>
          </thead>
          <tbody>
            {datos?.map((d) => {
              const propio = evento.equipos.find((e) => e.equipoId === d.equipoId)?.cantidad ?? 0;
              const maximo = Math.min(d.cantidadTotal, d.disponible + propio);
              return (
                <tr key={d.equipoId}>
                  <td data-etiqueta="Equipo">{d.nombre}</td>
                  <td data-etiqueta="Disponible" className="numero">
                    {maximo} de {d.cantidadTotal}
                  </td>
                  <td data-etiqueta="Asignar">
                    <input
                      aria-label={`Cantidad de ${d.nombre}`}
                      name={`equipo-${d.equipoId}`}
                      type="number"
                      min={0}
                      max={maximo}
                      className="entrada-corta"
                      value={cantidades[d.equipoId] ?? 0}
                      onChange={(e) => setCantidades((c) => ({ ...c, [d.equipoId]: Number(e.target.value) }))}
                    />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </FormularioAccion>
  );
}

function AsignarPersonal({ evento, alTerminar }: { evento: Evento; alTerminar: () => void }) {
  const { datos, cargando, error } = useDatos<Empleado[]>('/api/empleados');
  const [elegidos, setElegidos] = useState<Record<number, string>>(() =>
    Object.fromEntries(evento.trabajadores.map((t) => [t.empleadoId, t.funcion ?? ''])),
  );

  if (cargando) return <Cargando />;
  if (error) return <Mensaje tipo="error">{error}</Mensaje>;
  if (datos?.length === 0) return <Mensaje>No hay personal registrado. Agréguelo en Catálogo → Personal.</Mensaje>;

  return (
    <FormularioAccion
      textoBoton="Guardar personal"
      alEnviar={async () => {
        const lista = Object.entries(elegidos).map(([id, funcion]) => ({
          empleadoId: Number(id),
          funcion: funcion.trim() || null,
        }));
        await api.put(`/api/eventos/${evento.id}/trabajadores`, lista);
        alTerminar();
      }}
    >
      <ul className="lista-simple">
        {datos?.map((emp) => {
          const marcado = emp.id in elegidos;
          return (
            <li key={emp.id}>
              <label className="casilla" htmlFor={`emp-${emp.id}`}>
                <input
                  id={`emp-${emp.id}`}
                  name={`empleado-${emp.id}`}
                  type="checkbox"
                  checked={marcado}
                  onChange={(e) =>
                    setElegidos((actual) => {
                      const copia = { ...actual };
                      if (e.target.checked) copia[emp.id] = '';
                      else delete copia[emp.id];
                      return copia;
                    })
                  }
                />
                <span>
                  <strong>{emp.nombreCompleto}</strong> <span className="texto-suave">· {emp.puesto}</span>
                </span>
              </label>
              {marcado && (
                <input
                  aria-label={`Función de ${emp.nombreCompleto}`}
                  name={`funcion-${emp.id}`}
                  placeholder="Función en este evento…"
                  maxLength={100}
                  autoComplete="off"
                  className="entrada-media"
                  value={elegidos[emp.id]}
                  onChange={(e) => setElegidos((actual) => ({ ...actual, [emp.id]: e.target.value }))}
                />
              )}
            </li>
          );
        })}
      </ul>
    </FormularioAccion>
  );
}
