import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../../auth/contexto';

// Estructura del panel de administración: menú lateral (arriba en celular) y la sección elegida
export function Panel() {
  const { perfil, tieneRol } = useAuth();
  const esSuper = tieneRol('Superadministrador');

  const secciones = [
    { a: '/panel', texto: 'Resumen', fin: true },
    { a: '/panel/solicitudes', texto: 'Solicitudes' },
    { a: '/panel/eventos', texto: 'Eventos' },
    { a: '/panel/catalogo', texto: 'Catálogo' },
    { a: '/panel/usuarios', texto: 'Usuarios' },
    ...(esSuper
      ? [
          { a: '/panel/configuracion', texto: 'Configuración' },
          { a: '/panel/bitacora', texto: 'Bitácora' },
        ]
      : []),
  ];

  return (
    <div className="panel contenedor">
      <aside className="panel__lado">
        <p className="antetitulo">Panel</p>
        <p className="panel__quien">
          {perfil?.nombreCompleto}
          <span className="texto-suave">{esSuper ? 'Superadministrador' : 'Administrador'}</span>
        </p>
        <nav aria-label="Secciones del panel">
          <ul className="panel__menu">
            {secciones.map((s) => (
              <li key={s.a}>
                <NavLink to={s.a} end={s.fin} className="panel__enlace">
                  {s.texto}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </aside>
      <div className="panel__contenido">
        <Outlet />
      </div>
    </div>
  );
}
