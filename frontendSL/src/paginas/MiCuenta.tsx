import { Link } from 'react-router-dom';
import { useAuth } from '../auth/contexto';
import { Mensaje } from '../componentes/Estado';
import { formatoFecha } from '../utilidades/formato';

// Página protegida: solo llega aquí quien inició sesión (ver RutaProtegida en App.tsx)
export function MiCuenta() {
  const { perfil } = useAuth();

  if (!perfil) {
    return null;
  }

  return (
    <section className="seccion contenedor contenedor--angosto">
      <div className="encabezado">
        <p className="antetitulo">Su cuenta</p>
        <h1 className="titulo-seccion">Hola, {perfil.nombreCompleto.split(' ')[0]}</h1>
      </div>

      <div className="perfil">
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
      </div>

      <Mensaje>Pronto verá aquí sus solicitudes, cotizaciones y eventos.</Mensaje>

      <div className="acciones acciones--separadas">
        <Link to="/solicitar" className="boton boton--accion">
          Solicitar una cotización
        </Link>
        <Link to="/paquetes" className="boton boton--borde">
          Ver paquetes
        </Link>
      </div>
    </section>
  );
}
