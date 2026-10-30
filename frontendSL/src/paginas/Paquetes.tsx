import { useSearchParams } from 'react-router-dom';
import type { Paquete, Servicio } from '../api/tipos';
import { Cargando, Mensaje } from '../componentes/Estado';
import { TarjetaPaquete } from '../componentes/TarjetaPaquete';
import { useDatos } from '../hooks/useDatos';

export function Paquetes() {
  // El filtro se guarda en la URL (?servicio=2), así se puede compartir el enlace
  const [parametros, setParametros] = useSearchParams();
  const servicioId = parametros.get('servicio') ?? '';

  const servicios = useDatos<Servicio[]>('/api/servicios');
  const paquetes = useDatos<Paquete[]>(servicioId ? `/api/paquetes?servicioId=${servicioId}` : '/api/paquetes');

  const cambiarServicio = (valor: string) => {
    setParametros(valor ? { servicio: valor } : {});
  };

  const opciones = [{ id: '', nombre: 'Todos' }, ...(servicios.datos ?? []).map((s) => ({ id: String(s.id), nombre: s.nombre }))];

  return (
    <section className="seccion contenedor">
      <div className="encabezado">
        <p className="antetitulo">Paquetes</p>
        <h1 className="titulo-seccion">Elija cómo quiere que suene</h1>
        <p className="intro">Precios de referencia. La cotización final depende de la fecha, el lugar y las horas.</p>
      </div>

      <div className="filtros" role="group" aria-label="Filtrar paquetes por servicio">
        {opciones.map((o) => (
          <button
            key={o.id || 'todos'}
            type="button"
            className="chip"
            aria-pressed={servicioId === o.id}
            onClick={() => cambiarServicio(o.id)}
          >
            {o.nombre}
          </button>
        ))}
      </div>

      <div aria-live="polite">
        {paquetes.cargando && <Cargando />}
        {paquetes.error && <Mensaje tipo="error">{paquetes.error}</Mensaje>}
        {paquetes.datos && paquetes.datos.length === 0 && (
          <Mensaje>No hay paquetes disponibles para este servicio por ahora.</Mensaje>
        )}
        {paquetes.datos && paquetes.datos.length > 0 && (
          <div className="paquetes">
            {paquetes.datos.map((p) => (
              <TarjetaPaquete key={p.id} paquete={p} />
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
