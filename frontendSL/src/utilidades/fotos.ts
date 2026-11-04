// Fotos reales de eventos y montajes de Sabor Latino
// Cuando un paquete o la galería tengan su propia imagen en la API, se usa esa.

import type { Paquete } from '../api/tipos';

export const FOTOS_EVENTOS_REALES = {
  orquesta: '/eventos/orquesta-en-vivo.jpg',
  pista: '/eventos/pista-baile.jpg',
  karaoke: '/eventos/karaoke-show.jpg',
  sonido: '/eventos/sonido-luces.jpg',
  boda: '/eventos/boda-fiesta.jpg',
  marimba: '/eventos/concierto-marimba.jpg',
};

const IDS = {
  portada: '1524368535928-5b5e00ddc76b',
  karaoke: '1511671782779-c97d3d27a1d4',
  sonido: '1598488035139-bdbb2231ce04',
  secuenciado: '1549213783-8284d0336c4f',
  orquesta: '1516450360452-9312f5e86fc7',
  cotizar: '1522158637959-30385a09e0da',
  cuenta: '1501281668745-f7f57925c3b4',
};

// Arma la URL de Unsplash con el tamaño justo para no descargar de más
export function fotoUnsplash(id: string, ancho: number, alto?: number): string {
  const tam = alto ? `&h=${alto}` : '';
  return `https://images.unsplash.com/photo-${id}?w=${ancho}${tam}&q=70&auto=format&fit=crop`;
}

export const FOTO_PORTADA = FOTOS_EVENTOS_REALES.orquesta;
export const FOTO_COTIZAR = FOTOS_EVENTOS_REALES.sonido;
export const FOTO_CUENTA = fotoUnsplash(IDS.cuenta, 1200);

// Foto del paquete: la suya si la tiene; si no, una real según el tipo de servicio
export function fotoDePaquete(paquete: Paquete): string {
  if (paquete.imagenUrl) {
    return paquete.imagenUrl;
  }

  const texto = `${paquete.servicioNombre} ${paquete.nombre}`.toLowerCase();
  if (texto.includes('karaoke')) return FOTOS_EVENTOS_REALES.karaoke;
  if (texto.includes('sonido') || texto.includes('sonoriz')) return FOTOS_EVENTOS_REALES.sonido;
  if (texto.includes('marimba') || texto.includes('orquesta')) return FOTOS_EVENTOS_REALES.orquesta;
  return FOTOS_EVENTOS_REALES.pista;
}

// Galería de eventos reales de Sabor Latino
export const GALERIA_EJEMPLO = [
  { id: 1, titulo: 'La pista llena', tituloEn: 'A full dance floor', imagenUrl: FOTOS_EVENTOS_REALES.pista, ancho: 700, alto: 467 },
  { id: 2, titulo: 'Bodas', tituloEn: 'Weddings', imagenUrl: FOTOS_EVENTOS_REALES.boda, ancho: 700, alto: 900 },
  { id: 3, titulo: 'Show de karaoke con pantalla Full HD', tituloEn: 'Interactive karaoke party with Full HD screen', imagenUrl: FOTOS_EVENTOS_REALES.karaoke, ancho: 700, alto: 467 },
  { id: 4, titulo: 'Sonorización profesional e iluminación DMX', tituloEn: 'Professional audio reinforcement & DMX lighting', imagenUrl: FOTOS_EVENTOS_REALES.sonido, ancho: 700, alto: 467 },
  { id: 5, titulo: 'Orquesta en vivo en concierto', tituloEn: 'Live orchestra in concert', imagenUrl: FOTOS_EVENTOS_REALES.orquesta, ancho: 700, alto: 467 },
  { id: 6, titulo: 'Son del Gallo: marimba y percusión en vivo', tituloEn: 'Live marimba and Latin percussion show', imagenUrl: FOTOS_EVENTOS_REALES.marimba, ancho: 700, alto: 467 },
];
