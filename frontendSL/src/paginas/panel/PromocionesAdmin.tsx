import { useState } from 'react';
import { api } from '../../api/cliente';
import type { Paquete, PromocionTemporada } from '../../api/tipos';
import { Cargando, Mensaje } from '../../componentes/Estado';
import { Dialogo, FormularioAccion, TituloPanel } from '../../componentes/panel/Piezas';
import { useDatos } from '../../hooks/useDatos';
import { formatoColones, formatoFecha, paraInputFecha } from '../../utilidades/formato';

interface FormularioPromocion {
  nombre: string;
  nombreEn: string;
  etiquetaTemporada: string;
  etiquetaTemporadaEn: string;
  descripcion: string;
  descripcionEn: string;
  porcentajeDescuento: number;
  codigoPromocional: string;
  fechaInicio: string;
  fechaFin: string;
  imagenUrl: string;
  destacadaEnInicio: boolean;
  activo: boolean;
  paqueteIds: number[];
}

function nuevoFormularioPromocion(): FormularioPromocion {
  const hoy = new Date();
  const fin = new Date();
  fin.setDate(hoy.getDate() + 30);
  return {
    nombre: '',
    nombreEn: '',
    etiquetaTemporada: '',
    etiquetaTemporadaEn: '',
    descripcion: '',
    descripcionEn: '',
    porcentajeDescuento: 15,
    codigoPromocional: '',
    fechaInicio: hoy.toISOString().slice(0, 16),
    fechaFin: fin.toISOString().slice(0, 16),
    imagenUrl: '',
    destacadaEnInicio: true,
    activo: true,
    paqueteIds: [],
  };
}

