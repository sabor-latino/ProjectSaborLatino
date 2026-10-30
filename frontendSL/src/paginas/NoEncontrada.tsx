import { Link } from 'react-router-dom';

export function NoEncontrada() {
  return (
    <section className="seccion contenedor contenedor--angosto centrado">
      <p className="no-encontrada__codigo" aria-hidden="true">
        404
      </p>
      <div className="encabezado encabezado--centro">
        <h1 className="titulo-seccion">Esta página no existe</h1>
        <p className="intro">Puede que el enlace esté mal escrito o que la página se haya movido.</p>
      </div>
      <div className="acciones acciones--centradas">
        <Link to="/" className="boton boton--accion">
          Volver al inicio
        </Link>
        <Link to="/paquetes" className="boton boton--borde">
          Ver paquetes
        </Link>
      </div>
    </section>
  );
}
