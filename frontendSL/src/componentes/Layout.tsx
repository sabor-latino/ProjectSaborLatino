import { useEffect, useRef, useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/contexto';
import type { ContenidoInicio } from '../api/tipos';
import { useDatos } from '../hooks/useDatos';
import { IconoFacebook, IconoInstagram, IconoTikTok, IconoWhatsApp, IconoYouTube } from './Iconos';
import { usePreferencias, IDIOMAS_DISPONIBLES, type EscalaFuente, type ModoContraste } from '../preferencias/PreferenciasContext';
import { obtenerModoPortal, obtenerUrlSitioPublico, SOLO_ADMIN, SOLO_PUBLICO, type ModoPortal } from '../utilidades/portal';

const OPCIONES_FUENTE: { valor: EscalaFuente; etiqueta: string; descripcionEs: string; descripcionEn: string }[] = [
  { valor: 'normal', etiqueta: 'A', descripcionEs: 'Tamaño de letra normal', descripcionEn: 'Normal font size' },
  { valor: 'grande', etiqueta: 'A+', descripcionEs: 'Tamaño de letra grande', descripcionEn: 'Large font size' },
  { valor: 'extra', etiqueta: 'A++', descripcionEs: 'Tamaño de letra extra grande', descripcionEn: 'Extra large font size' },
];

const OPCIONES_CONTRASTE: { valor: ModoContraste; cortoEs: string; cortoEn: string; descEs: string; descEn: string }[] = [
  {
    valor: 'estandar',
    cortoEs: 'Estándar',
    cortoEn: 'Standard',
    descEs: 'Contraste estándar',
    descEn: 'Standard contrast',
  },
  {
    valor: 'alto-claro',
    cortoEs: 'Alto claro',
    cortoEn: 'High light',
    descEs: 'Alto contraste claro',
    descEn: 'High contrast light',
  },
  {
    valor: 'alto-oscuro',
    cortoEs: 'Alto oscuro',
    cortoEn: 'High dark',
    descEs: 'Alto contraste oscuro',
    descEn: 'High contrast dark',
  },
];

// Estructura común: se adapta según modoPortal ('publico' sin login vs 'admin' privado)
export function Layout({ modoPortal }: { modoPortal?: ModoPortal } = {}) {
  const modo = obtenerModoPortal(modoPortal);
  const esPublico = SOLO_PUBLICO || (!SOLO_ADMIN && modo === 'publico');
  const { perfil, cerrarSesion, tieneRol } = useAuth();
  const { idioma, setIdioma, escalaFuente, setEscalaFuente, contraste, setContraste, t, textos } = usePreferencias();
  const { datos: contenido } = useDatos<ContenidoInicio>('/api/contenido-inicio');
  const esAdmin = tieneRol('Administrador', 'Superadministrador');
  const navegar = useNavigate();
  const { pathname } = useLocation();
  const botonMenu = useRef<HTMLButtonElement>(null);
  const [menuAbierto, setMenuAbierto] = useState(false);
  const [bajo, setBajo] = useState(() => window.scrollY > 40);
  const [expandidoIdioma, setExpandidoIdioma] = useState(false);
  const [expandidoFuente, setExpandidoFuente] = useState(false);
  const [expandidoContraste, setExpandidoContraste] = useState(false);

  const opcionFuenteActual = OPCIONES_FUENTE.find((op) => op.valor === escalaFuente) ?? OPCIONES_FUENTE[0];
  const opcionContrasteActual = OPCIONES_CONTRASTE.find((op) => op.valor === contraste) ?? OPCIONES_CONTRASTE[0];

  const enlacesPublicos = [
    { a: '/', texto: textos.inicio },
    { a: '/paquetes', texto: textos.paquetes },
    { a: '/galeria', texto: textos.galeria },
    { a: '/preguntas', texto: textos.preguntas },
  ];

  // En la portada pública la barra flota transparente sobre la foto hasta que la persona baja
  const enPortada = esPublico && pathname === '/';
  const sobreFoto = enPortada && !bajo && !menuAbierto && contraste === 'estandar';

  useEffect(() => {
    const alBajar = () => setBajo(window.scrollY > 40);
    window.addEventListener('scroll', alBajar, { passive: true });
    return () => window.removeEventListener('scroll', alBajar);
  }, []);

  useEffect(() => {
    const cerrarAlClicFuera = (e: MouseEvent) => {
      const target = e.target as HTMLElement | null;
      if (!target?.closest('.accesibilidad-grupo')) {
        setExpandidoIdioma(false);
        setExpandidoFuente(false);
        setExpandidoContraste(false);
      }
    };
    document.addEventListener('click', cerrarAlClicFuera);
    return () => document.removeEventListener('click', cerrarAlClicFuera);
  }, []);

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
    navegar(!esPublico ? '/login' : '/');
  };

  const clasesBarra = ['barra', enPortada && 'barra--flotante', sobreFoto && 'barra--sobre-foto']
    .filter(Boolean)
    .join(' ');

  return (
    <div className="app">
      <a href="#contenido" className="saltar">
        {textos.saltarContenido}
      </a>

      <header className={clasesBarra}>
        <div className="contenedor barra__interior">
          <Link to={!esPublico ? '/panel' : '/'} className="marca" translate="no" onClick={cerrarMenu}>
            <img src="/logo.png" alt="" className="marca__logo" width={46} height={58} />
            Sabor Latino
          </Link>

          <button
            ref={botonMenu}
            type="button"
            className="barra__menu-btn"
            aria-expanded={menuAbierto}
            aria-controls="menu-principal"
            aria-label={menuAbierto ? t('Cerrar menú', 'Close menu') : t('Abrir menú', 'Open menu')}
            onClick={() => setMenuAbierto((abierto) => !abierto)}
          >
            <span aria-hidden="true">{menuAbierto ? '✕' : '☰'}</span>
          </button>

          <nav
            id="menu-principal"
            className={`menu ${menuAbierto ? 'menu--abierto' : ''}`}
            aria-label={t('Navegación principal', 'Main navigation')}
          >
            {esPublico ? (
              enlacesPublicos.map((enlace) => (
                <NavLink
                  key={enlace.a}
                  to={enlace.a}
                  end={enlace.a === '/'}
                  className="menu__enlace"
                  onClick={cerrarMenu}
                >
                  {enlace.texto}
                </NavLink>
              ))
            ) : (
              <>
                {esAdmin && (
                  <NavLink to="/panel" className="menu__enlace" onClick={cerrarMenu}>
                    {textos.panel}
                  </NavLink>
                )}
                <a
                  href={obtenerUrlSitioPublico()}
                  className="menu__enlace"
                  target="_blank"
                  rel="noreferrer"
                  onClick={cerrarMenu}
                >
                  {t('Sitio público ↗', 'Public site ↗')}
                </a>
              </>
            )}

            <div
              className="accesibilidad-barra"
              role="region"
              aria-label={t('Preferencias de idioma y accesibilidad', 'Language and accessibility preferences')}
            >
              <div
                className={`accesibilidad-grupo ${expandidoIdioma ? 'accesibilidad-grupo--expandido' : ''}`}
                role="group"
                aria-label={t('Idioma', 'Language')}
              >
                <button
                  type="button"
                  className="accesibilidad-trigger"
                  aria-expanded={expandidoIdioma}
                  aria-label={t('Opciones de idioma', 'Language options')}
                  onClick={() => setExpandidoIdioma((v) => !v)}
                >
                  <span>🌐 {idioma.toUpperCase()}</span>
                  <span className="accesibilidad-flecha" aria-hidden="true">
                    {expandidoIdioma ? '▴' : '▾'}
                  </span>
                </button>
                <div className="accesibilidad-opciones">
                  {IDIOMAS_DISPONIBLES.map((item) => (
                    <button
                      key={item.codigo}
                      type="button"
                      className="accesibilidad-btn"
                      aria-pressed={idioma === item.codigo}
                      aria-label={t(
                        `Cambiar idioma a ${item.nombre.toLowerCase()} (${item.etiqueta})`,
                        `Switch language to ${item.nombreEn} (${item.etiqueta})`
                      )}
                      onClick={() => {
                        setIdioma(item.codigo);
                        setExpandidoIdioma(false);
                      }}
                    >
                      {item.etiqueta} — {t(item.nombre, item.nombreEn)}
                    </button>
                  ))}
                </div>
              </div>

              <div
                className={`accesibilidad-grupo ${expandidoFuente ? 'accesibilidad-grupo--expandido' : ''}`}
                role="group"
                aria-label={t('Tamaño de fuente', 'Font size')}
              >
                <button
                  type="button"
                  className="accesibilidad-trigger"
                  aria-expanded={expandidoFuente}
                  aria-label={t('Opciones de tamaño de fuente', 'Font size options')}
                  onClick={() => setExpandidoFuente((v) => !v)}
                >
                  <span>🗛 {opcionFuenteActual.etiqueta}</span>
                  <span className="accesibilidad-flecha" aria-hidden="true">
                    {expandidoFuente ? '▴' : '▾'}
                  </span>
                </button>
                <div className="accesibilidad-opciones">
                  {OPCIONES_FUENTE.map((op) => (
                    <button
                      key={op.valor}
                      type="button"
                      className="accesibilidad-btn"
                      aria-pressed={escalaFuente === op.valor}
                      aria-label={`${op.etiqueta} — ${t(op.descripcionEs, op.descripcionEn)}`}
                      onClick={() => {
                        setEscalaFuente(op.valor);
                        setExpandidoFuente(false);
                      }}
                    >
                      {op.etiqueta} — {t(op.descripcionEs, op.descripcionEn)}
                    </button>
                  ))}
                </div>
              </div>

              <div
                className={`accesibilidad-grupo ${expandidoContraste ? 'accesibilidad-grupo--expandido' : ''}`}
                role="group"
                aria-label={t('Modo de contraste', 'Contrast mode')}
              >
                <button
                  type="button"
                  className="accesibilidad-trigger"
                  aria-expanded={expandidoContraste}
                  aria-label={t('Opciones de contraste', 'Contrast options')}
                  onClick={() => setExpandidoContraste((v) => !v)}
                >
                  <span>🌓 {t(opcionContrasteActual.cortoEs, opcionContrasteActual.cortoEn)}</span>
                  <span className="accesibilidad-flecha" aria-hidden="true">
                    {expandidoContraste ? '▴' : '▾'}
                  </span>
                </button>
                <div className="accesibilidad-opciones">
                  {OPCIONES_CONTRASTE.map((op) => (
                    <button
                      key={op.valor}
                      type="button"
                      className="accesibilidad-btn"
                      aria-pressed={contraste === op.valor}
                      aria-label={t(op.descEs, op.descEn)}
                      onClick={() => {
                        setContraste(op.valor);
                        setExpandidoContraste(false);
                      }}
                    >
                      {t(op.cortoEs, op.cortoEn)}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="menu__cuenta">
              {esPublico ? (
                <Link to="/solicitar" className="boton boton--accion boton--chico" onClick={cerrarMenu}>
                  {textos.solicitarCotizacion}
                </Link>
              ) : perfil ? (
                <>
                  <span className="menu__enlace">{perfil.nombreCompleto.split(' ')[0]}</span>
                  <button type="button" className="boton boton--borde boton--chico" onClick={salir}>
                    {textos.salir}
                  </button>
                </>
              ) : null}
            </div>
          </nav>
        </div>
      </header>

      <main id="contenido" className="contenido" tabIndex={-1}>
        <Outlet />
      </main>

      <footer className="pie">
        <div className="contenedor">
          {esPublico ? (
            <div className="pie__grid">
              <div className="encabezado encabezado--sin-margen">
                <Link to="/" className="marca" translate="no">
                  <img src="/logo.png" alt="" className="marca__logo" width={46} height={58} />
                  Sabor Latino
                </Link>
                <p>{textos.esloganPie}</p>
                <div className="pie__redes-sociales" aria-label={t('Redes sociales oficiales', 'Official social networks')}>
                  <span className="pie__subtitulo-redes">{t('Síganos en redes', 'Follow us')}</span>
                  <div className="redes-botones">
                    <a
                      href={contenido?.redSocialFacebook || 'https://facebook.com/saborlatinocr'}
                      target="_blank"
                      rel="noreferrer"
                      className="boton-red-social boton-red-social--facebook"
                      aria-label="Facebook · Sabor Latino"
                    >
                      <IconoFacebook />
                    </a>
                    <a
                      href={contenido?.redSocialInstagram || 'https://instagram.com/saborlatinocr'}
                      target="_blank"
                      rel="noreferrer"
                      className="boton-red-social boton-red-social--instagram"
                      aria-label="Instagram · Sabor Latino"
                    >
                      <IconoInstagram />
                    </a>
                    <a
                      href={contenido?.redSocialTikTok || 'https://tiktok.com/@saborlatinocr'}
                      target="_blank"
                      rel="noreferrer"
                      className="boton-red-social boton-red-social--tiktok"
                      aria-label="TikTok · Sabor Latino"
                    >
                      <IconoTikTok />
                    </a>
                    <a
                      href={contenido?.redSocialYouTube || 'https://youtube.com/@saborlatinocr'}
                      target="_blank"
                      rel="noreferrer"
                      className="boton-red-social boton-red-social--youtube"
                      aria-label="YouTube · Sabor Latino"
                    >
                      <IconoYouTube />
                    </a>
                    <a
                      href={
                        contenido?.redSocialWhatsApp
                          ? (contenido.redSocialWhatsApp.startsWith('http')
                              ? contenido.redSocialWhatsApp
                              : `https://wa.me/${contenido.redSocialWhatsApp.replace(/\D/g, '')}`)
                          : 'https://wa.me/50685262096'
                      }
                      target="_blank"
                      rel="noreferrer"
                      className="boton-red-social boton-red-social--whatsapp"
                      aria-label="WhatsApp · Sabor Latino"
                    >
                      <IconoWhatsApp />
                    </a>
                  </div>
                </div>
              </div>
              <div>
                <h2 className="pie__titulo">{textos.explorar}</h2>
                <ul>
                  <li>
                    <Link to="/paquetes">{textos.paquetes}</Link>
                  </li>
                  <li>
                    <Link to="/galeria">{textos.galeria}</Link>
                  </li>
                  <li>
                    <Link to="/preguntas">{textos.preguntasFrecuentes}</Link>
                  </li>
                </ul>
              </div>
              <div>
                <h2 className="pie__titulo">{t('Atención y cotizaciones', 'Support & quotes')}</h2>
                <p>{t('¿Preparamos su próximo evento?', 'Let’s plan your next event.')}</p>
                <ul>
                  <li>
                    <Link to="/solicitar">{textos.solicitarCotizacion}</Link>
                  </li>
                  <li>
                    <a href="https://wa.me/50685262096">WhatsApp · +506 8526 2096</a>
                  </li>
                </ul>
              </div>
            </div>
          ) : (
            <div className="encabezado encabezado--sin-margen">
              <Link to="/panel" className="marca" translate="no">
                <img src="/logo.png" alt="" className="marca__logo" width={46} height={58} />
                Sabor Latino
              </Link>
              <p>
                {t(
                  'Portal Administrativo Privado · Acceso exclusivo para personal autorizado.',
                  {
                    en: 'Private Administrative Portal · Exclusive access for authorized staff.',
                    fr: 'Portail Administratif Privé · Accès exclusif au personnel autorisé.',
                    pt: 'Portal Administrativo Privado · Acesso exclusivo para equipe autorizada.',
                  },
                )}
              </p>
            </div>
          )}
          <p className="pie__legal">
            © {new Date().getFullYear()} <span translate="no">Sabor Latino</span> ·{' '}
            {t('Todos los derechos reservados.', 'All rights reserved.')}
          </p>
        </div>
      </footer>
    </div>
  );
}
