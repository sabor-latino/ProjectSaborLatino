import { api } from '../api/cliente';

export interface ResultadoSubida {
  url: string;
  rutaRelativa: string;
  nombreOriginal: string;
  tamanoBytes: number;
}

/**
 * Redimensiona y comprime una imagen seleccionada (desde cámara o carrete del celular)
 * a formato WebP usando Canvas nativo del navegador, reduciendo fotos de 10MB a ~150KB.
 */
export async function comprimirImagen(
  archivo: File,
  maxDimension = 1600,
  calidad = 0.82,
): Promise<{ blob: Blob; nombre: string }> {
  return new Promise((resolve, reject) => {
    // Si ya es un webp ligero menor a 500KB, no hace falta recomprimir
    if (archivo.type === 'image/webp' && archivo.size < 500 * 1024) {
      resolve({ blob: archivo, nombre: archivo.name });
      return;
    }

    const lector = new FileReader();
    lector.onerror = () => reject(new Error('No se pudo leer el archivo de imagen.'));
    lector.onload = () => {
      const img = new Image();
      img.onerror = () => reject(new Error('El archivo no es una imagen compatible.'));
      img.onload = () => {
        let ancho = img.naturalWidth || img.width;
        let alto = img.naturalHeight || img.height;

        if (ancho > maxDimension || alto > maxDimension) {
          if (ancho > alto) {
            alto = Math.round((alto * maxDimension) / ancho);
            ancho = maxDimension;
          } else {
            ancho = Math.round((ancho * maxDimension) / alto);
            alto = maxDimension;
          }
        }

        const canvas = document.createElement('canvas');
        canvas.width = ancho;
        canvas.height = alto;
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          reject(new Error('No se pudo inicializar el procesador de imágenes.'));
          return;
        }

        ctx.drawImage(img, 0, 0, ancho, alto);

        const nombreBase = archivo.name.replace(/\.[^/.]+$/, '') || 'foto';
        canvas.toBlob(
          (blob) => {
            if (blob) {
              resolve({ blob, nombre: `${nombreBase}.webp` });
            } else {
              // Fallback a JPEG si el navegador no soporta codificación a WebP
              canvas.toBlob(
                (fallbackBlob) => {
                  if (fallbackBlob) {
                    resolve({ blob: fallbackBlob, nombre: `${nombreBase}.jpg` });
                  } else {
                    reject(new Error('No se pudo procesar la imagen.'));
                  }
                },
                'image/jpeg',
                calidad,
              );
            }
          },
          'image/webp',
          calidad,
        );
      };
      img.src = lector.result as string;
    };
    lector.readAsDataURL(archivo);
  });
}

/**
 * Comprime la imagen y la sube al endpoint nativo de la API de Sabor Latino.
 */
export async function subirImagen(archivo: File): Promise<ResultadoSubida> {
  const { blob, nombre } = await comprimirImagen(archivo);
  const formData = new FormData();
  formData.append('archivo', blob, nombre);

  return await api.postForm<ResultadoSubida>('/api/archivos/subir-imagen', formData);
}
