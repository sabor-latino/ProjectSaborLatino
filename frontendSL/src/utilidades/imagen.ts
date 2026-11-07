import { api } from '../api/cliente';

export interface ResultadoSubida {
  url: string;
  rutaRelativa: string;
  nombreOriginal: string;
  tamanoBytes: number;
}

/**
 * Carga una imagen de forma compatible con Safari (iOS/macOS), Chrome, Firefox y Android.
 * Prioriza createImageBitmap cuando está disponible (soporta HEIC nativo en iOS Safari)
 * y recurre a Image con URL.createObjectURL como fallback ligero.
 */
async function cargarDimensionesYFuente(
  archivo: File,
): Promise<{ fuente: CanvasImageSource; ancho: number; alto: number; liberar: () => void }> {
  if (typeof createImageBitmap === 'function') {
    try {
      const bitmap = await createImageBitmap(archivo);
      return {
        fuente: bitmap,
        ancho: bitmap.width,
        alto: bitmap.height,
        liberar: () => {
          if (typeof bitmap.close === 'function') {
            bitmap.close();
          }
        },
      };
    } catch {
      // Si el formato específico no fue reconocido por createImageBitmap, continuar con Image
    }
  }

  return new Promise((resolve, reject) => {
    let url = '';
    try {
      url = URL.createObjectURL(archivo);
    } catch {
      // Fallback extremo si createObjectURL no estuviera disponible en el entorno
    }

    const img = new Image();
    const liberar = () => {
      if (url) {
        URL.revokeObjectURL(url);
      }
    };

    img.onload = () => {
      resolve({
        fuente: img,
        ancho: img.naturalWidth || img.width,
        alto: img.naturalHeight || img.height,
        liberar,
      });
    };

    img.onerror = () => {
      liberar();
      // Si falló por URL, intentar lectura con FileReader por compatibilidad de navegadores antiguos
      const lector = new FileReader();
      lector.onerror = () => reject(new Error('No se pudo leer el archivo de imagen.'));
      lector.onload = () => {
        const imgFallback = new Image();
        imgFallback.onload = () => {
          resolve({
            fuente: imgFallback,
            ancho: imgFallback.naturalWidth || imgFallback.width,
            alto: imgFallback.naturalHeight || imgFallback.height,
            liberar: () => {},
          });
        };
        imgFallback.onerror = () => reject(new Error('El archivo no es una imagen compatible o Safari no pudo leerla.'));
        imgFallback.src = lector.result as string;
      };
      lector.readAsDataURL(archivo);
    };

    if (url) {
      img.src = url;
    } else {
      img.onerror(new Event('error'));
    }
  });
}

/**
 * Exporta el canvas asegurando que la extensión coincida exactamente con los bytes generados.
 * En Chrome/Firefox: produce WebP real.
 * En Safari (que devuelve PNG o null al pedir WebP en toBlob): produce JPEG universal compatible.
 */
function exportarCanvas(
  canvas: HTMLCanvasElement,
  calidad = 0.82,
): Promise<{ blob: Blob; extension: string }> {
  return new Promise((resolve, reject) => {
    try {
      // 1. Intentar WebP primero
      canvas.toBlob(
        (blobWebp) => {
          if (blobWebp && blobWebp.type === 'image/webp' && blobWebp.size > 0) {
            resolve({ blob: blobWebp, extension: '.webp' });
            return;
          }

          // 2. Si Safari devuelve null o devuelve image/png por falta de soporte de codificación WebP,
          // exportar a JPEG universal de alta compresión.
          try {
            canvas.toBlob(
              (blobJpeg) => {
                if (blobJpeg && blobJpeg.size > 0) {
                  resolve({ blob: blobJpeg, extension: '.jpg' });
                } else {
                  reject(new Error('No se pudo procesar la imagen en Safari ni en el navegador.'));
                }
              },
              'image/jpeg',
              calidad,
            );
          } catch (errJpeg) {
            reject(errJpeg instanceof Error ? errJpeg : new Error('Error al exportar JPEG'));
          }
        },
        'image/webp',
        calidad,
      );
    } catch (errWebp) {
      reject(errWebp instanceof Error ? errWebp : new Error('Error al exportar WebP'));
    }
  });
}

/**
 * Redimensiona y comprime una imagen seleccionada (desde cámara o fototeca de Safari o Chrome)
 * reduciendo fotos de 8-12 MB a ~150 KB antes de la subida.
 */
export async function comprimirImagen(
  archivo: File,
  maxDimension = 1600,
  calidad = 0.82,
): Promise<{ blob: Blob; nombre: string }> {
  // Si ya es un webp ligero menor a 500 KB, no hace falta recomprimir
  if (archivo.type === 'image/webp' && archivo.size < 500 * 1024) {
    return { blob: archivo, nombre: archivo.name };
  }

  const { fuente, ancho: anchoOrig, alto: altoOrig, liberar } = await cargarDimensionesYFuente(archivo);

  try {
    let ancho = anchoOrig;
    let alto = altoOrig;

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
      throw new Error('No se pudo inicializar el procesador de imágenes.');
    }

    ctx.drawImage(fuente, 0, 0, ancho, alto);

    const { blob, extension } = await exportarCanvas(canvas, calidad);
    const nombreBase = archivo.name.replace(/\.[^/.]+$/, '') || 'foto';
    const nombreFinal = `${nombreBase}${extension}`;

    return { blob, nombre: nombreFinal };
  } finally {
    liberar();
  }
}

/**
 * Comprime la imagen y la sube al endpoint nativo de la API de Sabor Latino.
 * Si la compresión en el cliente falla (por ejemplo por limitaciones de memoria de Safari
 * o formatos no decodificables en canvas), recurre de inmediato a subir el archivo original
 * para garantizar que la fotografía nunca se pierda ni bloquee al usuario.
 */
export async function subirImagen(archivo: File): Promise<ResultadoSubida> {
  let blobParaSubir: Blob = archivo;
  let nombreParaSubir: string = archivo.name;

  try {
    const comprimido = await comprimirImagen(archivo);
    blobParaSubir = comprimido.blob;
    nombreParaSubir = comprimido.nombre;
  } catch (errorCompresion) {
    console.warn('La compresión en el cliente falló o fue omitida; subiendo archivo original:', errorCompresion);
    if (!/\.(jpg|jpeg|png|webp)$/i.test(nombreParaSubir)) {
      if (archivo.type === 'image/png') {
        nombreParaSubir = `${nombreParaSubir.replace(/\.[^/.]+$/, '') || 'foto'}.png`;
      } else {
        nombreParaSubir = `${nombreParaSubir.replace(/\.[^/.]+$/, '') || 'foto'}.jpg`;
      }
    }
  }

  const formData = new FormData();
  formData.append('archivo', blobParaSubir, nombreParaSubir);

  return await api.postForm<ResultadoSubida>('/api/archivos/subir-imagen', formData);
}
