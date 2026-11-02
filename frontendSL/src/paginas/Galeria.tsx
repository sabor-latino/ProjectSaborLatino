import type { ImagenGaleria } from '../api/tipos';
import { Cargando, Mensaje } from '../componentes/Estado';
import { useDatos } from '../hooks/useDatos';
import { usePreferencias } from '../preferencias/PreferenciasContext';
import { GALERIA_EJEMPLO } from '../utilidades/fotos';

export function Galeria() {
  const { t } = usePreferencias();
  const { datos, cargando, error } = useDatos<ImagenGaleria[]>('/api/galeria');

  const sinFotos = datos !== null && datos.length === 0;

  return (
    <section className="seccion contenedor">
      <div className="encabezado">
        <p className="antetitulo">{t('Galería de eventos', 'Event gallery')}</p>
        <h1 className="titulo-seccion">
          {t('Así se viven nuestras noches en vivo', 'Experience our live celebrations')}
        </h1>
        <p className="intro">
          {t(
            'Cada montaje se adapta a la arquitectura del lugar, el aforo y el estilo musical de sus invitados.',
            'Every setup is tailored to the venue architecture, guest count, and musical style of your celebration.',
          )}
        </p>
      </div>

      {cargando && <Cargando texto={t('Cargando galería…', 'Loading gallery…')} />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {sinFotos && (
        <span className="nota-ejemplo">
          {t(
            'Fotografías de referencia: pronto verá aquí imágenes de nuestros eventos recientes.',
            'Reference photographs: you will soon see images from our recent events here.',
          )}
        </span>
      )}

      {datos && (
        <div className="galeria">
          {sinFotos
            ? GALERIA_EJEMPLO.map((img) => (
                <figure key={img.id} className="galeria__item">
                  <img
                    src={img.imagenUrl}
                    alt={t(img.titulo, img.tituloEn)}
                    width={img.ancho}
                    height={img.alto}
                    loading="lazy"
                    decoding="async"
                  />
                  <figcaption>{t(img.titulo, img.tituloEn)}</figcaption>
                </figure>
              ))
            : datos.map((img) => (
                <figure key={img.id} className="galeria__item">
                  <img
                    src={img.imagenUrl}
                    alt={t(img.titulo, img.tituloEn?.trim() || img.titulo)}
                    width={700}
                    height={467}
                    loading="lazy"
                    decoding="async"
                  />
                  <figcaption>{t(img.titulo, img.tituloEn?.trim() || img.titulo)}</figcaption>
                </figure>
              ))}
        </div>
      )}
    </section>
  );
}
