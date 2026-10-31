import { useState, type ReactNode } from 'react';
import { api } from '../../api/cliente';
import { useDatos } from '../../hooks/useDatos';
import { Cargando, Mensaje } from '../Estado';
import { Dialogo, FormularioAccion, TituloPanel } from './Piezas';

// Un campo del formulario de crear/editar
export interface CampoCrud {
  nombre: string;
  etiqueta: string;
  tipo?: 'texto' | 'area' | 'numero' | 'url' | 'check' | 'select';
  requerido?: boolean;
  max?: number;
  min?: number;
  paso?: string;
  ayuda?: string;
  opciones?: { valor: string | number; texto: string }[];
}

// Una columna de la tabla
export interface ColumnaCrud<T> {
  etiqueta: string;
  valor: (fila: T) => ReactNode;
}

interface Props<T extends { id: number; activo: boolean }> {
  titulo: string;
  ruta: string; // por ejemplo /api/servicios
  columnas: ColumnaCrud<T>[];
  campos: CampoCrud[];
  // Valores iniciales para "Nuevo"
  nuevo: Record<string, unknown>;
  // Convierte una fila de la API en los valores del formulario (por defecto, la misma fila)
  aFormulario?: (fila: T) => Record<string, unknown>;
  // Convierte los valores del formulario en lo que espera la API
  aApi?: (valores: Record<string, unknown>) => unknown;
  // Campos especiales que no caben en la lista (por ejemplo, el equipo de un paquete)
  extra?: (valores: Record<string, unknown>, cambiar: (nombre: string, valor: unknown) => void) => ReactNode;
}

// Pantalla genérica de catálogo: tabla + crear + editar + desactivar.
// "Desactivar" no borra: la API marca Activo = false y se puede reactivar editando.
export function Crud<T extends { id: number; activo: boolean }>({
  titulo,
  ruta,
  columnas,
  campos,
  nuevo,
  aFormulario = (fila) => ({ ...fila }),
  aApi = (valores) => valores,
  extra,
}: Props<T>) {
  const { datos, cargando, error, recargar } = useDatos<T[]>(`${ruta}?incluirInactivos=true`);
  const [editando, setEditando] = useState<{ id: number | null; valores: Record<string, unknown> } | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  const cambiar = (nombre: string, valor: unknown) =>
    setEditando((e) => (e ? { ...e, valores: { ...e.valores, [nombre]: valor } } : e));

  const guardar = async () => {
    if (!editando) return;
    const cuerpo = aApi(editando.valores);
    if (editando.id === null) {
      await api.post(ruta, cuerpo);
      setAviso('Se creó correctamente.');
    } else {
      await api.put(`${ruta}/${editando.id}`, cuerpo);
      setAviso('Se guardaron los cambios.');
    }
    setEditando(null);
    recargar();
  };

  const desactivar = async (fila: T) => {
    if (!window.confirm('¿Desactivar este registro? Podrá reactivarlo después editándolo.')) return;
    try {
      await api.delete(`${ruta}/${fila.id}`);
      setAviso('Se desactivó.');
      recargar();
    } catch {
      setAviso('No se pudo desactivar.');
    }
  };

  return (
    <section>
      <TituloPanel titulo={titulo}>
        <button
          type="button"
          className="boton boton--accion boton--chico"
          onClick={() => setEditando({ id: null, valores: { ...nuevo } })}
        >
          + Nuevo
        </button>
      </TituloPanel>

      {aviso && <Mensaje tipo="exito">{aviso}</Mensaje>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {datos && datos.length === 0 && <Mensaje>Todavía no hay registros.</Mensaje>}

      {datos && datos.length > 0 && (
        <div className="tabla-envoltura">
          <table className="tabla">
            <thead>
              <tr>
                {columnas.map((c) => (
                  <th key={c.etiqueta} scope="col">
                    {c.etiqueta}
                  </th>
                ))}
                <th scope="col">Estado</th>
                <th scope="col">
                  <span className="solo-lectores">Acciones</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {datos.map((fila) => (
                <tr key={fila.id} className={fila.activo ? '' : 'tabla__inactiva'}>
                  {columnas.map((c) => (
                    <td key={c.etiqueta} data-etiqueta={c.etiqueta}>
                      {c.valor(fila)}
                    </td>
                  ))}
                  <td data-etiqueta="Estado">
                    <span className={`insignia insignia--${fila.activo ? 'ok' : 'apagado'}`}>
                      {fila.activo ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                  <td className="tabla__acciones">
                    <button
                      type="button"
                      className="boton boton--borde boton--mini"
                      onClick={() => setEditando({ id: fila.id, valores: aFormulario(fila) })}
                    >
                      Editar
                    </button>
                    {fila.activo && (
                      <button type="button" className="boton boton--texto boton--mini" onClick={() => desactivar(fila)}>
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
        titulo={editando?.id === null ? `Nuevo: ${titulo.toLowerCase()}` : `Editar: ${titulo.toLowerCase()}`}
        abierto={editando !== null}
        alCerrar={() => setEditando(null)}
      >
        {editando && (
          <FormularioAccion textoBoton="Guardar" alEnviar={guardar}>
            {campos.map((campo) => (
              <CampoFormulario key={campo.nombre} campo={campo} valor={editando.valores[campo.nombre]} cambiar={cambiar} />
            ))}
            {extra?.(editando.valores, cambiar)}
          </FormularioAccion>
        )}
      </Dialogo>
    </section>
  );
}

function CampoFormulario({
  campo,
  valor,
  cambiar,
}: {
  campo: CampoCrud;
  valor: unknown;
  cambiar: (nombre: string, valor: unknown) => void;
}) {
  const id = `campo-${campo.nombre}`;
  const texto = valor === null || valor === undefined ? '' : String(valor);

  if (campo.tipo === 'check') {
    return (
      <label className="casilla" htmlFor={id}>
        <input
          id={id}
          name={campo.nombre}
          type="checkbox"
          checked={Boolean(valor)}
          onChange={(e) => cambiar(campo.nombre, e.target.checked)}
        />
        {campo.etiqueta}
      </label>
    );
  }

  return (
    <div className="campo">
      <label htmlFor={id}>
        {campo.etiqueta}
        {campo.requerido && ' *'}
      </label>
      {campo.tipo === 'area' ? (
        <textarea
          id={id}
          name={campo.nombre}
          required={campo.requerido}
          maxLength={campo.max}
          value={texto}
          onChange={(e) => cambiar(campo.nombre, e.target.value)}
        />
      ) : campo.tipo === 'select' ? (
        <select
          id={id}
          name={campo.nombre}
          required={campo.requerido}
          value={texto}
          onChange={(e) => cambiar(campo.nombre, e.target.value)}
        >
          <option value="">Elija una opción…</option>
          {campo.opciones?.map((o) => (
            <option key={o.valor} value={o.valor}>
              {o.texto}
            </option>
          ))}
        </select>
      ) : (
        <input
          id={id}
          name={campo.nombre}
          type={campo.tipo === 'numero' ? 'number' : campo.tipo === 'url' ? 'url' : 'text'}
          inputMode={campo.tipo === 'numero' ? 'decimal' : undefined}
          required={campo.requerido}
          maxLength={campo.tipo === 'numero' ? undefined : campo.max}
          min={campo.min}
          max={campo.tipo === 'numero' ? campo.max : undefined}
          step={campo.paso}
          autoComplete="off"
          value={texto}
          onChange={(e) => cambiar(campo.nombre, e.target.value)}
        />
      )}
      {campo.ayuda && <p className="campo__ayuda">{campo.ayuda}</p>}
    </div>
  );
}
