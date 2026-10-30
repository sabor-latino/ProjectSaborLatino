import { createContext, useContext } from 'react';
import type { Perfil, RegistroDatos, Rol } from '../api/tipos';

// Lo que cualquier componente puede usar de la sesión
export interface Sesion {
  perfil: Perfil | null;
  cargando: boolean;
  iniciarSesion: (email: string, password: string) => Promise<void>;
  registrarse: (datos: RegistroDatos) => Promise<void>;
  cerrarSesion: () => void;
  tieneRol: (...roles: Rol[]) => boolean;
}

export const ContextoSesion = createContext<Sesion | null>(null);

// const { perfil, iniciarSesion } = useAuth();
export function useAuth(): Sesion {
  const sesion = useContext(ContextoSesion);
  if (!sesion) {
    throw new Error('useAuth debe usarse dentro de <AuthProvider>.');
  }
  return sesion;
}
