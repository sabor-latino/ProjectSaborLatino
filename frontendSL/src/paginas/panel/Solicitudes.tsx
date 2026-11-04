import { useEffect, useRef, useState } from 'react';
import { api } from '../../api/cliente';
import type { Cotizacion, Fidelidad, Solicitud } from '../../api/tipos';
import { Cargando, Mensaje } from '../../componentes/Estado';
import { Dialogo, FormularioAccion, Insignia, TituloPanel } from '../../componentes/panel/Piezas';
import { useDatos } from '../../hooks/useDatos';
import { ESTADOS_SOLICITUD, nombreEstado } from '../../utilidades/estados';
import { ahoraParaInput, formatoColones, formatoFecha, formatoFechaHora } from '../../utilidades/formato';
import { useAuth } from '../../auth/contexto';
import { usePreferencias } from '../../preferencias/PreferenciasContext';

type Accion =
  | { tipo: 'cotizar'; solicitud: Solicitud }
  | { tipo: 'evento'; solicitud: Solicitud; cotizacion: Cotizacion }
  | null;

// Bandeja de solicitudes: aquí el admin cotiza, acepta y convierte en evento
export function Solicitudes() {
  const { tieneRol } = useAuth();
  const { t } = usePreferencias();
  const [estado, setEstado] = useState('Pendiente');
  const { datos, cargando, error, recargar } = useDatos<Solicitud[]>(
    estado ? `/api/solicitudes?estado=${estado}` : '/api/solicitudes',
  );
  const [abierta, setAbierta] = useState<number | null>(null);
  const [accion, setAccion] = useState<Accion>(null);
  const [aviso, setAviso] = useState<{ tipo: 'exito' | 'error'; texto: string } | null>(null);
  const avisoRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (aviso) avisoRef.current?.focus();
  }, [aviso]);

  const ejecutar = async (
    pregunta: string,
    llamada: () => Promise<unknown>,
    exito: string,
    recargarSiFalla = false,
  ) => {
    if (!window.confirm(pregunta)) return;
    try {
      await llamada();
      setAviso({ tipo: 'exito', texto: exito });
      recargar();
    } catch (e) {
      setAviso({ tipo: 'error', texto: e instanceof Error ? e.message : 'No se pudo completar la acción.' });
      if (recargarSiFalla) recargar();
    }
  };

  const terminar = (texto: string) => {
    setAccion(null);
    setAviso({ tipo: 'exito', texto });
    recargar();
  };

  return (
    <section>
      <TituloPanel titulo="Solicitudes" />

      <div className="filtros" role="group" aria-label="Filtrar por estado">
        {[...ESTADOS_SOLICITUD, ''].map((e) => (
          <button key={e || 'todas'} type="button" className="chip" aria-pressed={estado === e} onClick={() => setEstado(e)}>
            {e ? nombreEstado(e) : 'Todas'}
          </button>
        ))}
      </div>

      {aviso && <div ref={avisoRef} tabIndex={-1} className="panel__aviso"><Mensaje tipo={aviso.tipo}>{aviso.texto}</Mensaje></div>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {datos && datos.length === 0 && <Mensaje>No hay solicitudes en este estado.</Mensaje>}

      <div className="lista-tarjetas">
        {datos?.map((s) => {
          const aceptada = s.cotizaciones?.find((c) => c.estado === 'Aceptada');
          const cerrada = ['Aceptada', 'Rechazada', 'Cancelada'].includes(s.estado);
          return (
            <article key={s.id} className="tarjeta-panel">
              <button
                type="button"
                className="tarjeta-panel__cabeza"
                aria-expanded={abierta === s.id}
                onClick={() => setAbierta(abierta === s.id ? null : s.id)}
              >
                <span>
                  <strong>
                    #{s.id} · {s.tipoEvento}
                  </strong>
                  <span className="texto-suave">
                    {s.nombreContacto} · {formatoFechaHora(s.fechaEvento)} · {s.lugar}
                  </span>
                </span>
                <Insignia estado={s.estado} />
              </button>

              {abierta === s.id && (
                <div className="tarjeta-panel__cuerpo">
                  <dl className="ficha">
                    <dt>Contacto</dt>
                    <dd>
                      {s.nombreContacto}{s.correoContacto && <> · <a href={`mailto:${s.correoContacto}`}>{s.correoContacto}</a></>}
                      {s.telefonoContacto && ` · ${s.telefonoContacto}`}
                    </dd>
                    <dt>Paquete</dt>
                    <dd>{s.paqueteNombre ?? 'Sin paquete (solicita asesoría)'}</dd>
                    <dt>Invitados</dt>
                    <dd>{s.cantidadInvitados ?? '—'}</dd>
                    <dt>Cuenta</dt>
                    <dd>{s.clienteUsuarioId ? 'Cliente registrado' : 'Visitante sin cuenta'}</dd>
                    <dt>{t('Registrada', 'Registered')}</dt>
                    <dd>{formatoFechaHora(s.fechaCreacion)}</dd>
                    {s.comentarios && (
                      <>
                        <dt>Comentarios</dt>
                        <dd>{s.comentarios}</dd>
                      </>
                    )}
                  </dl>

                  <section className="recepcion-whatsapp" aria-labelledby={`recepcion-whatsapp-${s.id}`}>
                    <div>
                      <h2 id={`recepcion-whatsapp-${s.id}`} className="recepcion-whatsapp__titulo">{t('Recepción por WhatsApp', 'WhatsApp receipt')}</h2>
                      <p>{s.recibidoWhatsAppEn ? t('Recepción confirmada manualmente', 'Receipt manually confirmed') : t('Sin confirmación de recepción', 'Receipt not confirmed')}</p>
                      {s.recibidoWhatsAppEn && <p className="texto-suave">{formatoFechaHora(s.recibidoWhatsAppEn)}</p>}
                      {s.recibidoWhatsAppPorUsuarioId && <p className="recepcion-whatsapp__autor">{t('Registrado por', 'Recorded by')}: <span>{s.recibidoWhatsAppPorUsuarioId}</span></p>}
                    </div>
                    {!s.recibidoWhatsAppEn && tieneRol('Administrador', 'Superadministrador') && (
                      <button type="button" className="boton boton--borde boton--chico" onClick={() => ejecutar(
                        t('Confirme que recibió el mensaje de esta solicitud en WhatsApp. ¿Marcar como recibido?', 'Confirm you received this request message on WhatsApp. Mark as received?'),
                        () => api.post(`/api/solicitudes/${s.id}/whatsapp-recibido`),
                        t('Recepción de WhatsApp confirmada manualmente.', 'WhatsApp receipt manually confirmed.'),
                      )}>
                        {t('Marcar recibido por WhatsApp', 'Mark WhatsApp received')}
                      </button>
                    )}
                  </section>

                  <h2 className="subtitulo">{t('Cotizaciones', 'Quotes')}</h2>
                  {(s.cotizaciones?.length ?? 0) === 0 && <p className="texto-suave">Todavía no tiene cotizaciones.</p>}
                  <ul className="lista-simple">
                    {s.cotizaciones?.map((c) => (
                      <li key={c.id}>
                        <span>
                          <strong className="numero">{formatoColones(c.total)}</strong>
                          {c.descuento > 0 && (
                            <span className="texto-suave">
                              {' '}
                              ({formatoColones(c.monto)} − {formatoColones(c.descuento)} de descuento)
                            </span>
                          )}
                          <span className="texto-suave"> · vigente hasta {formatoFecha(c.vigenteHasta)}</span>
                          {c.detalle && <span className="texto-suave"> · {c.detalle}</span>}
                        </span>
                        <span className="acciones acciones--chicas">
                          <Insignia estado={c.estado} />
                          {c.estado === 'Enviada' && (
                            <>
                              <button
                                type="button"
                                className="boton boton--borde boton--mini"
                                onClick={() =>
                                  ejecutar(
                                    'El cliente confirmó esta cotización. ¿Marcarla como aceptada?',
                                    () => api.patch(`/api/cotizaciones/${c.id}/aceptar`),
                                    'Cotización aceptada. Ya puede crear el evento.',
                                    true,
                                  )
                                }
                              >
                                Aceptar
                              </button>
                              <button
                                type="button"
                                className="boton boton--texto boton--mini"
                                onClick={() =>
                                  ejecutar(
                                    '¿Rechazar esta cotización?',
                                    () => api.patch(`/api/cotizaciones/${c.id}/rechazar`),
                                    'Cotización rechazada.',
                                  )
                                }
                              >
                                Rechazar
                              </button>
                            </>
                          )}
                        </span>
                      </li>
                    ))}
                  </ul>

                  <div className="acciones acciones--chicas">
                    {!cerrada && (
                      <button
                        type="button"
                        className="boton boton--accion boton--chico"
                        onClick={() => setAccion({ tipo: 'cotizar', solicitud: s })}
                      >
                        {t('Preparar cotización', 'Prepare quote')}
                      </button>
                    )}
                    {aceptada && (
                      <button
                        type="button"
                        className="boton boton--accion boton--chico"
                        onClick={() => setAccion({ tipo: 'evento', solicitud: s, cotizacion: aceptada })}
                      >
                        Crear evento
                      </button>
                    )}
                    {!cerrada && (
                      <>
                        <button
                          type="button"
                          className="boton boton--texto boton--chico"
                          onClick={() =>
                            ejecutar(
                              '¿Rechazar la solicitud? Use esto si no pueden atender el evento.',
                              () => api.patch(`/api/solicitudes/${s.id}/estado`, { estado: 'Rechazada' }),
                              'Solicitud rechazada.',
                            )
                          }
                        >
                          Rechazar solicitud
                        </button>
                        <button
                          type="button"
                          className="boton boton--texto boton--chico"
                          onClick={() =>
                            ejecutar(
                              '¿Cancelar la solicitud? Use esto si el cliente ya no la quiere.',
                              () => api.patch(`/api/solicitudes/${s.id}/estado`, { estado: 'Cancelada' }),
                              'Solicitud cancelada.',
                            )
                          }
                        >
                          Cancelar
                        </button>
                      </>
                    )}
                  </div>
                </div>
              )}
            </article>
          );
        })}
      </div>

      <Dialogo titulo={t('Preparar cotización', 'Prepare quote')} abierto={accion?.tipo === 'cotizar'} alCerrar={() => setAccion(null)}>
        {accion?.tipo === 'cotizar' && (
          <NuevaCotizacion solicitud={accion.solicitud} alTerminar={() => terminar(t('Cotización registrada.', 'Quote saved.'))} />
        )}
      </Dialogo>

      <Dialogo titulo="Crear evento" abierto={accion?.tipo === 'evento'} alCerrar={() => setAccion(null)}>
        {accion?.tipo === 'evento' && (
          <NuevoEvento
            solicitud={accion.solicitud}
            cotizacion={accion.cotizacion}
            alTerminar={() => terminar('Evento creado. Puede verlo en Eventos para asignar equipo y personal.')}
          />
        )}
      </Dialogo>
    </section>
  );
}

