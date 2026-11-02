export type ModoPortal = 'publico' | 'admin';

export function obtenerModoPortal(override?: ModoPortal): ModoPortal {
  if (override) return override;
  const envMode = import.meta.env.VITE_PORTAL_MODE?.toLowerCase();
  if (envMode === 'admin') return 'admin';
  if (envMode === 'public' || envMode === 'publico') return 'publico';

  if (typeof window !== 'undefined') {
    const { hostname, port } = window.location;
    if (hostname.startsWith('admin.') || port === '54527') {
      return 'admin';
    }
  }
  return 'publico';
}

export function obtenerUrlSitioPublico(): string {
  return import.meta.env.VITE_PUBLIC_SITE_URL || 'http://localhost:54526';
}

// Constantes de compilación: con build:public / build:admin Vite las resuelve en el build y el código del otro portal se elimina.
export const SOLO_PUBLICO = import.meta.env.VITE_PORTAL_MODE === 'public' || import.meta.env.VITE_PORTAL_MODE === 'publico';
export const SOLO_ADMIN = import.meta.env.VITE_PORTAL_MODE === 'admin';
