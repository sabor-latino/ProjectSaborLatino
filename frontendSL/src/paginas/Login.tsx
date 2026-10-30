import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { ErrorApi } from '../api/cliente';
import { useAuth } from '../auth/contexto';
import { Acceso } from '../componentes/Acceso';
import { Mensaje } from '../componentes/Estado';
import { validacionAccesible } from '../utilidades/formularios';

export function Login() {
  const { perfil, iniciarSesion } = useAuth();
  const navegar = useNavigate();
  const ubicacion = useLocation();
  // Página a la que quería ir antes de que le pidieran iniciar sesión
  const destino = (ubicacion.state as { desde?: string } | null)?.desde ?? '/mi-cuenta';

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const cajaError = useRef<HTMLDivElement>(null);

  // El foco va al mensaje de error para que se anuncie
  useEffect(() => {
    if (error) {
      cajaError.current?.focus();
    }
  }, [error]);

  if (perfil) {
    return <Navigate to={destino} replace />;
  }

  const enviar = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setEnviando(true);
    setError(null);

    try {
      await iniciarSesion(email.trim(), password);
      navegar(destino, { replace: true });
    } catch (err) {
      setError(err instanceof ErrorApi ? err.message : 'No se pudo iniciar sesión. Intente de nuevo.');
      setEnviando(false);
    }
  };

  return (
    <Acceso frase="Siga sus cotizaciones y eventos desde un solo lugar">
      <form className="formulario formulario--plano" onSubmit={enviar}>
        <div className="encabezado encabezado--sin-margen">
          <p className="antetitulo">Su cuenta</p>
          <h1 className="titulo-seccion">Iniciar sesión</h1>
        </div>

        <div className="campo">
          <label htmlFor="email">Correo electrónico</label>
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
            Escriba un correo válido.
          </p>
        </div>

        <div className="campo">
          <label htmlFor="password">Contraseña</label>
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
            Escriba su contraseña.
          </p>
        </div>

        {error && (
          <div ref={cajaError} tabIndex={-1}>
            <Mensaje tipo="error">{error}</Mensaje>
          </div>
        )}

        <button type="submit" className="boton boton--accion boton--ancho" disabled={enviando}>
          {enviando ? 'Entrando…' : 'Entrar'}
        </button>

        <p className="centrado">
          ¿No tiene cuenta?{' '}
          <Link to="/registro" className="enlace">
            Cree una aquí
          </Link>
        </p>
      </form>
    </Acceso>
  );
}
