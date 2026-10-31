import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/cliente';
import type { Evento, Solicitud } from '../api/tipos';
import { useAuth } from '../auth/contexto';
import { Cargando, Mensaje } from '../componentes/Estado';
import { FichaEvento } from '../componentes/panel/FichaEvento';
import { Dialogo, Insignia } from '../componentes/panel/Piezas';
import { useDatos } from '../hooks/useDatos';
import { siguienteEstadoEvento, nombreEstado } from '../utilidades/estados';
import { formatoColones, formatoFecha, formatoFechaHora } from '../utilidades/formato';
import { CambiarEstado } from './panel/Eventos';

// Página protegida: solo llega aquí quien inició sesión (ver RutaProtegida en App.tsx).
// Cada rol ve lo suyo: el cliente sus solicitudes y eventos, el trabajador sus eventos asignados
// y el administrador un acceso a su panel.
export function MiCuenta() {
  const { perfil, tieneRol } = useAuth();

  if (!perfil) {
    return null;
  }

  const esAdmin = tieneRol('Administrador', 'Superadministrador');

  return (
    <section className="seccion contenedor">
      <div className="encabezado">
        <p className="antetitulo">Su cuenta</p>
        <h1 className="titulo-seccion">Hola, {perfil.nombreCompleto.split(' ')[0]}</h1>
      </div>

      <div className="cuenta">
        <aside className="perfil">
          <div className="perfil__avatar" aria-hidden="true">
            {perfil.nombreCompleto.charAt(0).toUpperCase()}
          </div>
          <dl className="perfil__datos">
            <dt>Nombre</dt>
            <dd>{perfil.nombreCompleto}</dd>
            <dt>Correo</dt>
            <dd>{perfil.email}</dd>
            <dt>Teléfono</dt>
            <dd>{perfil.telefono ?? 'Sin registrar'}</dd>
            <dt>Rol</dt>
            <dd>{perfil.roles.join(', ')}</dd>
            <dt>Miembro desde</dt>
            <dd>{formatoFecha(perfil.fechaRegistro)}</dd>
          </dl>
        </aside>

        <div className="cuenta__contenido">
          {esAdmin && (
            <div className="caja caja--destacada">
              <h2 className="subtitulo">Panel de administración</h2>
              <p>Solicitudes, cotizaciones, eventos, catálogo y usuarios en un solo lugar.</p>
              <Link to="/panel" className="boton boton--accion boton--chico">
                Ir al panel
              </Link>
            </div>
          )}
          {tieneRol('Cliente') && <PanelCliente />}
          {tieneRol('Trabajador') && <PanelTrabajador />}
        </div>
      </div>
    </section>
  );
}

