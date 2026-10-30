import { useEffect, useState } from 'react';
import { api, ErrorApi } from '../api/cliente';

interface Estado<T> {
  ruta: string;
  datos: T | null;
  error: string | null;
}

// Pide datos a la API con GET y avisa si está cargando o si hubo error.
// Si la ruta cambia (por ejemplo, un filtro), vuelve a pedir.
export function useDatos<T>(ruta: string) {
  const [estado, setEstado] = useState<Estado<T>>({ ruta: '', datos: null, error: null });

  useEffect(() => {
    let vigente = true;

    api
      .get<T>(ruta)
      .then((datos) => {
        if (vigente) setEstado({ ruta, datos, error: null });
      })
      .catch((e: unknown) => {
        const mensaje = e instanceof ErrorApi ? e.message : 'No se pudieron cargar los datos.';
        if (vigente) setEstado({ ruta, datos: null, error: mensaje });
      });

    // Si la ruta cambia antes de que llegue la respuesta, se ignora la respuesta vieja
    return () => {
      vigente = false;
    };
  }, [ruta]);

  // Está cargando mientras la respuesta guardada no sea de la ruta actual
  const cargando = estado.ruta !== ruta;

  return {
    datos: cargando ? null : estado.datos,
    error: cargando ? null : estado.error,
    cargando,
  };
}
