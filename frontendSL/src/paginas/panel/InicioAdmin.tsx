import { useState } from 'react';
import { api } from '../../api/cliente';
import type { ContenidoInicio } from '../../api/tipos';
import { Cargando, Mensaje } from '../../componentes/Estado';
import { FormularioAccion, TituloPanel } from '../../componentes/panel/Piezas';
import { useDatos } from '../../hooks/useDatos';

const VALORES_INICIALES: ContenidoInicio = {
  etiquetaPortada: 'Bodas · Quince años · Aniversarios · Eventos corporativos',
  etiquetaPortadaEn: 'Weddings · Sweet 15s · Anniversaries · Corporate events',
  tituloPortada: 'Que su fiesta suene en vivo',
  tituloPortadaEn: 'Bring your celebration to life with live sound',
  subtituloPortada:
    'Karaoke interactivo, marimba, discomóvil, orquesta e iluminación escénica en toda Costa Rica. Usted elige el paquete ideal; nosotros llevamos el espectáculo, la ingeniería de sonido y el personal técnico.',
  subtituloPortadaEn:
    'Interactive karaoke, marimba, mobile DJ, live orchestra, and stage lighting across Costa Rica. You choose the package; we bring the show, audio engineering, and technical crew.',
  fotoPortadaUrl: null,
  fotoCotizarUrl: null,
  paso1Titulo: 'Cuéntenos sobre su evento',
  paso1TituloEn: 'Tell us about your event',
  paso1Texto: 'Indique la fecha, el lugar, el aforo estimado y el paquete de entretenimiento que más le interesa.',
  paso1TextoEn: 'Share the date, venue, estimated guest count, and the entertainment package you prefer.',
  paso2Titulo: 'Reciba su cotización formal',
  paso2TituloEn: 'Receive your formal quote',
  paso2Texto:
    'Le enviamos una propuesta detallada en colones con el desglose técnico de audio, iluminación y horas de servicio.',
  paso2TextoEn:
    'We send a clear proposal in colones with the full technical breakdown of sound, lighting, and service hours.',
  paso3Titulo: 'Disfrute la celebración',
  paso3TituloEn: 'Enjoy the celebration',
  paso3Texto:
    'Nuestro equipo técnico llega con anticipación para el montaje y la prueba acústica, garantizando sonido impecable de principio a fin.',
  paso3TextoEn:
    'Our technical crew arrives early for setup and soundcheck, ensuring flawless audio from the very first song.',
  ctaTitulo: 'Aparte su fecha antes de que se agote la agenda',
  ctaTituloEn: 'Reserve your date before the calendar fills up',
  ctaSubtitulo: 'Solicitar su cotización personalizada no tiene ningún costo ni compromiso.',
  ctaSubtituloEn: 'Requesting your personalized quote is completely free and without obligation.',
};

export function InicioAdmin() {
  const { datos, cargando, error, recargar } = useDatos<ContenidoInicio>('/api/contenido-inicio');
  const [aviso, setAviso] = useState<string | null>(null);

  const contenidoActual =
    datos && !Array.isArray(datos) && datos.tituloPortada ? { ...VALORES_INICIALES, ...datos } : VALORES_INICIALES;

  return (
    <section>
      <TituloPanel titulo="Página de Inicio (ES / EN)" />
      <p className="texto-suave">
        Edite los titulares, subtítulos, pasos explicativos, llamadas a la acción e imágenes de la portada principal en
        español e inglés.
      </p>

      {aviso && <Mensaje tipo="exito">{aviso}</Mensaje>}
      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}

      {!cargando && (
        <FormularioContenidoInicio
          key={contenidoActual.tituloPortada}
          inicial={contenidoActual}
          alGuardar={() => {
            setAviso('El contenido de la Página de Inicio se guardó correctamente.');
            recargar();
          }}
        />
      )}
    </section>
  );
}

