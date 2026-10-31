import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { Resumen as DatosResumen } from '../../api/tipos';
import { Cargando, Mensaje } from '../../componentes/Estado';
import { Cifra, TituloPanel } from '../../componentes/panel/Piezas';
import { useDatos } from '../../hooks/useDatos';
import { nombreEstado } from '../../utilidades/estados';
import { formatoColones } from '../../utilidades/formato';

const MESES = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];

// Portada del panel: cómo va el mes
export function Resumen() {
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