function NuevaCotizacion({ solicitud, alTerminar }: { solicitud: Solicitud; alTerminar: () => void }) {
  const { t } = usePreferencias();
  const fidelidad = useDatos<Fidelidad>(`/api/solicitudes/${solicitud.id}/fidelidad`);
  const [monto, setMonto] = useState('');
  const [descuento, setDescuento] = useState('');
  const [detalle, setDetalle] = useState('');
  const [vigente, setVigente] = useState('');
  const f = fidelidad.datos;

  const tienePromo = Boolean(f?.promocionTemporadaNombre && (f.porcentajePromocionTemporada ?? 0) > 0);
  const mejorDescuento = Math.max(f?.descuentoSugerido ?? 0, f?.descuentoPromocionSugerido ?? 0);

  const usarSugerencia = () => {
    if (f?.precioBase) setMonto(String(f.precioBase));
    if (mejorDescuento > 0) setDescuento(String(mejorDescuento));
  };

  return (
    <FormularioAccion
      textoBoton={t('Guardar cotización', 'Save quote')}
      alEnviar={async () => {
        await api.post(`/api/solicitudes/${solicitud.id}/cotizaciones`, {
          monto: Number(monto),
          descuento: Number(descuento) || 0,
          detalle: detalle.trim() || null,
          vigenteHasta: vigente,
        });
        alTerminar();
      }}
    >
      <p className="texto-suave">
        {solicitud.tipoEvento} de {solicitud.nombreContacto}, {formatoFechaHora(solicitud.fechaEvento)}
        {solicitud.paqueteNombre && ` · ${solicitud.paqueteNombre}`}
      </p>

      {f && (
        <Mensaje tipo={f.aplica || tienePromo ? 'exito' : 'info'}>
          <p>
            {f.aplica
              ? `Cliente frecuente: tiene ${f.eventosFinalizados} eventos y le corresponde ${f.porcentajeDescuento}% de descuento.`
              : `Fidelidad: ${f.eventosFinalizados} de ${f.eventosMinimos} eventos para el ${f.porcentajeDescuento}% de descuento.`}
          </p>
          {tienePromo && (
            <p>
              <strong>Promoción de temporada aplicable:</strong> {f.promocionTemporadaNombre} (-
              {f.porcentajePromocionTemporada}%)
              {f.descuentoPromocionSugerido ? ` · ahorro sugerido de ${formatoColones(f.descuentoPromocionSugerido)}` : ''}.
            </p>
          )}
          {f.precioBase !== null && (
            <p>
              <button type="button" className="enlace boton-enlace" onClick={usarSugerencia}>
                Usar precio del paquete ({formatoColones(f.precioBase)})
                {mejorDescuento > 0 ? ` y descuento de ${formatoColones(mejorDescuento)}` : ''}
              </button>
            </p>
          )}
        </Mensaje>
      )}

      <div className="fila">
        <div className="campo">
          <label htmlFor="c-monto">Monto (₡) *</label>
          <input id="c-monto" name="monto" type="number" min={1000} step="1000" required value={monto} onChange={(e) => setMonto(e.target.value)} />
        </div>
        <div className="campo">
          <label htmlFor="c-descuento">Descuento (₡)</label>
          <input id="c-descuento" name="descuento" type="number" min={0} step="1000" value={descuento} onChange={(e) => setDescuento(e.target.value)} />
        </div>
      </div>
      <div className="campo">
        <label htmlFor="c-vigente">Vigente hasta *</label>
        <input id="c-vigente" name="vigenteHasta" type="datetime-local" required min={ahoraParaInput()} value={vigente} onChange={(e) => setVigente(e.target.value)} />
      </div>
      <div className="campo">
        <label htmlFor="c-detalle">Detalle</label>
        <textarea id="c-detalle" name="detalle" maxLength={2000} placeholder="Qué incluye, horas, transporte…" value={detalle} onChange={(e) => setDetalle(e.target.value)} />
      </div>
      {monto && (() => {
        const totalCalculado = Math.max(0, Number(monto) - (Number(descuento) || 0));
        const anticipo50 = Math.round(totalCalculado * 0.5);
        const saldo50 = totalCalculado - anticipo50;
        return (
          <div className="cotizacion__resumen-pago" aria-label={t('Desglose sugerido de cotización', 'Suggested quote breakdown')}>
            <p className="total">
              {t('Total para el cliente:', 'Total for customer:')}{' '}
              <strong className="numero">{formatoColones(totalCalculado)}</strong>
            </p>
            <p className="texto-suave cotizacion__desglose-anticipo">
              <span>
                {t('Anticipo sugerido 50%:', 'Suggested 50% deposit:')}{' '}
                <strong className="numero">{formatoColones(anticipo50)}</strong>
              </span>
              <span aria-hidden="true"> · </span>
              <span>
                {t('Saldo 50% (día del evento):', '50% balance (event day):')}{' '}
                <strong className="numero">{formatoColones(saldo50)}</strong>
              </span>
            </p>
          </div>
        );
      })()}
    </FormularioAccion>
  );
}

