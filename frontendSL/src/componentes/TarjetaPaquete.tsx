import { Link } from 'react-router-dom';
import type { Paquete } from '../api/tipos';
import { formatoColones } from '../utilidades/formato';
import { fotoDePaquete } from '../utilidades/fotos';

// Tarjeta de un paquete: foto grande, equipo incluido, precio y botón para cotizar
export function TarjetaPaquete({ paquete }: { paquete: Paquete }) {
  return (
    <article className="paquete">
      <div className="paquete__foto">
        <img src={fotoDePaquete(paquete)} alt="" width={800} height={600} loading="lazy" decoding="async" />
        <span className="paquete__servicio">{paquete.servicioNombre}</span>
      </div>

      <div className="paquete__cuerpo">
        <h3>{paquete.nombre}</h3>
        {paquete.descripcion && <p>{paquete.descripcion}</p>}

        {paquete.equipos.length > 0 && (
          <ul className="equipo" aria-label="Equipo incluido">
            {paquete.equipos.map((e) => (
              <li key={e.equipoId}>
                {e.cantidad} × {e.equipoNombre}
              </li>
            ))}
          </ul>
        )}

        <div className="paquete__pie">
          <p className="precio">
            <span>Desde</span>
            <strong>{formatoColones(paquete.precioBase)}</strong>
          </p>
          <Link
            to={`/solicitar?paquete=${paquete.id}`}
            className="boton boton--accion boton--chico"
            aria-label={`Cotizar ${paquete.nombre}`}
          >
            Cotizar
          </Link>
        </div>
      </div>
    </article>
  );
}
