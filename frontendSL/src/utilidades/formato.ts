// Formatos para mostrar datos en pantalla (Costa Rica)

const colones = new Intl.NumberFormat('es-CR', {
  style: 'currency',
  currency: 'CRC',
  maximumFractionDigits: 0,
});

const fecha = new Intl.DateTimeFormat('es-CR', { dateStyle: 'long' });

const fechaHora = new Intl.DateTimeFormat('es-CR', { dateStyle: 'medium', timeStyle: 'short' });

// 120000 → "₡120 000"
export function formatoColones(monto: number): string {
  return colones.format(monto);
}

// "2027-03-15T18:00:00" → "15 de marzo de 2027"
export function formatoFecha(valor: string): string {
  return fecha.format(new Date(valor));
}

// "2027-03-15T18:00:00" → "15 mar 2027, 6:00 p. m."
export function formatoFechaHora(valor: string): string {
  return fechaHora.format(new Date(valor));
}

// Fecha y hora actual en el formato que usa <input type="datetime-local">
export function ahoraParaInput(): string {
  const ahora = new Date();
  ahora.setMinutes(ahora.getMinutes() - ahora.getTimezoneOffset());
  return ahora.toISOString().slice(0, 16);
}

// "2027-03-15T18:00:00" → "2027-03-15T18:00" (valor para <input type="datetime-local">)
export function paraInputFecha(valor: string): string {
  return valor.slice(0, 16);
}