function NuevoEvento({
  solicitud,
  cotizacion,
  alTerminar,
}: {
  solicitud: Solicitud;
  cotizacion: Cotizacion;
  alTerminar: () => void;
}) {
  const inicioSugerido = solicitud.fechaEvento.slice(0, 16);
  const finSugerido = (() => {
    const d = new Date(solicitud.fechaEvento);
    d.setHours(d.getHours() + 5);
    d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
    return d.toISOString().slice(0, 16);
  })();
  const [inicio, setInicio] = useState(inicioSugerido);
  const [fin, setFin] = useState(finSugerido);
  const [lugar, setLugar] = useState(solicitud.lugar);
  const [notas, setNotas] = useState('');
  const [copiar, setCopiar] = useState(true);

  return (
    <FormularioAccion
      textoBoton="Crear evento"
      alEnviar={async () => {
        await api.post('/api/eventos', {
          cotizacionId: cotizacion.id,
          fechaInicio: inicio,
          fechaFin: fin,
          lugar: lugar.trim() || null,
          notas: notas.trim() || null,
          copiarEquipoDelPaquete: copiar,
        });
        alTerminar();
      }}
    >
      <p className="texto-suave">
        Cotización aceptada por <strong className="numero">{formatoColones(cotizacion.total)}</strong>
      </p>
      <div className="fila">
        <div className="campo">
          <label htmlFor="e-inicio">Inicio *</label>
          <input id="e-inicio" name="fechaInicio" type="datetime-local" required value={inicio} onChange={(e) => setInicio(e.target.value)} />
        </div>
        <div className="campo">
          <label htmlFor="e-fin">Fin *</label>
          <input id="e-fin" name="fechaFin" type="datetime-local" required min={inicio} value={fin} onChange={(e) => setFin(e.target.value)} />
        </div>
      </div>
      <div className="campo">
        <label htmlFor="e-lugar">Lugar</label>
        <input id="e-lugar" name="lugar" maxLength={300} autoComplete="off" value={lugar} onChange={(e) => setLugar(e.target.value)} />
      </div>
      <div className="campo">
        <label htmlFor="e-notas">Notas</label>
        <textarea id="e-notas" name="notas" maxLength={2000} value={notas} onChange={(e) => setNotas(e.target.value)} />
      </div>
      {solicitud.paqueteId && (
        <label className="casilla" htmlFor="e-copiar">
          <input id="e-copiar" name="copiarEquipo" type="checkbox" checked={copiar} onChange={(e) => setCopiar(e.target.checked)} />
          Reservar el equipo que trae el paquete
        </label>
      )}
    </FormularioAccion>
  );
}
