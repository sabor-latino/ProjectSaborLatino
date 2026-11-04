import type { Idioma } from '../preferencias/PreferenciasContext';

// Formatos para mostrar datos en pantalla (Costa Rica)

const colones = new Intl.NumberFormat('es-CR', {
  style: 'currency',
  currency: 'CRC',
  maximumFractionDigits: 0,
});

const LOCALES: Record<Idioma, string> = {
  es: 'es-CR',
  en: 'en-US',
  fr: 'fr-FR',
  pt: 'pt-BR',
};

// 120000 → "₡120 000"
export function formatoColones(monto: number): string {
  return colones.format(monto);
}

// "2027-03-15T18:00:00" → "15 de marzo de 2027" / "March 15, 2027" / "15 mars 2027"
export function formatoFecha(valor: string, idioma: Idioma = 'es'): string {
  const loc = LOCALES[idioma] ?? 'es-CR';
  return new Intl.DateTimeFormat(loc, { dateStyle: 'long' }).format(new Date(valor));
}

// "2027-03-15T18:00:00" → "15 mar 2027, 6:00 p. m."
export function formatoFechaHora(valor: string, idioma: Idioma = 'es'): string {
  const loc = LOCALES[idioma] ?? 'es-CR';
  return new Intl.DateTimeFormat(loc, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(valor));
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
