import type { ReactNode } from 'react';
import { FOTO_CUENTA } from '../utilidades/fotos';

// Pantalla dividida para iniciar sesión y registrarse: foto a un lado, formulario al otro
export function Acceso({ frase, children }: { frase: string; children: ReactNode }) {
  return (
    <div className="acceso">
      <div className="acceso__foto">
        <img src={FOTO_CUENTA} alt="" width={1200} height={800} />
        <p className="acceso__frase">{frase}</p>
      </div>
      <div className="acceso__panel">{children}</div>
    </div>
  );
}
