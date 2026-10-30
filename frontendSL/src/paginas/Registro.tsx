import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { ErrorApi } from '../api/cliente';
import { useAuth } from '../auth/contexto';
import { Acceso } from '../componentes/Acceso';
import { Mensaje } from '../componentes/Estado';
import { validacionAccesible } from '../utilidades/formularios';

// Mismas reglas que exige la API: 8 caracteres, mayúscula, minúscula, número y símbolo
const PATRON_CLAVE = '(?=.*\\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[\\W_]).{8,}';

export function Registro() {
  const { perfil, registrarse } = useAuth();
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
      setErrores(['Las contraseñas no coinciden. Escríbala igual en los dos campos.']);
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
      setErrores(err instanceof ErrorApi ? err.mensajes : ['No se pudo crear la cuenta. Intente de nuevo.']);
      setEnviando(false);
    }
  };

  return (
    <Acceso frase="Con una cuenta, sus solicitudes quedan guardadas">
      <form className="formulario formulario--plano" onSubmit={enviar}>
        <div className="encabezado encabezado--sin-margen">
          <p className="antetitulo">Su cuenta</p>
          <h1 className="titulo-seccion">Crear cuenta</h1>
        </div>

        <div className="campo">
          <label htmlFor="nombre">Nombre completo</label>
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
            Escriba su nombre.
          </p>
        </div>

        <div className="campo">
          <label htmlFor="email">Correo electrónico</label>
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
            Escriba un correo válido.
          </p>
        </div>

        <div className="campo">
          <label htmlFor="telefono">Teléfono (opcional)</label>
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
          <label htmlFor="password">Contraseña</label>
          {/* Las reglas van arriba del campo para que el teclado del celular no las tape */}
          <p id="reglas-clave" className="campo__ayuda">
            Mínimo 8 caracteres, con mayúscula, minúscula, número y un símbolo (por ejemplo ! o #).
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
            La contraseña no cumple las reglas de arriba.
          </p>
        </div>

        <div className="campo">
          <label htmlFor="confirmar">Repita la contraseña</label>
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
          {enviando ? 'Creando cuenta…' : 'Crear cuenta'}
        </button>

        <p className="centrado">
          ¿Ya tiene cuenta?{' '}
          <Link to="/login" className="enlace">
            Inicie sesión
          </Link>
        </p>
      </form>
    </Acceso>
  );
}