function FormularioContenidoInicio({
  inicial,
  alGuardar,
}: {
  inicial: ContenidoInicio;
  alGuardar: () => void;
}) {
  const [valores, setValores] = useState<ContenidoInicio>(inicial);

  const cambiar = (campo: keyof ContenidoInicio, valor: string) =>
    setValores((prev) => ({
      ...prev,
      [campo]: campo === 'fotoPortadaUrl' || campo === 'fotoCotizarUrl' ? valor.trim() || null : valor,
    }));

  const guardar = async () => {
    await api.put('/api/contenido-inicio', {
      ...valores,
      fotoPortadaUrl: valores.fotoPortadaUrl?.trim() || null,
      fotoCotizarUrl: valores.fotoCotizarUrl?.trim() || null,
    });
    alGuardar();
  };

  return (
    <div className="caja">
      <FormularioAccion textoBoton="Guardar cambios de Inicio" alEnviar={guardar}>
        <fieldset className="grupo-campos">
          <legend>Portada principal (Hero)</legend>
          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-etiqueta-es">Etiqueta superior (Español) *</label>
              <input
                id="ini-etiqueta-es"
                required
                maxLength={120}
                value={valores.etiquetaPortada}
                onChange={(e) => cambiar('etiquetaPortada', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-etiqueta-en">Etiqueta superior (Inglés) *</label>
              <input
                id="ini-etiqueta-en"
                required
                maxLength={120}
                value={valores.etiquetaPortadaEn}
                onChange={(e) => cambiar('etiquetaPortadaEn', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-titulo-es">Título de portada (Español) *</label>
              <input
                id="ini-titulo-es"
                required
                maxLength={250}
                value={valores.tituloPortada}
                onChange={(e) => cambiar('tituloPortada', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-titulo-en">Título de portada (Inglés) *</label>
              <input
                id="ini-titulo-en"
                required
                maxLength={250}
                value={valores.tituloPortadaEn}
                onChange={(e) => cambiar('tituloPortadaEn', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-subtitulo-es">Subtítulo de portada (Español) *</label>
              <textarea
                id="ini-subtitulo-es"
                required
                maxLength={500}
                value={valores.subtituloPortada}
                onChange={(e) => cambiar('subtituloPortada', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-subtitulo-en">Subtítulo de portada (Inglés) *</label>
              <textarea
                id="ini-subtitulo-en"
                required
                maxLength={500}
                value={valores.subtituloPortadaEn}
                onChange={(e) => cambiar('subtituloPortadaEn', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-foto-portada">URL de fotografía de portada (opcional)</label>
              <input
                id="ini-foto-portada"
                type="url"
                maxLength={500}
                value={valores.fotoPortadaUrl ?? ''}
                onChange={(e) => cambiar('fotoPortadaUrl', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-foto-cotizar">URL de fotografía de banda de cotización (opcional)</label>
              <input
                id="ini-foto-cotizar"
                type="url"
                maxLength={500}
                value={valores.fotoCotizarUrl ?? ''}
                onChange={(e) => cambiar('fotoCotizarUrl', e.target.value)}
              />
            </div>
          </div>
        </fieldset>

        <fieldset className="grupo-campos">
          <legend>Sección “Cómo funciona” (3 pasos)</legend>
          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-paso1-tit-es">Paso 1 — Título (ES) *</label>
              <input
                id="ini-paso1-tit-es"
                required
                maxLength={150}
                value={valores.paso1Titulo}
                onChange={(e) => cambiar('paso1Titulo', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-paso1-tit-en">Paso 1 — Título (EN) *</label>
              <input
                id="ini-paso1-tit-en"
                required
                maxLength={150}
                value={valores.paso1TituloEn}
                onChange={(e) => cambiar('paso1TituloEn', e.target.value)}
              />
            </div>
          </div>
          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-paso1-txt-es">Paso 1 — Descripción (ES) *</label>
              <textarea
                id="ini-paso1-txt-es"
                required
                maxLength={500}
                value={valores.paso1Texto}
                onChange={(e) => cambiar('paso1Texto', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-paso1-txt-en">Paso 1 — Descripción (EN) *</label>
              <textarea
                id="ini-paso1-txt-en"
                required
                maxLength={500}
                value={valores.paso1TextoEn}
                onChange={(e) => cambiar('paso1TextoEn', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-paso2-tit-es">Paso 2 — Título (ES) *</label>
              <input
                id="ini-paso2-tit-es"
                required
                maxLength={150}
                value={valores.paso2Titulo}
                onChange={(e) => cambiar('paso2Titulo', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-paso2-tit-en">Paso 2 — Título (EN) *</label>
              <input
                id="ini-paso2-tit-en"
                required
                maxLength={150}
                value={valores.paso2TituloEn}
                onChange={(e) => cambiar('paso2TituloEn', e.target.value)}
              />
            </div>
          </div>
          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-paso2-txt-es">Paso 2 — Descripción (ES) *</label>
              <textarea
                id="ini-paso2-txt-es"
                required
                maxLength={500}
                value={valores.paso2Texto}
                onChange={(e) => cambiar('paso2Texto', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-paso2-txt-en">Paso 2 — Descripción (EN) *</label>
              <textarea
                id="ini-paso2-txt-en"
                required
                maxLength={500}
                value={valores.paso2TextoEn}
                onChange={(e) => cambiar('paso2TextoEn', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-paso3-tit-es">Paso 3 — Título (ES) *</label>
              <input
                id="ini-paso3-tit-es"
                required
                maxLength={150}
                value={valores.paso3Titulo}
                onChange={(e) => cambiar('paso3Titulo', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-paso3-tit-en">Paso 3 — Título (EN) *</label>
              <input
                id="ini-paso3-tit-en"
                required
                maxLength={150}
                value={valores.paso3TituloEn}
                onChange={(e) => cambiar('paso3TituloEn', e.target.value)}
              />
            </div>
          </div>
          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-paso3-txt-es">Paso 3 — Descripción (ES) *</label>
              <textarea
                id="ini-paso3-txt-es"
                required
                maxLength={500}
                value={valores.paso3Texto}
                onChange={(e) => cambiar('paso3Texto', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-paso3-txt-en">Paso 3 — Descripción (EN) *</label>
              <textarea
                id="ini-paso3-txt-en"
                required
                maxLength={500}
                value={valores.paso3TextoEn}
                onChange={(e) => cambiar('paso3TextoEn', e.target.value)}
              />
            </div>
          </div>
        </fieldset>

        <fieldset className="grupo-campos">
          <legend>Banda final de llamada a la acción (CTA)</legend>
          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-cta-tit-es">Título CTA (Español) *</label>
              <input
                id="ini-cta-tit-es"
                required
                maxLength={200}
                value={valores.ctaTitulo}
                onChange={(e) => cambiar('ctaTitulo', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-cta-tit-en">Título CTA (Inglés) *</label>
              <input
                id="ini-cta-tit-en"
                required
                maxLength={200}
                value={valores.ctaTituloEn}
                onChange={(e) => cambiar('ctaTituloEn', e.target.value)}
              />
            </div>
          </div>
          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-cta-sub-es">Subtítulo CTA (Español) *</label>
              <input
                id="ini-cta-sub-es"
                required
                maxLength={500}
                value={valores.ctaSubtitulo}
                onChange={(e) => cambiar('ctaSubtitulo', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-cta-sub-en">Subtítulo CTA (Inglés) *</label>
              <input
                id="ini-cta-sub-en"
                required
                maxLength={500}
                value={valores.ctaSubtituloEn}
                onChange={(e) => cambiar('ctaSubtituloEn', e.target.value)}
              />
            </div>
          </div>
        </fieldset>
      </FormularioAccion>
    </div>
  );
}
