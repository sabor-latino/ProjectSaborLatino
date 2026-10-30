// Fotos de ejemplo (Unsplash) mientras el negocio sube las suyas.
// Cuando un paquete o la galería tengan su propia imagen en la API, se usa esa.

import type { Paquete } from '../api/tipos';

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

export const FOTO_PORTADA = fotoUnsplash(IDS.portada, 2000);
export const FOTO_COTIZAR = fotoUnsplash(IDS.cotizar, 1800);
export const FOTO_CUENTA = fotoUnsplash(IDS.cuenta, 1200);

// Foto del paquete: la suya si la tiene; si no, una de ejemplo según el tipo de servicio
export function fotoDePaquete(paquete: Paquete): string {
  if (paquete.imagenUrl) {
    return paquete.imagenUrl;
  }

  const texto = `${paquete.servicioNombre} ${paquete.nombre}`.toLowerCase();
  if (texto.includes('karaoke')) return fotoUnsplash(IDS.karaoke, 800, 600);
  if (texto.includes('sonido') || texto.includes('sonoriz')) return fotoUnsplash(IDS.sonido, 800, 600);
  if (texto.includes('marimba') || texto.includes('orquesta')) return fotoUnsplash(IDS.orquesta, 800, 600);
  return fotoUnsplash(IDS.secuenciado, 800, 600);
}

// Galería de ejemplo para cuando todavía no hay fotos reales cargadas
export const GALERIA_EJEMPLO = [
  { id: 1, titulo: 'La pista llena', imagenUrl: fotoUnsplash('1504609813442-a8924e83f76e', 700), ancho: 700, alto: 467 },
  { id: 2, titulo: 'Bodas', imagenUrl: fotoUnsplash('1519741497674-611481863552', 700, 900), ancho: 700, alto: 900 },
  { id: 3, titulo: 'El momento del confeti', imagenUrl: fotoUnsplash('1492684223066-81342ee5ff30', 700), ancho: 700, alto: 467 },
  { id: 4, titulo: 'Quince años y cumpleaños', imagenUrl: fotoUnsplash('1530103862676-de8c9debad1d', 700, 860), ancho: 700, alto: 860 },
  { id: 5, titulo: 'Voces en vivo', imagenUrl: fotoUnsplash('1493225457124-a3eb161ffa5f', 700), ancho: 700, alto: 467 },
  { id: 6, titulo: 'El primer baile', imagenUrl: fotoUnsplash('1465495976277-4387d4b0b4c6', 700), ancho: 700, alto: 467 },
];
