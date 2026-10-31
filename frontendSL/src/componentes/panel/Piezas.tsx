import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { ErrorApi } from '../../api/cliente';
import { nombreEstado, tonoEstado } from '../../utilidades/estados';
import { Mensaje } from '../Estado';

// Etiqueta de color con el estado (Pendiente, Aceptada, En montaje…)
export function Insignia({ estado }: { estado: string }) {
  return <span className={`insignia insignia--${tonoEstado(estado)}`}>{nombreEstado(estado)}</span>;
}

// Ventana para formularios, con <dialog> nativo: Esc la cierra y el foco queda adentro
export function Dialogo({
  titulo,
  abierto,
  alCerrar,
  children,
}: {
  titulo: string;
  abierto: boolean;
  alCerrar: () => void;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialogo = ref.current;
    if (!dialogo) return;
    if (abierto && !dialogo.open) dialogo.showModal();
    if (!abierto && dialogo.open) dialogo.close();
  }, [abierto]);

  return (
    <dialog ref={ref} className="dialogo" onClose={alCerrar} aria-labelledby="dialogo-titulo">
      {abierto && (
        <>
          <div className="dialogo__encabezado">
            <h2 id="dialogo-titulo">{titulo}</h2>
            <button type="button" className="dialogo__cerrar" onClick={alCerrar} aria-label="Cerrar">
              ✕
            </button>
          </div>
          {children}
        </>
      )}
    </dialog>
  );
}

// Formulario que llama a la API: muestra "Guardando…" y los errores en español
export function FormularioAccion({
  textoBoton,
  alEnviar,
  children,
  peligro = false,
}: {
  textoBoton: string;
  alEnviar: () => Promise<void>;
  children?: ReactNode;
  peligro?: boolean;
}) {
  const [enviando, setEnviando] = useState(false);
  const [errores, setErrores] = useState<string[]>([]);

  const enviar = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setEnviando(true);
    setErrores([]);
    try {
      await alEnviar();
    } catch (error) {
      setErrores(error instanceof ErrorApi ? error.mensajes : ['No se pudo completar la acción.']);
    } finally {
      setEnviando(false);
    }
  };

  return (
    <form className="formulario-panel" onSubmit={enviar}>
      {children}
      {errores.length > 0 && (
        <Mensaje tipo="error">
          {errores.map((m) => (
            <p key={m}>{m}</p>
          ))}
        </Mensaje>
      )}
      <button
        type="submit"
        className={`boton boton--chico ${peligro ? 'boton--peligro' : 'boton--accion'}`}
        disabled={enviando}
      >
        {enviando ? 'Guardando…' : textoBoton}
      </button>
    </form>
  );
}

// Tarjeta con un número grande (resumen del panel)
export function Cifra({ etiqueta, valor, nota }: { etiqueta: string; valor: ReactNode; nota?: string }) {
  return (
    <div className="cifra">
      <span className="cifra__etiqueta">{etiqueta}</span>
      <strong className="cifra__valor">{valor}</strong>
      {nota && <span className="cifra__nota">{nota}</span>}
    </div>
  );
}

// Encabezado de cada sección del panel
export function TituloPanel({ titulo, children }: { titulo: string; children?: ReactNode }) {
  return (
    <div className="panel__titulo">
      <h1>{titulo}</h1>
      {children && <div className="acciones">{children}</div>}
    </div>
  );
}

