import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  api,
  borrarSesion,
  ErrorApi,
  EVENTO_SESION_EXPIRADA,
  guardarSesion,
  obtenerToken,
} from '../api/cliente';
import type { Perfil, RegistroDatos, RespuestaLogin, Rol } from '../api/tipos';
import { ContextoSesion, type Sesion } from './contexto';

// Guarda la sesión (token en localStorage + perfil del usuario) y la comparte con toda la app
export function AuthProvider({ children }: { children: ReactNode }) {
  const [perfil, setPerfil] = useState<Perfil | null>(null);
  // Si al abrir la página ya hay un token guardado, hay que esperar a leer el perfil
  const [cargando, setCargando] = useState(() => obtenerToken() !== null);

  const cerrarSesion = useCallback(() => {
    borrarSesion();
    setPerfil(null);
  }, []);

  // Al abrir la página: si hay token guardado, se pide el perfil para saber quién es
  useEffect(() => {
    if (!obtenerToken()) {
      return;
    }

    api
      .get<Perfil>('/api/cuenta/yo')
      .then(setPerfil)
      .catch(() => borrarSesion())
      .finally(() => setCargando(false));
  }, []);

  // Si la API responde 401 en cualquier pantalla, se cierra la sesión
  useEffect(() => {
    window.addEventListener(EVENTO_SESION_EXPIRADA, cerrarSesion);
    return () => window.removeEventListener(EVENTO_SESION_EXPIRADA, cerrarSesion);
  }, [cerrarSesion]);

  const iniciarSesion = useCallback(async (email: string, password: string) => {
    try {
      const respuesta = await api.post<RespuestaLogin>('/api/identity/login', { email, password });
      guardarSesion(respuesta.accessToken, respuesta.expiresIn);
    } catch (e) {
      // La API responde 401 tanto si la clave está mal como si la cuenta está desactivada
      if (e instanceof ErrorApi && e.estado === 401) {
        throw new ErrorApi(401, ['Correo o contraseña incorrectos, o la cuenta está desactivada. Revise los datos o cree una cuenta nueva.']);
      }
      throw e;
    }

    setPerfil(await api.get<Perfil>('/api/cuenta/yo'));
  }, []);

  const registrarse = useCallback(
    async (datos: RegistroDatos) => {
      await api.post('/api/cuenta/registro', datos);
      // Después de crear la cuenta se entra de una vez
      await iniciarSesion(datos.email, datos.password);
    },
    [iniciarSesion],
  );

  const tieneRol = useCallback(
    (...roles: Rol[]) => perfil !== null && perfil.roles.some((r) => roles.includes(r)),
    [perfil],
  );

  const sesion = useMemo<Sesion>(
    () => ({ perfil, cargando, iniciarSesion, registrarse, cerrarSesion, tieneRol }),
    [perfil, cargando, iniciarSesion, registrarse, cerrarSesion, tieneRol],
  );

  return <ContextoSesion.Provider value={sesion}>{children}</ContextoSesion.Provider>;
}
