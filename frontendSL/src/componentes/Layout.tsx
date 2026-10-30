import { useEffect, useRef, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/contexto';

const ENLACES = [
  { a: '/', texto: 'Inicio' },
  { a: '/paquetes', texto: 'Paquetes' },
  { a: '/galeria', texto: 'Galería' },
  { a: '/preguntas', texto: 'Preguntas' },
];

// Estructura común de todas las páginas: barra de navegación, contenido y pie
export function Layout() {
  const { perfil, cerrarSesion } = useAuth();
  const navegar = useNavigate();
  const { pathname } = useLocation();
  const botonMenu = useRef<HTMLButtonElement>(null);
  const [menuAbierto, setMenuAbierto] = useState(false);
  const [bajo, setBajo] = useState(() => window.scrollY > 40);

  // En la portada la barra flota transparente sobre la foto hasta que la persona baja
  const enPortada = pathname === '/';
  const sobreFoto = enPortada && !bajo && !menuAbierto;

  useEffect(() => {
    const alBajar = () => setBajo(window.scrollY > 40);
    window.addEventListener('scroll', alBajar, { passive: true });
    return () => window.removeEventListener('scroll', alBajar);
  }, []);

  // Esc cierra el menú del celular y devuelve el foco al botón
  useEffect(() => {
    if (!menuAbierto) {
      return;
    }
    const alTeclear = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setMenuAbierto(false);
        botonMenu.current?.focus();
      }
    };
    document.addEventListener('keydown', alTeclear);
    return () => document.removeEventListener('keydown', alTeclear);
  }, [menuAbierto]);

  const cerrarMenu = () => setMenuAbierto(false);

  const salir = () => {
    cerrarSesion();
    cerrarMenu();
    navegar('/');
  };

  const clasesBarra = ['barra', enPortada && 'barra--flotante', sobreFoto && 'barra--sobre-foto']
    .filter(Boolean)
    .join(' ');

  return (
    <div className="app">
      <a href="#contenido" className="saltar">
        Saltar al contenido
      </a>

      <header className={clasesBarra}>
        <div className="contenedor barra__interior">
          <Link to="/" className="marca" translate="no" onClick={cerrarMenu}>
            <span className="marca__nota" aria-hidden="true">
              ♪
            </span>
            Sabor Latino
          </Link>

          <button
            ref={botonMenu}
            type="button"
            className="barra__menu-btn"
            aria-expanded={menuAbierto}
            aria-controls="menu-principal"
            aria-label={menuAbierto ? 'Cerrar menú' : 'Abrir menú'}
            onClick={() => setMenuAbierto((abierto) => !abierto)}
          >
            <span aria-hidden="true">{menuAbierto ? '✕' : '☰'}</span>
          </button>

          <nav id="menu-principal" className={`menu ${menuAbierto ? 'menu--abierto' : ''}`} aria-label="Principal">
            {ENLACES.map((enlace) => (
              <NavLink
                key={enlace.a}
                to={enlace.a}
                end={enlace.a === '/'}
                className="menu__enlace"
                onClick={cerrarMenu}
              >
                {enlace.texto}
              </NavLink>
            ))}

            <div className="menu__cuenta">
              {perfil ? (
                <>
                  <NavLink to="/mi-cuenta" className="menu__enlace" onClick={cerrarMenu}>
                    {perfil.nombreCompleto.split(' ')[0]}
                  </NavLink>
                  <button type="button" className="boton boton--borde boton--chico" onClick={salir}>
                    Salir
                  </button>
                </>
              ) : (
                <NavLink to="/login" className="menu__enlace" onClick={cerrarMenu}>
                  Iniciar sesión
                </NavLink>
              )}
              <Link to="/solicitar" className="boton boton--accion boton--chico" onClick={cerrarMenu}>
                Solicitar cotización
              </Link>
            </div>
          </nav>
        </div>
      </header>

      <main id="contenido" className="contenido" tabIndex={-1}>
        <Outlet />
      </main>

      <footer className="pie">
        <div className="contenedor">
          <div className="pie__grid">
            <div className="encabezado encabezado--sin-margen">
              <Link to="/" className="marca" translate="no">
                <span className="marca__nota" aria-hidden="true">
                  ♪
                </span>
                Sabor Latino
              </Link>
              <p>Karaoke, música en vivo y sonido para sus eventos en Costa Rica.</p>
            </div>
            <div>
              <h2 className="pie__titulo">Explorar</h2>
              <ul>
                <li>
                  <Link to="/paquetes">Paquetes</Link>
                </li>
                <li>
                  <Link to="/galeria">Galería</Link>
                </li>
                <li>
                  <Link to="/preguntas">Preguntas frecuentes</Link>
                </li>
              </ul>
            </div>
            <div>
              <h2 className="pie__titulo">Su cuenta</h2>
              <ul>
                <li>
                  <Link to="/solicitar">Solicitar cotización</Link>
                </li>
                <li>
                  <Link to={perfil ? '/mi-cuenta' : '/login'}>{perfil ? 'Mi cuenta' : 'Iniciar sesión'}</Link>
                </li>
                {!perfil && (
                  <li>
                    <Link to="/registro">Crear cuenta</Link>
                  </li>
                )}
              </ul>
            </div>
          </div>
          <p className="pie__legal">
            © {new Date().getFullYear()} <span translate="no">Sabor Latino</span>
          </p>
        </div>
      </footer>
    </div>
  );
}
