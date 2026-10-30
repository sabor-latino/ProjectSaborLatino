import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import type { Rol } from '../api/tipos';
import { Cargando, Mensaje } from '../componentes/Estado';
import { useAuth } from './contexto';

interface Props {
  children: ReactNode;
  // Si se indica, solo entran usuarios con alguno de estos roles
  roles?: Rol[];
}

// Envuelve una página que necesita sesión iniciada
export function RutaProtegida({ children, roles }: Props) {
  const { perfil, cargando, tieneRol } = useAuth();
  const ubicacion = useLocation();

  if (cargando) {
    return <Cargando />;
  }

  // Sin sesión: al login, recordando a dónde quería ir
  if (!perfil) {
    return <Navigate to="/login" replace state={{ desde: ubicacion.pathname }} />;
  }

  if (roles && !tieneRol(...roles)) {
    return <Mensaje tipo="error">No tiene permiso para ver esta página.</Mensaje>;
  }

  return children;
}
