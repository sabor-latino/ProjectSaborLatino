import { useState } from 'react';
import { api } from '../../api/cliente';
import type { ContenidoInicio } from '../../api/tipos';
import { Cargando, Mensaje } from '../../componentes/Estado';
import { CampoImagen } from '../../componentes/panel/CampoImagen';
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
  redSocialFacebook: 'https://facebook.com/saborlatinocr',
  redSocialInstagram: 'https://instagram.com/saborlatinocr',
  redSocialTikTok: 'https://tiktok.com/@saborlatinocr',
  redSocialYouTube: 'https://youtube.com/@saborlatinocr',
  redSocialWhatsApp: '50685262096',
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
        Edite los titulares, subtítulos, pasos explicativos, llamadas a la acción, redes sociales e imágenes de la portada principal en
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
            setAviso('El contenido de la Página de Inicio y Redes Sociales se guardó correctamente.');
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
      [campo]:
        campo === 'fotoPortadaUrl' ||
        campo === 'fotoCotizarUrl' ||
        campo === 'redSocialFacebook' ||
        campo === 'redSocialInstagram' ||
        campo === 'redSocialTikTok' ||
        campo === 'redSocialYouTube' ||
        campo === 'redSocialWhatsApp'
          ? valor.trim() || null
          : valor,
    }));

  const guardar = async () => {
    await api.put('/api/contenido-inicio', {
      ...valores,
      fotoPortadaUrl: valores.fotoPortadaUrl?.trim() || null,
      fotoCotizarUrl: valores.fotoCotizarUrl?.trim() || null,
      redSocialFacebook: valores.redSocialFacebook?.trim() || null,
      redSocialInstagram: valores.redSocialInstagram?.trim() || null,
      redSocialTikTok: valores.redSocialTikTok?.trim() || null,
      redSocialYouTube: valores.redSocialYouTube?.trim() || null,
      redSocialWhatsApp: valores.redSocialWhatsApp?.trim() || null,
    });
    alGuardar();
  };

  return (
    <div className="caja">
      <FormularioAccion textoBoton="Guardar cambios de Inicio" alEnviar={guardar}>
        <fieldset className="grupo-campos">
          <legend>Portada Principal — Títulos y Presentación</legend>
          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-eti-es">Etiqueta superior (Español) *</label>
              <input
                id="ini-eti-es"
                required
                maxLength={120}
                value={valores.etiquetaPortada}
                onChange={(e) => cambiar('etiquetaPortada', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-eti-en">Etiqueta superior (Inglés) *</label>
              <input
                id="ini-eti-en"
                required
                maxLength={120}
                value={valores.etiquetaPortadaEn}
                onChange={(e) => cambiar('etiquetaPortadaEn', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-tit-es">Título de portada (Español) *</label>
              <input
                id="ini-tit-es"
                required
                maxLength={250}
                value={valores.tituloPortada}
                onChange={(e) => cambiar('tituloPortada', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-tit-en">Título de portada (Inglés) *</label>
              <input
                id="ini-tit-en"
                required
                maxLength={250}
                value={valores.tituloPortadaEn}
                onChange={(e) => cambiar('tituloPortadaEn', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-sub-es">Subtítulo descriptivo (Español) *</label>
              <textarea
                id="ini-sub-es"
                required
                maxLength={500}
                value={valores.subtituloPortada}
                onChange={(e) => cambiar('subtituloPortada', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-sub-en">Subtítulo descriptivo (Inglés) *</label>
              <textarea
                id="ini-sub-en"
                required
                maxLength={500}
                value={valores.subtituloPortadaEn}
                onChange={(e) => cambiar('subtituloPortadaEn', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-foto-portada">Foto de portada (hero de inicio)</label>
              <CampoImagen
                id="ini-foto-portada"
                nombre="fotoPortadaUrl"
                valor={valores.fotoPortadaUrl || ''}
                alCambiar={(url) => cambiar('fotoPortadaUrl', url)}
                ayuda="Opcional. Foto principal en la portada del sitio web."
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-foto-cotizar">Foto para sección de cotización</label>
              <CampoImagen
                id="ini-foto-cotizar"
                nombre="fotoCotizarUrl"
                valor={valores.fotoCotizarUrl || ''}
                alCambiar={(url) => cambiar('fotoCotizarUrl', url)}
                ayuda="Opcional. Foto de fondo en la sección para solicitar cotización."
              />
            </div>
          </div>
        </fieldset>

        <fieldset className="grupo-campos">
          <legend>Redes Sociales y Canales Oficiales</legend>
          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-red-fb">URL de Facebook</label>
              <input
                id="ini-red-fb"
                type="url"
                maxLength={500}
                placeholder="https://facebook.com/saborlatinocr"
                value={valores.redSocialFacebook || ''}
                onChange={(e) => cambiar('redSocialFacebook', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-red-ig">URL de Instagram</label>
              <input
                id="ini-red-ig"
                type="url"
                maxLength={500}
                placeholder="https://instagram.com/saborlatinocr"
                value={valores.redSocialInstagram || ''}
                onChange={(e) => cambiar('redSocialInstagram', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-red-tt">URL de TikTok</label>
              <input
                id="ini-red-tt"
                type="url"
                maxLength={500}
                placeholder="https://tiktok.com/@saborlatinocr"
                value={valores.redSocialTikTok || ''}
                onChange={(e) => cambiar('redSocialTikTok', e.target.value)}
              />
            </div>
            <div className="campo">
              <label htmlFor="ini-red-yt">URL de YouTube</label>
              <input
                id="ini-red-yt"
                type="url"
                maxLength={500}
                placeholder="https://youtube.com/@saborlatinocr"
                value={valores.redSocialYouTube || ''}
                onChange={(e) => cambiar('redSocialYouTube', e.target.value)}
              />
            </div>
          </div>

          <div className="fila">
            <div className="campo">
              <label htmlFor="ini-red-wa">Teléfono WhatsApp para cotizaciones (código país + número)</label>
              <input
                id="ini-red-wa"
                type="text"
                maxLength={50}
                placeholder="50685262096"
                value={valores.redSocialWhatsApp || ''}
                onChange={(e) => cambiar('redSocialWhatsApp', e.target.value)}
              />
            </div>
          </div>
        </fieldset>

        <fieldset className="grupo-campos">
          <legend>Cómo funciona — Paso 1</legend>
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
        </fieldset>

        <fieldset className="grupo-campos">
          <legend>Cómo funciona — Paso 2</legend>
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
        </fieldset>

        <fieldset className="grupo-campos">
          <legend>Cómo funciona — Paso 3</legend>
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
