import { lazy, Suspense, useEffect } from 'react';
import { Cargando } from './componentes/Estado';
import { PreferenciasAutoProvider } from './preferencias/PreferenciasContext';
import { obtenerModoPortal, type ModoPortal } from './utilidades/portal';

// Cada portal vive en su propio fragmento de código. Cuando el build fija VITE_PORTAL_MODE
// (build:public / build:admin), Vite evalúa estas condiciones en compilación y elimina por completo
// el fragmento del otro portal: dist-public no contiene el login ni el panel, y dist-admin no contiene la vitrina.
const FIJADO = import.meta.env.VITE_PORTAL_MODE;
const AppPublico = FIJADO === 'admin' ? null : lazy(() => import('./AppPublico'));
const AppAdmin = FIJADO === 'public' || FIJADO === 'publico' ? null : lazy(() => import('./AppAdmin'));

// - 'publico' (54526 / dist-public): únicamente vitrina pública y solicitud de cotización, sin login ni panel.
// - 'admin' (54527 / dist-admin): acceso privado administrativo y consola de gestión.
export default function App({ modoPortal }: { modoPortal?: ModoPortal } = {}) {
  const modo = obtenerModoPortal(modoPortal);
  const Portal = modo === 'admin' ? (AppAdmin ?? AppPublico) : (AppPublico ?? AppAdmin);

  useEffect(() => {
    if (modo !== 'admin' || typeof document === 'undefined') return;
    let meta = document.head.querySelector('meta[name="robots"]') as HTMLMetaElement | null;
    if (!meta) {
      meta = document.createElement('meta');
      meta.name = 'robots';
      document.head.appendChild(meta);
    }
    meta.content = 'noindex, nofollow';
  }, [modo]);

  return (
    <PreferenciasAutoProvider>
      <Suspense fallback={<Cargando />}>{Portal && <Portal />}</Suspense>
    </PreferenciasAutoProvider>
  );
}
