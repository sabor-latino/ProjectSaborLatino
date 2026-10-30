import type { ReactNode } from 'react';

// Indicador mientras llegan los datos
export function Cargando({ texto = 'Cargando…' }: { texto?: string }) {
  return (
    <p className="cargando" role="status">
      <span className="cargando__punto" aria-hidden="true" />
      {texto}
    </p>
  );
}

// Aviso de éxito, error o información
export function Mensaje({ tipo = 'info', children }: { tipo?: 'info' | 'exito' | 'error'; children: ReactNode }) {
  return (
    <div className={`mensaje mensaje--${tipo}`} role={tipo === 'error' ? 'alert' : 'status'}>
      {children}
    </div>
  );
}
