import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../../auth/contexto';

// Estructura del panel de administración y personal: menú lateral (arriba en celular) y la sección elegida
export function Panel() {
  const { perfil, tieneRol } = useAuth();
  const esSuper = tieneRol('Superadministrador');
  const esTrabajador = tieneRol('Trabajador') && !tieneRol('Administrador', 'Superadministrador');

  const secciones = esTrabajador
    ? [
        { a: '/panel', texto: 'Mis Eventos', fin: true },
        { a: '/panel/perfil', texto: 'Mi Perfil' },
      ]
    : [
        { a: '/panel', texto: 'Resumen', fin: true },
        { a: '/panel/inicio', texto: 'Página de Inicio' },
        { a: '/panel/promociones', texto: 'Promociones' },
        { a: '/panel/catalogo', texto: 'Catálogo y Paquetes' },
        { a: '/panel/personal', texto: 'Personal' },
        { a: '/panel/galeria', texto: 'Galería' },
        { a: '/panel/solicitudes', texto: 'Solicitudes' },
        { a: '/panel/eventos', texto: 'Eventos' },
        { a: '/panel/usuarios', texto: 'Usuarios' },
        { a: '/panel/perfil', texto: 'Mi Perfil' },
        ...(esSuper
          ? [
              { a: '/panel/configuracion', texto: 'Configuración' },
              { a: '/panel/bitacora', texto: 'Bitácora' },
            ]
          : []),
      ];

  const rolEtiqueta = esSuper
    ? 'Superadministrador'
    : esTrabajador
      ? 'Empleado / Personal'
      : 'Administrador';

  return (
    <div className="panel contenedor">
      <aside className="panel__lado">
        <p className="antetitulo">Panel</p>
        <p className="panel__quien">
          {perfil?.nombreCompleto}
          <span className="texto-suave">{rolEtiqueta}</span>
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
