import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { ErrorApi } from '../api/cliente';
import { useAuth } from '../auth/contexto';
import { Acceso } from '../componentes/Acceso';
import { Mensaje } from '../componentes/Estado';
import { usePreferencias } from '../preferencias/PreferenciasContext';
import { validacionAccesible } from '../utilidades/formularios';

const PATRON_CLAVE = '(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[\\W_]).{8,}';

export function Registro() {
  const { perfil, registrarse } = useAuth();
  const { t } = usePreferencias();
  const navegar = useNavigate();

  const [datos, setDatos] = useState({ nombreCompleto: '', email: '', telefono: '', password: '', confirmar: '' });
  const [errores, setErrores] = useState<string[]>([]);
  const [enviando, setEnviando] = useState(false);
  const cajaErrores = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (errores.length > 0) {
      cajaErrores.current?.focus();
    }
  }, [errores]);

  if (perfil) {
    return <Navigate to="/mi-cuenta" replace />;
  }

  const cambiar = (campo: keyof typeof datos, valor: string) => setDatos((d) => ({ ...d, [campo]: valor }));

  const enviar = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (datos.password !== datos.confirmar) {
      setErrores([
        t(
          'Las contraseñas no coinciden. Escríbala igual en los dos campos.',
          'Passwords do not match. Please enter the same password in both fields.',
        ),
      ]);
      return;
    }

    setEnviando(true);
    setErrores([]);

    try {
      await registrarse({
        nombreCompleto: datos.nombreCompleto.trim(),
        email: datos.email.trim(),
        password: datos.password,
        telefono: datos.telefono.trim() || undefined,
      });
      navegar('/mi-cuenta', { replace: true });
    } catch (err) {
      setErrores(
        err instanceof ErrorApi
          ? err.mensajes
          : [t('No se pudo crear la cuenta. Intente de nuevo.', 'Could not create the account. Please try again.')],
      );
      setEnviando(false);
    }
  };

  return (
    <Acceso
      frase={t(
        'Cree su cuenta antes de cotizar para guardar su historial de solicitudes, revisar sus cotizaciones y acumular beneficios de fidelidad.',
        'Create your account before requesting a quote to save your request history, review proposals, and earn loyalty discounts.',
      )}
    >
      <form className="formulario formulario--plano" onSubmit={enviar}>
        <div className="encabezado encabezado--sin-margen">
          <p className="antetitulo">{t('Su cuenta', 'Your account')}</p>
          <h1 className="titulo-seccion">{t('Crear cuenta', 'Create account')}</h1>
        </div>

        <div className="campo">
          <label htmlFor="nombre">{t('Nombre completo', 'Full name')}</label>
          <input
            id="nombre"
            name="nombreCompleto"
            required
            maxLength={150}
            autoComplete="name"
            aria-errormessage="nombre-error"
            {...validacionAccesible}
            value={datos.nombreCompleto}
            onChange={(e) => cambiar('nombreCompleto', e.target.value)}
          />
          <p className="campo__error" id="nombre-error">
            {t('Escriba su nombre completo.', 'Please enter your full name.')}
          </p>
        </div>

        <div className="campo">
          <label htmlFor="email">{t('Correo electrónico', 'Email address')}</label>
          <input
            id="email"
            name="email"
            type="email"
            required
            autoComplete="email"
            spellCheck={false}
            aria-errormessage="email-error"
            {...validacionAccesible}
            value={datos.email}
            onChange={(e) => cambiar('email', e.target.value)}
          />
          <p className="campo__error" id="email-error">
            {t('Escriba un correo electrónico válido.', 'Please enter a valid email address.')}
          </p>
        </div>

        <div className="campo">
          <label htmlFor="telefono">{t('Teléfono (opcional)', 'Phone number (optional)')}</label>
          <input
            id="telefono"
            name="telefono"
            type="tel"
            inputMode="tel"
            autoComplete="tel"
            value={datos.telefono}
            onChange={(e) => cambiar('telefono', e.target.value)}
          />
        </div>

        <div className="campo">
          <label htmlFor="password">{t('Contraseña', 'Password')}</label>
          <p id="reglas-clave" className="campo__ayuda">
            {t(
              'Mínimo 8 caracteres, con mayúscula, minúscula, número y un símbolo (por ejemplo ! o #).',
              'At least 8 characters, including uppercase, lowercase, number, and a symbol (e.g. ! or #).',
            )}
          </p>
          <input
            id="password"
            name="password"
            type="password"
            required
            minLength={8}
            pattern={PATRON_CLAVE}
            autoComplete="new-password"
            aria-describedby="reglas-clave"
            aria-errormessage="password-error"
            {...validacionAccesible}
            value={datos.password}
            onChange={(e) => cambiar('password', e.target.value)}
          />
          <p className="campo__error" id="password-error">
            {t('La contraseña no cumple las reglas indicadas arriba.', 'Password does not meet the requirements above.')}
          </p>
        </div>

        <div className="campo">
          <label htmlFor="confirmar">{t('Repita la contraseña', 'Confirm password')}</label>
          <input
            id="confirmar"
            name="confirmar"
            type="password"
            required
            autoComplete="new-password"
            value={datos.confirmar}
            onChange={(e) => cambiar('confirmar', e.target.value)}
          />
        </div>

        {errores.length > 0 && (
          <div ref={cajaErrores} tabIndex={-1}>
            <Mensaje tipo="error">
              {errores.map((m) => (
                <p key={m}>{m}</p>
              ))}
            </Mensaje>
          </div>
        )}

        <button type="submit" className="boton boton--accion boton--ancho" disabled={enviando}>
          {enviando ? t('Creando cuenta…', 'Creating account…') : t('Crear cuenta', 'Create account')}
        </button>

        <p className="centrado">
          {t('¿Ya tiene cuenta?', 'Already have an account?')}{' '}
          <Link to="/login" className="enlace">
            {t('Inicie sesión', 'Sign in')}
          </Link>
        </p>
      </form>
    </Acceso>
  );
}
