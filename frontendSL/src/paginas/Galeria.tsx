import type { ImagenGaleria } from '../api/tipos';
import { Cargando, Mensaje } from '../componentes/Estado';
import { useDatos } from '../hooks/useDatos';
import { GALERIA_EJEMPLO } from '../utilidades/fotos';

export function Galeria() {
  const { datos, cargando, error } = useDatos<ImagenGaleria[]>('/api/galeria');

  // Mientras el negocio no suba fotos, se muestran fotos de ejemplo marcadas como tales
  const sinFotos = datos !== null && datos.length === 0;

  return (
    <section className="seccion contenedor">
      <div className="encabezado">
        <p className="antetitulo">Galería</p>
        <h1 className="titulo-seccion">Así se viven nuestras noches</h1>
      </div>

      {cargando && <Cargando />}
      {error && <Mensaje tipo="error">{error}</Mensaje>}
      {sinFotos && <span className="nota-ejemplo">Fotos de ejemplo: pronto verá aquí las de nuestros eventos</span>}

      {datos && (
        <div className="galeria">
          {sinFotos
            ? GALERIA_EJEMPLO.map((img) => (
                <figure key={img.id} className="galeria__item">
                  <img src={img.imagenUrl} alt={img.titulo} width={img.ancho} height={img.alto} loading="lazy" decoding="async" />
                  <figcaption>{img.titulo}</figcaption>
                </figure>
              ))
            : datos.map((img) => (
                <figure key={img.id} className="galeria__item">
                  <img src={img.imagenUrl} alt={img.titulo} width={700} height={467} loading="lazy" decoding="async" />
                  <figcaption>{img.titulo}</figcaption>
                </figure>
              ))}
        </div>
      )}
    </section>
  );
}
