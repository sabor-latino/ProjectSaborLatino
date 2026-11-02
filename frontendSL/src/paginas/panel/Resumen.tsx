import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { Evento, Resumen as DatosResumen } from '../../api/tipos';
import { useAuth } from '../../auth/contexto';
import { Cargando, Mensaje } from '../../componentes/Estado';
import { FichaEvento } from '../../componentes/panel/FichaEvento';
import { Cifra, Dialogo, Insignia, TituloPanel } from '../../componentes/panel/Piezas';
import { useDatos } from '../../hooks/useDatos';
import { nombreEstado, siguienteEstadoEvento } from '../../utilidades/estados';
import { formatoColones, formatoFechaHora } from '../../utilidades/formato';
import { CambiarEstado } from './Eventos';

const MESES = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];

export function Resumen() {
  const { tieneRol } = useAuth();
  const esTrabajador = tieneRol('Trabajador') && !tieneRol('Administrador', 'Superadministrador');

  if (esTrabajador) {
    return <EventosTrabajador />;
  }

  return <ResumenAdmin />;
}

// Vista exclusiva para empleados / técnicos
function EventosTrabajador() {
  const { datos, cargando, error, recargar } = useDatos<Evento[]>('/api/mis-eventos');
  const [cambiando, setCambiando] = useState<Evento | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  const proximos = datos?.filter((e) => e.estado !== 'Finalizado' && e.estado !== 'Cancelado') ?? [];
  const pasados = datos?.filter((e) => e.estado === 'Finalizado' || e.estado === 'Cancelado') ?? [];

  return (
    <section>
      <TituloPanel titulo="Mis Eventos Asignados">
        <Link to="/panel/perfil" className="boton boton--borde boton--chico">
          Editar mi perfil
        </Link>
      </TituloPanel>

      {aviso && <Mensaje tipo="exito">{aviso}</Mensaje>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}

      {datos && datos.length === 0 && (
        <Mensaje>
          No tiene eventos asignados por ahora. Cuando la administración lo vincule a un evento, aparecerá aquí.
        </Mensaje>
      )}

      {proximos.length > 0 && (
        <div className="lista-tarjetas" style={{ marginBottom: 'var(--esp-5)' }}>
          {proximos.map((ev) => {
            const siguiente = siguienteEstadoEvento(ev.estado);
            return (
              <article key={ev.id} className="tarjeta-panel tarjeta-panel__cuerpo">
                <div className="panel__titulo">
                  <strong>
                    #{ev.id} · {ev.paqueteNombre ?? 'Evento a la medida'}
                  </strong>
                  <Insignia estado={ev.estado} />
                </div>
                <FichaEvento evento={ev} />
                {siguiente && (
                  <div className="acciones" style={{ marginTop: 'var(--esp-3)' }}>
                    <button
                      type="button"
                      className="boton boton--accion boton--chico"
                      onClick={() => setCambiando(ev)}
                    >
                      Avanzar a {nombreEstado(siguiente)}
                    </button>
                  </div>
                )}
              </article>
            );
          })}
        </div>
      )}

      {pasados.length > 0 && (
        <details className="pasados" style={{ marginTop: 'var(--esp-4)' }}>
          <summary>
            Historial de eventos anteriores ({pasados.length})
          </summary>
          <ul className="lista-simple" style={{ marginTop: 'var(--esp-2)' }}>
            {pasados.map((ev) => (
              <li key={ev.id}>
                <span>
                  #{ev.id} · {ev.paqueteNombre ?? 'Evento'} · {formatoFechaHora(ev.fechaInicio)}
                </span>
                <Insignia estado={ev.estado} />
              </li>
            ))}
          </ul>
        </details>
      )}

      <Dialogo titulo="Cambiar estado del evento" abierto={cambiando !== null} alCerrar={() => setCambiando(null)}>
        {cambiando && (
          <CambiarEstado
            evento={cambiando}
            alTerminar={(msg) => {
              setCambiando(null);
              setAviso(msg);
              recargar();
            }}
          />
        )}
      </Dialogo>
    </section>
  );
}

// Portada del panel: cómo va el mes (Administrador y Superadministrador)
function ResumenAdmin() {
  const hoy = new Date();
  const [mes, setMes] = useState(`${hoy.getFullYear()}-${String(hoy.getMonth() + 1).padStart(2, '0')}`);
  const [anio, numeroMes] = mes.split('-').map(Number);
  const { datos, cargando, error } = useDatos<DatosResumen>(`/api/reportes/resumen?anio=${anio}&mes=${numeroMes}`);

  const totalEventos = datos?.eventosPorEstado.reduce((s, e) => s + e.cantidad, 0) ?? 0;
  const maxPaquete = Math.max(1, ...(datos?.topPaquetes.map((p) => p.cantidad) ?? [1]));

  return (
    <section>
      <TituloPanel titulo="Resumen">
        <label className="filtro">
          <span>Mes</span>
          <input type="month" name="mes" value={mes} onChange={(e) => e.target.value && setMes(e.target.value)} />
        </label>
      </TituloPanel>

      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}

      {datos && (
        <>
          <p className="texto-suave">
            {MESES[datos.mes - 1]} de {datos.anio}
          </p>
          <div className="cifras">
            <Cifra etiqueta="Ingresos" valor={formatoColones(datos.ingresos)} nota="Eventos finalizados del mes" />
            <Cifra etiqueta="Eventos" valor={totalEventos} nota="Que empiezan este mes" />
            <Cifra
              etiqueta="Solicitudes pendientes"
              valor={datos.solicitudesPendientes}
              nota={datos.solicitudesPendientes > 0 ? 'Esperan cotización' : 'Todo al día'}
            />
          </div>

          {datos.solicitudesPendientes > 0 && (
            <p>
              <Link to="/panel/solicitudes" className="enlace">
                Ver las solicitudes pendientes →
              </Link>
            </p>
          )}

          <div className="rejilla-panel">
            <div className="caja">
              <h2 className="subtitulo">Eventos por estado</h2>
              {datos.eventosPorEstado.length === 0 ? (
                <p className="texto-suave">No hay eventos este mes.</p>
              ) : (
                <ul className="lista-simple">
                  {datos.eventosPorEstado.map((e) => (
                    <li key={e.nombre}>
                      <span>{nombreEstado(e.nombre)}</span>
                      <strong className="numero">{e.cantidad}</strong>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="caja">
              <h2 className="subtitulo">Paquetes más contratados</h2>
              {datos.topPaquetes.length === 0 ? (
                <p className="texto-suave">Sin contrataciones este mes.</p>
              ) : (
                <ul className="barras">
                  {datos.topPaquetes.map((p) => (
                    <li key={p.nombre}>
                      <span className="barras__nombre">{p.nombre}</span>
                      <span className="barras__pista" aria-hidden="true">
                        <span className="barras__relleno" style={{ width: `${(p.cantidad / maxPaquete) * 100}%` }} />
                      </span>
                      <strong className="numero">{p.cantidad}</strong>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="caja">
              <h2 className="subtitulo">Clientes frecuentes</h2>
              {datos.topClientes.length === 0 ? (
                <p className="texto-suave">Todavía no hay clientes con eventos finalizados.</p>
              ) : (
                <ul className="lista-simple">
                  {datos.topClientes.map((c) => (
                    <li key={c.clienteUsuarioId}>
                      <span>
                        <strong>{c.nombreCompleto}</strong>
                        <span className="texto-suave"> · {c.email}</span>
                      </span>
                      <strong className="numero">{c.eventosFinalizados}</strong>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </>
      )}
    </section>
  );
}
