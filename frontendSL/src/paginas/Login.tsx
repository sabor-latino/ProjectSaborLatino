import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { ErrorApi } from '../api/cliente';
import { useAuth } from '../auth/contexto';
import { Acceso } from '../componentes/Acceso';
import { Mensaje } from '../componentes/Estado';
import { usePreferencias } from '../preferencias/PreferenciasContext';
import { validacionAccesible } from '../utilidades/formularios';

export type TipoAcceso = 'admin' | 'empleado';

export function Login() {
  const { perfil, iniciarSesion, cerrarSesion, tieneRol } = useAuth();
  const { t } = usePreferencias();
  const ubicacion = useLocation();
  const rawDestino = (ubicacion.state as { desde?: string } | null)?.desde;
  const destino = rawDestino && rawDestino.startsWith('/') && !rawDestino.startsWith('//') ? rawDestino : '/panel';

  const [tipoAcceso, setTipoAcceso] = useState<TipoAcceso>('admin');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [intentoRealizado, setIntentoRealizado] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const cajaError = useRef<HTMLDivElement>(null);

  const esAdmin = tieneRol('Administrador', 'Superadministrador');
  const esTrabajador = tieneRol('Trabajador');
  const esAutorizado = esAdmin || esTrabajador;

  const errorMostrado =
    error ??
    (intentoRealizado && !esAutorizado
      ? t(
          'Acceso restringido exclusivamente al personal administrativo y técnico autorizado.',
          'Access restricted exclusively to authorized administrative and technical staff.',
        )
      : null);

  useEffect(() => {
    if (perfil && !esAutorizado) {
      cerrarSesion();
    }
  }, [perfil, esAutorizado, cerrarSesion]);

  useEffect(() => {
    if (errorMostrado) {
      cajaError.current?.focus();
    }
  }, [errorMostrado]);

  if (perfil && esAutorizado) {
    return <Navigate to={destino} replace />;
  }

  const enviar = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setEnviando(true);
    setError(null);
    setIntentoRealizado(false);

    try {
      await iniciarSesion(email.trim(), password);
      setIntentoRealizado(true);
      setEnviando(false);
    } catch (err) {
      setError(
        err instanceof ErrorApi
          ? err.message
          : t('No se pudo iniciar sesión. Intente de nuevo.', 'Could not sign in. Please try again.'),
      );
      setEnviando(false);
    }
  };

  return (
    <Acceso
      frase={
        tipoAcceso === 'admin'
          ? t(
              'Consola privada de administración de catálogo, promociones de temporada, cotizaciones y logística de eventos de Sabor Latino.',
              'Private administration console for Sabor Latino catalog, seasonal promotions, quotes, and event logistics.',
            )
          : t(
              'Portal interno para el personal técnico y operativo de Sabor Latino. Consulte sus asignaciones de eventos y gestione montajes en tiempo real.',
              'Internal portal for Sabor Latino technical and operations staff. Review event assignments and manage real-time setups.',
            )
      }
    >
      <form className="formulario formulario--plano" onSubmit={enviar}>
        <div className="encabezado encabezado--sin-margen">
          <p className="antetitulo">{t('Acceso interno restringido', 'Restricted internal access')}</p>
          <h1 className="titulo-seccion">
            {tipoAcceso === 'admin'
              ? t('Portal Administrativo', { en: 'Administrative Portal', fr: 'Portail Administratif', pt: 'Portal Administrativo' })
              : t('Portal de Personal y Empleados', 'Staff & Employee Portal')}
          </h1>
        </div>

        <div className="filtros" role="tablist" aria-label={t('Modalidad de acceso al portal privado', 'Private portal access mode')}>
          <button
            type="button"
            role="tab"
            aria-selected={tipoAcceso === 'admin'}
            className="chip"
            onClick={() => {
              setTipoAcceso('admin');
              setError(null);
              setIntentoRealizado(false);
            }}
          >
            {t('Acceso Administrativo', 'Administrative Access')}
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={tipoAcceso === 'empleado'}
            className="chip"
            onClick={() => {
              setTipoAcceso('empleado');
              setError(null);
              setIntentoRealizado(false);
            }}
          >
            {t('Acceso Empleado / Personal', 'Employee / Staff Access')}
          </button>
        </div>

        <p className="campo__ayuda" style={{ margin: '0 0 var(--esp-2)' }}>
          {tipoAcceso === 'admin'
            ? t(
                'Acceso para Administradores y Superadministradores para la gestión integral de catálogo, promociones, eventos y usuarios.',
                'Access for Administrators and Superadministrators to manage catalog, promotions, events, and users.',
              )
            : t(
                'Acceso para Trabajadores y personal técnico para consultar eventos asignados y actualizar estados en vivo.',
                'Access for Workers and technical staff to check assigned events and update real-time status.',
              )}
        </p>

        <div className="campo">
          <label htmlFor="email">{t('Correo electrónico', 'Email address')}</label>
          <input
            id="email"
            name="email"
            type="email"
            required
            autoComplete="username"
            spellCheck={false}
            aria-errormessage="email-error"
            {...validacionAccesible}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          <p className="campo__error" id="email-error">
            {t('Escriba un correo electrónico válido.', 'Please enter a valid email address.')}
          </p>
        </div>

        <div className="campo">
          <label htmlFor="password">{t('Contraseña', 'Password')}</label>
          <input
            id="password"
            name="password"
            type="password"
            required
            autoComplete="current-password"
            aria-errormessage="password-error"
            {...validacionAccesible}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <p className="campo__error" id="password-error">
            {t('Escriba su contraseña.', 'Please enter your password.')}
          </p>
        </div>

        {errorMostrado && (
          <div ref={cajaError} tabIndex={-1}>
            <Mensaje tipo="error">{errorMostrado}</Mensaje>
          </div>
        )}

        <button type="submit" className="boton boton--accion boton--ancho" disabled={enviando}>
          {enviando ? t('Entrando…', 'Signing in…') : t('Entrar', 'Sign in')}
        </button>
      </form>
    </Acceso>
  );
}
