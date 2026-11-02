import type { ImagenGaleria } from '../../api/tipos';
import { Crud } from '../../componentes/panel/Crud';
const texto = (v: unknown) => String(v ?? '').trim();
const textoONulo = (v: unknown) => texto(v) || null;
const numero = (v: unknown) => Number(v) || 0;

export function GaleriaAdmin() {
  return (
    <Crud<ImagenGaleria>
      titulo="Galería"
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
        { nombre: 'titulo', etiqueta: 'Título (Español)', requerido: true, max: 150 },
        { nombre: 'tituloEn', etiqueta: 'Título (Inglés)', max: 150 },
        { nombre: 'imagenUrl', etiqueta: 'Dirección de la imagen', tipo: 'url', requerido: true, max: 500 },
        { nombre: 'orden', etiqueta: 'Orden', tipo: 'numero', min: 0, max: 1000, ayuda: 'Las de menor número salen primero.' },
        { nombre: 'activo', etiqueta: 'Activa (visible en el sitio)', tipo: 'check' },
      ]}
      nuevo={{ titulo: '', tituloEn: '', imagenUrl: '', orden: 0, activo: true }}
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