export function PromocionesAdmin() {
  const { datos, cargando, error, recargar } = useDatos<PromocionTemporada[]>('/api/promociones?incluirInactivas=true');
  const paquetes = useDatos<Paquete[]>('/api/paquetes?incluirInactivos=true');
  const [editando, setEditando] = useState<{ id: number | null; valores: FormularioPromocion } | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  const listaPromociones = Array.isArray(datos) ? datos : [];
  const listaPaquetes = Array.isArray(paquetes.datos) ? paquetes.datos : [];

  const abrirEdicion = (p: PromocionTemporada) => {
    setEditando({
      id: p.id,
      valores: {
        nombre: p.nombre,
        nombreEn: p.nombreEn ?? '',
        etiquetaTemporada: p.etiquetaTemporada ?? '',
        etiquetaTemporadaEn: p.etiquetaTemporadaEn ?? '',
        descripcion: p.descripcion ?? '',
        descripcionEn: p.descripcionEn ?? '',
        porcentajeDescuento: p.porcentajeDescuento,
        codigoPromocional: p.codigoPromocional ?? '',
        fechaInicio: paraInputFecha(p.fechaInicio),
        fechaFin: paraInputFecha(p.fechaFin),
        imagenUrl: p.imagenUrl ?? '',
        destacadaEnInicio: p.destacadaEnInicio,
        activo: p.activo,
        paqueteIds: p.paquetes?.map((pk) => pk.paqueteId) ?? [],
      },
    });
  };

  const cambiar = <K extends keyof FormularioPromocion>(campo: K, valor: FormularioPromocion[K]) => {
    setEditando((prev) => (prev ? { ...prev, valores: { ...prev.valores, [campo]: valor } } : prev));
  };

  const alternarPaquete = (paqueteId: number, marcado: boolean) => {
    if (!editando) return;
    const actuales = editando.valores.paqueteIds;
    const nuevos = marcado ? [...actuales, paqueteId] : actuales.filter((id) => id !== paqueteId);
    cambiar('paqueteIds', nuevos);
  };

  const guardar = async () => {
    if (!editando) return;
    const v = editando.valores;
    const cuerpo = {
      nombre: v.nombre.trim(),
      nombreEn: v.nombreEn.trim() || null,
      etiquetaTemporada: v.etiquetaTemporada.trim() || null,
      etiquetaTemporadaEn: v.etiquetaTemporadaEn.trim() || null,
      descripcion: v.descripcion.trim() || null,
      descripcionEn: v.descripcionEn.trim() || null,
      porcentajeDescuento: Number(v.porcentajeDescuento),
      codigoPromocional: v.codigoPromocional.trim() || null,
      fechaInicio: v.fechaInicio,
      fechaFin: v.fechaFin,
      imagenUrl: v.imagenUrl.trim() || null,
      destacadaEnInicio: Boolean(v.destacadaEnInicio),
      activo: Boolean(v.activo),
      paqueteIds: v.paqueteIds,
    };

    if (editando.id === null) {
      await api.post('/api/promociones', cuerpo);
      setAviso('Promoción de temporada creada correctamente.');
    } else {
      await api.put(`/api/promociones/${editando.id}`, cuerpo);
      setAviso('Cambios de la promoción guardados correctamente.');
    }
    setEditando(null);
    recargar();
  };

  const desactivar = async (promo: PromocionTemporada) => {
    if (!window.confirm('¿Desactivar esta promoción de temporada?')) return;
    try {
      await api.delete(`/api/promociones/${promo.id}`);
      setAviso('Promoción desactivada.');
      recargar();
    } catch {
      setAviso('No se pudo desactivar la promoción.');
    }
  };

  return (
    <section>
      <TituloPanel titulo="Promociones de Temporada">
        <button
          type="button"
          className="boton boton--accion boton--chico"
          onClick={() => setEditando({ id: null, valores: nuevoFormularioPromocion() })}
        >
          + Nueva promoción
        </button>
      </TituloPanel>

      <p className="texto-suave">
        Configure campañas por temporada (fin de año, graduaciones, bodas de verano, aniversarios) con porcentajes de
        descuento y paquetes asociados.
      </p>

      {aviso && <Mensaje tipo="exito">{aviso}</Mensaje>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {datos && listaPromociones.length === 0 && <Mensaje>Todavía no hay promociones registradas.</Mensaje>}

      {listaPromociones.length > 0 && (
        <div className="tabla-envoltura">
          <table className="tabla">
            <thead>
              <tr>
                <th scope="col">Promoción</th>
                <th scope="col">Temporada</th>
                <th scope="col">Descuento</th>
                <th scope="col">Vigencia</th>
                <th scope="col">Paquetes</th>
                <th scope="col">Estado</th>
                <th scope="col">
                  <span className="solo-lectores">Acciones</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {listaPromociones.map((promo) => (
                <tr key={promo.id} className={promo.activo ? '' : 'tabla__inactiva'}>
                  <td data-etiqueta="Promoción">
                    <strong>{promo.nombre}</strong>
                    {promo.codigoPromocional && <div className="texto-suave">Código: {promo.codigoPromocional}</div>}
                  </td>
                  <td data-etiqueta="Temporada">{promo.etiquetaTemporada ?? '—'}</td>
                  <td data-etiqueta="Descuento">
                    <span className="insignia insignia--alerta">-{promo.porcentajeDescuento}%</span>
                  </td>
                  <td data-etiqueta="Vigencia">
                    {formatoFecha(promo.fechaInicio)} — {formatoFecha(promo.fechaFin)}
                  </td>
                  <td data-etiqueta="Paquetes">
                    {promo.paquetes?.map((pk) => pk.paqueteNombre).join(', ') || 'Todos / General'}
                  </td>
                  <td data-etiqueta="Estado">
                    <span className={`insignia insignia--${promo.activo && promo.vigente ? 'ok' : 'apagado'}`}>
                      {promo.activo ? (promo.vigente ? 'Vigente' : 'Programada / Vencida') : 'Inactiva'}
                    </span>
                  </td>
                  <td className="tabla__acciones">
                    <button
                      type="button"
                      className="boton boton--borde boton--mini"
                      onClick={() => abrirEdicion(promo)}
                    >
                      Editar
                    </button>
                    {promo.activo && (
                      <button
                        type="button"
                        className="boton boton--texto boton--mini"
                        onClick={() => desactivar(promo)}
                      >
                        Desactivar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Dialogo
        titulo={editando?.id === null ? 'Nueva promoción de temporada' : 'Editar promoción de temporada'}
        abierto={editando !== null}
        alCerrar={() => setEditando(null)}
      >
        {editando && (
          <FormularioAccion textoBoton="Guardar promoción" alEnviar={guardar}>
            <div className="fila">
              <div className="campo">
                <label htmlFor="pr-nombre">Nombre (Español) *</label>
                <input
                  id="pr-nombre"
                  required
                  maxLength={120}
                  value={editando.valores.nombre}
                  onChange={(e) => cambiar('nombre', e.target.value)}
                />
              </div>
              <div className="campo">
                <label htmlFor="pr-nombre-en">Nombre (Inglés)</label>
                <input
                  id="pr-nombre-en"
                  maxLength={120}
                  value={editando.valores.nombreEn}
                  onChange={(e) => cambiar('nombreEn', e.target.value)}
                />
              </div>
            </div>

            <div className="fila">
              <div className="campo">
                <label htmlFor="pr-etiqueta">Etiqueta de temporada (Español)</label>
                <input
                  id="pr-etiqueta"
                  maxLength={80}
                  placeholder="Temporada de Graduaciones, Verano…"
                  value={editando.valores.etiquetaTemporada}
                  onChange={(e) => cambiar('etiquetaTemporada', e.target.value)}
                />
              </div>
              <div className="campo">
                <label htmlFor="pr-etiqueta-en">Etiqueta de temporada (Inglés)</label>
                <input
                  id="pr-etiqueta-en"
                  maxLength={80}
                  placeholder="Graduation Season, Summer…"
                  value={editando.valores.etiquetaTemporadaEn}
                  onChange={(e) => cambiar('etiquetaTemporadaEn', e.target.value)}
                />
              </div>
            </div>

            <div className="fila">
              <div className="campo">
                <label htmlFor="pr-descuento">Porcentaje de descuento (1% – 90%) *</label>
                <input
                  id="pr-descuento"
                  type="number"
                  required
                  min={1}
                  max={90}
                  value={editando.valores.porcentajeDescuento}
                  onChange={(e) => cambiar('porcentajeDescuento', Number(e.target.value))}
                />
              </div>
              <div className="campo">
                <label htmlFor="pr-codigo">Código promocional (opcional)</label>
                <input
                  id="pr-codigo"
                  maxLength={40}
                  placeholder="FIESTA2026"
                  value={editando.valores.codigoPromocional}
                  onChange={(e) => cambiar('codigoPromocional', e.target.value)}
                />
              </div>
            </div>

            <div className="fila">
              <div className="campo">
                <label htmlFor="pr-inicio">Fecha de inicio *</label>
                <input
                  id="pr-inicio"
                  type="datetime-local"
                  required
                  value={editando.valores.fechaInicio}
                  onChange={(e) => cambiar('fechaInicio', e.target.value)}
                />
              </div>
              <div className="campo">
                <label htmlFor="pr-fin">Fecha de fin *</label>
                <input
                  id="pr-fin"
                  type="datetime-local"
                  required
                  value={editando.valores.fechaFin}
                  onChange={(e) => cambiar('fechaFin', e.target.value)}
                />
              </div>
            </div>

            <div className="campo">
              <label htmlFor="pr-desc">Descripción (Español)</label>
              <textarea
                id="pr-desc"
                maxLength={1000}
                value={editando.valores.descripcion}
                onChange={(e) => cambiar('descripcion', e.target.value)}
              />
            </div>

            <div className="campo">
              <label htmlFor="pr-desc-en">Descripción (Inglés)</label>
              <textarea
                id="pr-desc-en"
                maxLength={1000}
                value={editando.valores.descripcionEn}
                onChange={(e) => cambiar('descripcionEn', e.target.value)}
              />
            </div>

            <fieldset className="grupo-campos">
              <legend>Paquetes incluidos en la promoción</legend>
              {listaPaquetes.map((pk) => {
                const seleccionado = editando.valores.paqueteIds.includes(pk.id);
                return (
                  <label key={pk.id} className="casilla" htmlFor={`pr-pk-${pk.id}`}>
                    <input
                      id={`pr-pk-${pk.id}`}
                      type="checkbox"
                      checked={seleccionado}
                      onChange={(e) => alternarPaquete(pk.id, e.target.checked)}
                    />
                    {pk.nombre} ({formatoColones(pk.precioBase)})
                  </label>
                );
              })}
            </fieldset>

            <label className="casilla" htmlFor="pr-destacada">
              <input
                id="pr-destacada"
                type="checkbox"
                checked={editando.valores.destacadaEnInicio}
                onChange={(e) => cambiar('destacadaEnInicio', e.target.checked)}
              />
              Destacar en la Página de Inicio
            </label>

            <label className="casilla" htmlFor="pr-activo">
              <input
                id="pr-activo"
                type="checkbox"
                checked={editando.valores.activo}
                onChange={(e) => cambiar('activo', e.target.checked)}
              />
              Activa (visible en el sitio)
            </label>
          </FormularioAccion>
        )}
      </Dialogo>
    </section>
  );
}
