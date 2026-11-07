import { useRef, useState } from 'react';
import { subirImagen } from '../../utilidades/imagen';

interface CampoImagenProps {
  id: string;
  nombre: string;
  valor: string;
  alCambiar: (url: string) => void;
  requerido?: boolean;
  ayuda?: string;
}

export function CampoImagen({ id, nombre, valor, alCambiar, requerido, ayuda }: CampoImagenProps) {
  const [subiendo, setSubiendo] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [modoUrl, setModoUrl] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const manejarArchivo = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const archivo = e.target.files?.[0];
    if (!archivo) return;

    setError(null);
    setSubiendo(true);
    try {
      const res = await subirImagen(archivo);
      alCambiar(res.url);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'No se pudo subir la imagen.');
    } finally {
      setSubiendo(false);
      if (inputRef.current) {
        inputRef.current.value = '';
      }
    }
  };

  return (
    <div className="campo-imagen">
      <input
        ref={inputRef}
        type="file"
        id={`${id}-archivo`}
        accept="image/jpeg,image/png,image/webp,image/heic,image/heif,image/*"
        style={{ display: 'none' }}
        onChange={manejarArchivo}
        aria-label="Seleccionar o tomar fotografía"
      />

      <input
        type="hidden"
        id={id}
        name={nombre}
        value={valor}
        required={requerido}
      />

      {valor ? (
        <div className="campo-imagen__previa-card">
          <div className="campo-imagen__imagen-contenedor">
            <img
              src={valor}
              alt="Vista previa"
              className="campo-imagen__miniatura"
              onError={(e) => {
                (e.currentTarget as HTMLImageElement).style.display = 'none';
              }}
            />
            <span className="campo-imagen__badge">✓ Foto lista</span>
          </div>
          <div className="campo-imagen__acciones">
            <button
              type="button"
              className="boton boton--borde boton--chico"
              disabled={subiendo}
              onClick={() => inputRef.current?.click()}
            >
              📷 Cambiar foto
            </button>
            <button
              type="button"
              className="boton boton--peligro boton--chico"
              disabled={subiendo}
              onClick={() => alCambiar('')}
            >
              Quitar
            </button>
          </div>
        </div>
      ) : (
        <div
          className={`campo-imagen__dropzone ${subiendo ? 'campo-imagen__dropzone--cargando' : ''}`}
          role="button"
          tabIndex={0}
          onClick={() => !subiendo && inputRef.current?.click()}
          onKeyDown={(e) => {
            if ((e.key === 'Enter' || e.key === ' ') && !subiendo) {
              e.preventDefault();
              inputRef.current?.click();
            }
          }}
          aria-label="Tomar foto o elegir del celular"
        >
          <div className="campo-imagen__icono-circulo" aria-hidden="true">
            <svg
              viewBox="0 0 24 24"
              width="26"
              height="26"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z" />
              <circle cx="12" cy="13" r="4" />
            </svg>
          </div>
          <div className="campo-imagen__info">
            <strong className="campo-imagen__titulo">
              {subiendo ? 'Optimizando y subiendo foto…' : 'Tomar foto o elegir del celular'}
            </strong>
            <span className="campo-imagen__subtitulo">
              {subiendo ? 'Por favor espere un momento' : 'Toca aquí para abrir la cámara o fototeca'}
            </span>
          </div>
          <button
            type="button"
            className="boton boton--accion boton--chico campo-imagen__boton-accion"
            disabled={subiendo}
            tabIndex={-1}
            aria-hidden="true"
          >
            {subiendo ? 'Subiendo…' : '📷 Seleccionar foto'}
          </button>
        </div>
      )}

      {modoUrl ? (
        <div className="campo-imagen__manual">
          <label htmlFor={`${id}-manual`} className="campo__etiqueta-chica">
            Enlace URL directo:
          </label>
          <div className="campo-imagen__manual-fila">
            <input
              id={`${id}-manual`}
              type="url"
              className="campo__entrada"
              value={valor}
              placeholder="https://ejemplo.com/foto.jpg"
              onChange={(e) => alCambiar(e.target.value)}
            />
            <button
              type="button"
              className="boton boton--borde boton--chico"
              onClick={() => setModoUrl(false)}
            >
              Volver a cámara
            </button>
          </div>
        </div>
      ) : (
        <div className="campo-imagen__pie">
          <button
            type="button"
            className="boton-enlace campo-imagen__alternativa"
            onClick={() => setModoUrl(true)}
          >
            🔗 O escribir un enlace URL manual
          </button>
        </div>
      )}

      {error && (
        <p className="campo__error" role="alert">
          {error}
        </p>
      )}
      {ayuda && <p className="campo__ayuda">{ayuda}</p>}
    </div>
  );
}
