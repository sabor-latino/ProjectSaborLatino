import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/cliente';
import type { Evento, Solicitud } from '../api/tipos';
import { useAuth } from '../auth/contexto';
import { Cargando, Mensaje } from '../componentes/Estado';
import { FichaEvento } from '../componentes/panel/FichaEvento';
import { Dialogo, Insignia } from '../componentes/panel/Piezas';
import { useDatos } from '../hooks/useDatos';
import { usePreferencias } from '../preferencias/PreferenciasContext';
import { siguienteEstadoEvento, nombreEstado } from '../utilidades/estados';
import { formatoColones, formatoFecha, formatoFechaHora } from '../utilidades/formato';
import { CambiarEstado } from './panel/Eventos';

export function MiCuenta() {
  const { perfil, tieneRol } = useAuth();
  const { t } = usePreferencias();

  if (!perfil) {
    return null;
  }

  const esAdmin = tieneRol('Administrador', 'Superadministrador');

  return (
    <section className="seccion contenedor">
      <div className="encabezado">
        <p className="antetitulo">{t('Su cuenta', 'Your account')}</p>
        <h1 className="titulo-seccion">
          {t('Hola', 'Hello')}, {perfil.nombreCompleto.split(' ')[0]}
        </h1>
      </div>

      <div className="cuenta">
        <aside className="perfil">
          <div className="perfil__avatar" aria-hidden="true">
            {perfil.nombreCompleto.charAt(0).toUpperCase()}
          </div>
          <dl className="perfil__datos">
            <dt>{t('Nombre', 'Name')}</dt>
            <dd>{perfil.nombreCompleto}</dd>
            <dt>{t('Correo', 'Email')}</dt>
            <dd>{perfil.email}</dd>
            <dt>{t('Teléfono', 'Phone')}</dt>
            <dd>{perfil.telefono ?? t('Sin registrar', 'Not registered')}</dd>
            <dt>{t('Rol', 'Role')}</dt>
            <dd>{perfil.roles.join(', ')}</dd>
            <dt>{t('Miembro desde', 'Member since')}</dt>
            <dd>{formatoFecha(perfil.fechaRegistro)}</dd>
          </dl>
        </aside>

        <div className="cuenta__contenido">
          {esAdmin && (
            <div className="caja caja--destacada">
              <h2 className="subtitulo">{t('Panel de administración', 'Administration dashboard')}</h2>
              <p>
                {t(
                  'Gestione la página de inicio, promociones de temporada, solicitudes, cotizaciones, eventos, catálogo y usuarios desde un solo lugar.',
                  'Manage the home page, seasonal promotions, quote requests, events, catalog, and users from one place.',
                )}
              </p>
              <Link to="/panel" className="boton boton--accion boton--chico">
                {t('Ir al panel', 'Go to dashboard')}
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
  const { t } = usePreferencias();
  const solicitudes = useDatos<Solicitud[]>('/api/solicitudes/mias');
  const eventos = useDatos<Evento[]>('/api/mis-eventos');
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const finalizados = eventos.datos?.filter((e) => e.estado === 'Finalizado').length ?? 0;

  const rechazar = async (id: number) => {
    if (!window.confirm(t('¿Rechazar esta cotización?', 'Decline this quote?'))) return;
    try {
      await api.patch(`/api/cotizaciones/${id}/rechazar`);
      setAviso({
        tipo: 'exito',
        texto: t(
          'Cotización rechazada. Si desea una propuesta ajustada, escríbanos.',
          'Quote declined. If you would like an adjusted proposal, please contact us.',
        ),
      });
      solicitudes.recargar();
    } catch (e) {
      setAviso({
        tipo: 'error',
        texto: e instanceof Error ? e.message : t('No se pudo rechazar.', 'Could not decline the quote.'),
      });
    }
  };

  return (
    <>
      <div className="caja caja--fidelidad">
        <h2 className="subtitulo">{t('Programa de cliente frecuente', 'Frequent client program')}</h2>
        <p>
          {t('Acumula', 'You have completed')} <strong className="numero">{finalizados}</strong>{' '}
          {finalizados === 1
            ? t('evento finalizado con nosotros.', 'completed event with us.')
            : t('eventos finalizados con nosotros.', 'completed events with us.')}{' '}
          {t(
            'Nuestros clientes frecuentes reciben descuentos preferenciales en sus próximas cotizaciones.',
            'Our frequent clients receive preferential discounts on future quotes.',
          )}
        </p>
      </div>

      <div className="caja">
        <div className="panel__titulo">
          <h2 className="subtitulo">{t('Mis solicitudes', 'My requests')}</h2>
          <Link to="/solicitar" className="boton boton--accion boton--chico">
            {t('Nueva solicitud', 'New request')}
          </Link>
        </div>
        {aviso && <Mensaje tipo={aviso.tipo}>{aviso.texto}</Mensaje>}
        {solicitudes.cargando && <Cargando />}
        {solicitudes.error && <Mensaje tipo="error">{solicitudes.error}</Mensaje>}
        {solicitudes.datos?.length === 0 && (
          <p className="texto-suave">{t('Todavía no ha solicitado ninguna cotización.', 'You have not requested any quotes yet.')}</p>
        )}
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
              {s.paqueteNombre && (
                <p className="texto-suave">
                  {t('Paquete:', 'Package:')} {s.paqueteNombre}
                </p>
              )}
              {(s.cotizaciones?.length ?? 0) === 0 && s.estado === 'Pendiente' && (
                <p className="texto-suave">{t('Estamos preparando su cotización.', 'We are preparing your quote.')}</p>
              )}
              {s.cotizaciones?.map((c) => (
                <div key={c.id} className="cotizacion">
                  <div>
                    <span className="cotizacion__total numero">{formatoColones(c.total)}</span>
                    {c.descuento > 0 && (
                      <span className="texto-suave">
                        {' '}
                        ({t('incluye', 'includes')} {formatoColones(c.descuento)} {t('de descuento', 'discount')})
                      </span>
                    )}
                    <p className="texto-suave">
                      {t('Vigente hasta', 'Valid until')} {formatoFecha(c.vigenteHasta)}
                      {c.detalle && ` · ${c.detalle}`}
                    </p>
                  </div>
                  <div className="acciones acciones--chicas">
                    <Insignia estado={c.estado} />
                    {c.estado === 'Enviada' && (
                      <button type="button" className="boton boton--texto boton--mini" onClick={() => rechazar(c.id)}>
                        {t('Rechazar', 'Decline')}
                      </button>
                    )}
                  </div>
                  {c.estado === 'Enviada' && (
                    <p className="cotizacion__nota">
                      {t(
                        '¿Desea confirmar esta propuesta? Responda a nuestro correo o contáctenos por teléfono para reservar su fecha.',
                        'Would you like to confirm this proposal? Reply to our email or call us to lock in your date.',
                      )}
                    </p>
                  )}
                </div>
              ))}
            </li>
          ))}
        </ul>
      </div>

      <div className="caja">
        <h2 className="subtitulo">{t('Mis eventos', 'My events')}</h2>
        {eventos.cargando && <Cargando />}
        {eventos.error && <Mensaje tipo="error">{eventos.error}</Mensaje>}
        {eventos.datos?.length === 0 && (
          <p className="texto-suave">
            {t('Cuando confirme una cotización, su evento aparecerá aquí.', 'Once you confirm a quote, your event will appear here.')}
          </p>
        )}
        <ul className="lista-tarjetas">
          {eventos.datos?.map((ev) => (
            <li key={ev.id} className="tarjeta-panel tarjeta-panel__cuerpo">
              <div className="panel__titulo">
                <strong>{ev.paqueteNombre ?? t('Su evento', 'Your event')}</strong>
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
  const { t } = usePreferencias();
  const { datos, cargando, error, recargar } = useDatos<Evento[]>('/api/mis-eventos');
  const [cambiando, setCambiando] = useState<Evento | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const proximos = datos?.filter((e) => e.estado !== 'Finalizado' && e.estado !== 'Cancelado') ?? [];
  const pasados = datos?.filter((e) => e.estado === 'Finalizado' || e.estado === 'Cancelado') ?? [];

  return (
    <div className="caja">
      <h2 className="subtitulo">{t('Mis eventos asignados', 'My assigned events')}</h2>
      {aviso && <Mensaje tipo="exito">{aviso}</Mensaje>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {datos?.length === 0 && (
        <p className="texto-suave">
          {t(
            'No tiene eventos asignados. Si debería tenerlos, solicite al administrador que vincule su cuenta en Personal.',
            'You have no assigned events. If you should have them, ask an administrator to link your account in Staff.',
          )}
        </p>
      )}
      <ul className="lista-tarjetas">
        {proximos.map((ev) => {
          const siguiente = siguienteEstadoEvento(ev.estado);
          return (
            <li key={ev.id} className="tarjeta-panel tarjeta-panel__cuerpo">
              <div className="panel__titulo">
                <strong>
                  #{ev.id} · {ev.paqueteNombre ?? t('Evento', 'Event')}
                </strong>
                <Insignia estado={ev.estado} />
              </div>
              <FichaEvento evento={ev} />
              {siguiente && (
                <button type="button" className="boton boton--accion boton--chico" onClick={() => setCambiando(ev)}>
                  {t('Pasar a', 'Move to')} {nombreEstado(siguiente)}
                </button>
              )}
            </li>
          );
        })}
      </ul>
      {pasados.length > 0 && (
        <details className="pasados">
          <summary>
            {t('Eventos anteriores', 'Past events')} ({pasados.length})
          </summary>
          <ul className="lista-simple">
            {pasados.map((ev) => (
              <li key={ev.id}>
                <span>
                  #{ev.id} · {ev.paqueteNombre ?? t('Evento', 'Event')} · {formatoFechaHora(ev.fechaInicio)}
                </span>
                <Insignia estado={ev.estado} />
              </li>
            ))}
          </ul>
        </details>
      )}

      <Dialogo titulo={t('Cambiar estado', 'Change status')} abierto={cambiando !== null} alCerrar={() => setCambiando(null)}>
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
