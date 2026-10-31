// Nombres para mostrar y color de cada estado que devuelve la API

const NOMBRES: Record<string, string> = {
  Pendiente: 'Pendiente',
  Cotizada: 'Cotizada',
  Aceptada: 'Aceptada',
  Rechazada: 'Rechazada',
  Cancelada: 'Cancelada',
  Enviada: 'Enviada',
  Vencida: 'Vencida',
  Programado: 'Programado',
  EnMontaje: 'En montaje',
  EnServicio: 'En servicio',
  Finalizado: 'Finalizado',
  Cancelado: 'Cancelado',
};

// ok = verde, alerta = coral, apagado = gris, info = ciruela
const TONOS: Record<string, 'ok' | 'alerta' | 'apagado' | 'info'> = {
  Pendiente: 'alerta',
  Cotizada: 'info',
  Aceptada: 'ok',
  Rechazada: 'apagado',
  Cancelada: 'apagado',
  Enviada: 'alerta',
  Vencida: 'apagado',
  Programado: 'info',
  EnMontaje: 'alerta',
  EnServicio: 'alerta',
  Finalizado: 'ok',
  Cancelado: 'apagado',
};

export function nombreEstado(estado: string): string {
  return NOMBRES[estado] ?? estado;
}

export function tonoEstado(estado: string): string {
  return TONOS[estado] ?? 'info';
}

// Orden de los estados de un evento: solo se avanza al siguiente
const ORDEN_EVENTO = ['Programado', 'EnMontaje', 'EnServicio', 'Finalizado'];

export function siguienteEstadoEvento(estado: string): string | null {
  const i = ORDEN_EVENTO.indexOf(estado);
  return i >= 0 && i < ORDEN_EVENTO.length - 1 ? ORDEN_EVENTO[i + 1] : null;
}

export const ESTADOS_SOLICITUD = ['Pendiente', 'Cotizada', 'Aceptada', 'Rechazada', 'Cancelada'];
export const ESTADOS_EVENTO = ['Programado', 'EnMontaje', 'EnServicio', 'Finalizado', 'Cancelado'];
