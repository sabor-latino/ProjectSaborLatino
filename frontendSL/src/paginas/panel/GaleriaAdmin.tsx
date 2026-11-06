import type { ImagenGaleria } from '../../api/tipos';
import { Crud } from '../../componentes/panel/Crud';
const texto = (v: unknown) => String(v ?? '').trim();
const textoONulo = (v: unknown) => texto(v) || null;
const numero = (v: unknown) => Number(v) || 0;

export function GaleriaAdmin() {
  return (
    <Crud<ImagenGaleria>
      titulo="Galería"
      textoBotonNuevo="📷 + Subir fotografía"
      ruta="/api/galeria"
      columnas={[
        {
          etiqueta: 'Foto',
          valor: (g) => <img className="miniatura" src={g.imagenUrl} alt="" width={96} height={64} loading="lazy" />,
        },
        { etiqueta: 'Título', valor: (g) => <strong>{g.titulo}</strong> },
        { etiqueta: 'Título (Inglés)', valor: (g) => g.tituloEn || '—' },
        { etiqueta: 'Orden', valor: (g) => <span className="numero">{g.orden}</span> },
      ]}
      campos={[
        {
          nombre: 'imagenUrl',
          etiqueta: 'Fotografía del evento',
          tipo: 'imagen',
          requerido: true,
          max: 500,
          ayuda: 'Tome una foto con su celular o elija una imagen de su galería para comprimirla y subirla.',
        },
        { nombre: 'titulo', etiqueta: 'Título (Español)', requerido: true, max: 150 },
        { nombre: 'tituloEn', etiqueta: 'Título (Inglés)', max: 150 },
        { nombre: 'orden', etiqueta: 'Orden', tipo: 'numero', min: 0, max: 1000, ayuda: 'Las de menor número salen primero.' },
        { nombre: 'activo', etiqueta: 'Activa (visible en el sitio)', tipo: 'check' },
      ]}
      nuevo={{ titulo: '', tituloEn: '', imagenUrl: '', orden: 0, activo: true }}
      cabeceraExtra={(abrirNuevo) => (
        <div
          className="galeria-admin__banner tarjeta"
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: '1rem',
            padding: '1.25rem',
            margin: '0 0 1.5rem',
            border: '1px solid var(--color-borde, #e2e8f0)',
            borderRadius: '12px',
          }}
        >
          <div>
            <h3 style={{ margin: 0, fontSize: '1.05rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <span>📷</span>
              <span>Subida de fotos para la Galería de Eventos</span>
            </h3>
            <p className="texto-suave" style={{ margin: '0.25rem 0 0', fontSize: '0.9rem' }}>
              Capture fotos en vivo desde su teléfono celular o suba fotografías de bodas, fiestas y conciertos.
            </p>
          </div>
          <button
            type="button"
            className="boton boton--accion"
            onClick={abrirNuevo}
          >
            📷 Subir foto desde el celular
          </button>
        </div>
      )}
      mensajeVacio={(abrirNuevo) => (
        <div
          className="tarjeta"
          style={{
            textAlign: 'center',
            padding: '3rem 1.5rem',
            borderRadius: '12px',
            border: '2px dashed var(--color-borde, #cbd5e1)',
            margin: '1.5rem 0',
          }}
        >
          <div style={{ fontSize: '3rem', marginBottom: '0.75rem' }}>📷</div>
          <h3 style={{ margin: '0 0 0.5rem', fontSize: '1.25rem' }}>No hay fotografías en la galería todavía</h3>
          <p className="texto-suave" style={{ maxWidth: '440px', margin: '0 auto 1.5rem', fontSize: '0.95rem' }}>
            Estrene la galería subiendo su primera foto. Puede tomarla al instante con la cámara del celular o elegir una de su dispositivo.
          </p>
          <button
            type="button"
            className="boton boton--accion"
            onClick={abrirNuevo}
          >
            📷 Tomar foto o elegir del celular
          </button>
        </div>
      )}
      aApi={(v) => ({
        titulo: texto(v.titulo),
        tituloEn: textoONulo(v.tituloEn),
        imagenUrl: texto(v.imagenUrl),
        orden: numero(v.orden),
        activo: Boolean(v.activo),
      })}
    />
  );
}
