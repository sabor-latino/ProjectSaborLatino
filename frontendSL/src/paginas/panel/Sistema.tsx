import { useState } from 'react';
import { api } from '../../api/cliente';
import type { Configuracion as DatosConfiguracion, RegistroBitacora, Usuario } from '../../api/tipos';
import { Cargando, Mensaje } from '../../componentes/Estado';
import { FormularioAccion, TituloPanel } from '../../componentes/panel/Piezas';
import { useDatos } from '../../hooks/useDatos';
import { formatoFechaHora } from '../../utilidades/formato';

// Nombres claros para las claves de configuración que trae la base de datos
const NOMBRES_CLAVE: Record<string, string> = {
  'Fidelidad.EventosMinimos': 'Eventos para ser cliente frecuente',
  'Fidelidad.PorcentajeDescuento': 'Descuento del cliente frecuente (%)',
};

// Solo Superadministrador: reglas del negocio
export function Configuracion() {
  const { datos, cargando, error, recargar } = useDatos<DatosConfiguracion[]>('/api/configuracion');
  const [aviso, setAviso] = useState<string | null>(null);

  return (
    <section>
      <TituloPanel titulo="Configuración" />
      {aviso && <Mensaje tipo="exito">{aviso}</Mensaje>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      <div className="rejilla-panel">
        {datos?.map((c) => (
          <div key={c.clave} className="caja">
            <ValorConfiguracion
              config={c}
              alGuardar={() => {
                setAviso('Configuración guardada.');
                recargar();
              }}
            />
          </div>
        ))}
      </div>
    </section>
  );
}

function ValorConfiguracion({ config, alGuardar }: { config: DatosConfiguracion; alGuardar: () => void }) {
  const [valor, setValor] = useState(config.valor);
  const id = `config-${config.clave}`;

  return (
    <FormularioAccion
      textoBoton="Guardar"
      alEnviar={async () => {
        await api.put(`/api/configuracion/${encodeURIComponent(config.clave)}`, { valor: valor.trim() });
        alGuardar();
      }}
    >
      <div className="campo">
        <label htmlFor={id}>{NOMBRES_CLAVE[config.clave] ?? config.clave}</label>
        <input id={id} name={config.clave} required maxLength={500} autoComplete="off" value={valor} onChange={(e) => setValor(e.target.value)} />
        {config.descripcion && <p className="campo__ayuda">{config.descripcion}</p>}
      </div>
    </FormularioAccion>
  );
}

const ENTIDADES = ['', 'Solicitud', 'Cotizacion', 'Evento', 'Paquete', 'Servicio', 'Equipo', 'Empleado', 'ImagenGaleria', 'PreguntaFrecuente', 'Configuracion'];

// Solo Superadministrador: quién hizo qué y cuándo (la API lo registra sola)
export function Bitacora() {
  const [entidad, setEntidad] = useState('');
  const { datos, cargando, error } = useDatos<RegistroBitacora[]>(entidad ? `/api/bitacora?entidad=${entidad}` : '/api/bitacora');
  const usuarios = useDatos<Usuario[]>('/api/usuarios?incluirInactivos=true');
  const nombre = (id: string | null) =>
    id ? (usuarios.datos?.find((u) => u.id === id)?.nombreCompleto ?? 'Usuario') : 'Visitante o sistema';

  return (
    <section>
      <TituloPanel titulo="Bitácora">
        <label className="filtro">
          <span>Tipo</span>
          <select name="entidad" value={entidad} onChange={(e) => setEntidad(e.target.value)}>
            {ENTIDADES.map((e) => (
              <option key={e || 'todas'} value={e}>
                {e || 'Todos'}
              </option>
            ))}
          </select>
        </label>
      </TituloPanel>

      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {datos && datos.length === 0 && <Mensaje>No hay registros.</Mensaje>}
      {datos && datos.length > 0 && (
        <div className="tabla-envoltura">
          <table className="tabla tabla--compacta">
            <thead>
              <tr>
                <th scope="col">Fecha</th>
                <th scope="col">Quién</th>
                <th scope="col">Acción</th>
                <th scope="col">Qué</th>
              </tr>
            </thead>
            <tbody>
              {datos.map((b) => (
                <tr key={b.id}>
                  <td data-etiqueta="Fecha" className="numero">
                    {formatoFechaHora(b.fecha)}
                  </td>
                  <td data-etiqueta="Quién">{nombre(b.usuarioId)}</td>
                  <td data-etiqueta="Acción">{b.accion}</td>
                  <td data-etiqueta="Qué">
                    {b.entidad}
                    {b.entidadId && ` #${b.entidadId}`}
                    {b.detalle && <span className="texto-suave"> · {b.detalle}</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