function PanelCliente() {
  const solicitudes = useDatos<Solicitud[]>('/api/solicitudes/mias');
  const eventos = useDatos<Evento[]>('/api/mis-eventos');
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const finalizados = eventos.datos?.filter((e) => e.estado === 'Finalizado').length ?? 0;

  const rechazar = async (id: number) => {
    if (!window.confirm('¿Rechazar esta cotización?')) return;
    try {
      await api.patch(`/api/cotizaciones/${id}/rechazar`);
      setAviso({ tipo: 'exito', texto: 'Cotización rechazada. Si quiere otra propuesta, escríbanos.' });
      solicitudes.recargar();
    } catch (e) {
      setAviso({ tipo: 'error', texto: e instanceof Error ? e.message : 'No se pudo rechazar.' });
    }
  };

  return (
    <>
      <div className="caja caja--fidelidad">
        <h2 className="subtitulo">Cliente frecuente</h2>
        <p>
          Lleva <strong className="numero">{finalizados}</strong> {finalizados === 1 ? 'evento finalizado' : 'eventos finalizados'} con
          nosotros. Los clientes frecuentes reciben descuento en sus próximas cotizaciones.
        </p>
      </div>

      <div className="caja">
        <div className="panel__titulo">
          <h2 className="subtitulo">Mis solicitudes</h2>
          <Link to="/solicitar" className="boton boton--accion boton--chico">
            Nueva solicitud
          </Link>
        </div>
        {aviso && <Mensaje tipo={aviso.tipo}>{aviso.texto}</Mensaje>}
        {solicitudes.cargando && <Cargando />}
        {solicitudes.error && <Mensaje tipo="error">{solicitudes.error}</Mensaje>}
        {solicitudes.datos?.length === 0 && <p className="texto-suave">Todavía no ha pedido ninguna cotización.</p>}
        <ul className="lista-tarjetas">
          {solicitudes.datos?.map((s) => (
            <li key={s.id} className="tarjeta-panel tarjeta-panel__cuerpo">
              <div className="panel__titulo">
                <span>
                  <strong>{s.tipoEvento}</strong>
                  <span className="texto-suave">
                    {' '}
                    · {formatoFechaHora(s.fechaEvento)} · {s.lugar}
                  </span>
                </span>
                <Insignia estado={s.estado} />
              </div>
              {s.paqueteNombre && <p className="texto-suave">Paquete: {s.paqueteNombre}</p>}
              {(s.cotizaciones?.length ?? 0) === 0 && s.estado === 'Pendiente' && (
                <p className="texto-suave">Estamos preparando su cotización.</p>
              )}
              {s.cotizaciones?.map((c) => (
                <div key={c.id} className="cotizacion">
                  <div>
                    <span className="cotizacion__total numero">{formatoColones(c.total)}</span>
                    {c.descuento > 0 && (
                      <span className="texto-suave"> (incluye {formatoColones(c.descuento)} de descuento)</span>
                    )}
                    <p className="texto-suave">
                      Vigente hasta {formatoFecha(c.vigenteHasta)}
                      {c.detalle && ` · ${c.detalle}`}
                    </p>
                  </div>
                  <div className="acciones acciones--chicas">
                    <Insignia estado={c.estado} />
                    {c.estado === 'Enviada' && (
                      <button type="button" className="boton boton--texto boton--mini" onClick={() => rechazar(c.id)}>
                        Rechazar
                      </button>
                    )}
                  </div>
                  {c.estado === 'Enviada' && (
                    <p className="cotizacion__nota">
                      ¿Le gusta? Confírmela respondiendo nuestro correo o por teléfono y apartamos su fecha.
                    </p>
                  )}
                </div>
              ))}
            </li>
          ))}
        </ul>
      </div>

      <div className="caja">
        <h2 className="subtitulo">Mis eventos</h2>
        {eventos.cargando && <Cargando />}
        {eventos.error && <Mensaje tipo="error">{eventos.error}</Mensaje>}
        {eventos.datos?.length === 0 && <p className="texto-suave">Cuando confirme una cotización, su evento aparecerá aquí.</p>}
        <ul className="lista-tarjetas">
          {eventos.datos?.map((ev) => (
            <li key={ev.id} className="tarjeta-panel tarjeta-panel__cuerpo">
              <div className="panel__titulo">
                <strong>{ev.paqueteNombre ?? 'Su evento'}</strong>
                <Insignia estado={ev.estado} />
              </div>
              <FichaEvento evento={ev} conHistorial={false} />
            </li>
          ))}
        </ul>
      </div>
    </>
  );
}

function PanelTrabajador() {
  const { datos, cargando, error, recargar } = useDatos<Evento[]>('/api/mis-eventos');
  const [cambiando, setCambiando] = useState<Evento | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const proximos = datos?.filter((e) => e.estado !== 'Finalizado' && e.estado !== 'Cancelado') ?? [];
  const pasados = datos?.filter((e) => e.estado === 'Finalizado' || e.estado === 'Cancelado') ?? [];

  return (
    <div className="caja">
      <h2 className="subtitulo">Mis eventos asignados</h2>
      {aviso && <Mensaje tipo="exito">{aviso}</Mensaje>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {datos?.length === 0 && (
        <p className="texto-suave">No tiene eventos asignados. Si debería tenerlos, pida al administrador que vincule su cuenta en Personal.</p>
      )}
      <ul className="lista-tarjetas">
        {proximos.map((ev) => {
          const siguiente = siguienteEstadoEvento(ev.estado);
          return (
            <li key={ev.id} className="tarjeta-panel tarjeta-panel__cuerpo">
              <div className="panel__titulo">
                <strong>
                  #{ev.id} · {ev.paqueteNombre ?? 'Evento'}
                </strong>
                <Insignia estado={ev.estado} />
              </div>
              <FichaEvento evento={ev} />
              {siguiente && (
                <button type="button" className="boton boton--accion boton--chico" onClick={() => setCambiando(ev)}>
                  Pasar a {nombreEstado(siguiente)}
                </button>
              )}
            </li>
          );
        })}
      </ul>
      {pasados.length > 0 && (
        <details className="pasados">
          <summary>Eventos anteriores ({pasados.length})</summary>
          <ul className="lista-simple">
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

      <Dialogo titulo="Cambiar estado" abierto={cambiando !== null} alCerrar={() => setCambiando(null)}>
        {cambiando && (
          <CambiarEstado
            evento={cambiando}
            alTerminar={(texto) => {
              setCambiando(null);
              setAviso(texto);
              recargar();
            }}
          />
        )}
      </Dialogo>
    </div>
  );
}
