export type Idioma = 'es' | 'en' | 'fr' | 'pt';

export interface IdiomaConfig {
  codigo: Idioma;
  etiqueta: string;
  nombre: string;
  nombreEn: string;
  bandera: string;
}

export const IDIOMAS_DISPONIBLES: readonly IdiomaConfig[] = [
  { codigo: 'es', etiqueta: 'ES', nombre: 'Español', nombreEn: 'Spanish', bandera: '🇨🇷' },
  { codigo: 'en', etiqueta: 'EN', nombre: 'Inglés', nombreEn: 'English', bandera: '🇺🇸' },
  { codigo: 'fr', etiqueta: 'FR', nombre: 'Francés', nombreEn: 'French', bandera: '🇫🇷' },
  { codigo: 'pt', etiqueta: 'PT', nombre: 'Portugués', nombreEn: 'Portuguese', bandera: '🇧🇷' },
] as const;

export type TraduccionesInline = Partial<Record<Idioma, string>>;

export interface DiccionarioEstatico {
  saltarContenido: string;
  inicio: string;
  paquetes: string;
  galeria: string;
  preguntas: string;
  panel: string;
  iniciarSesion: string;
  salir: string;
  solicitarCotizacion: string;
  explorar: string;
  suCuenta: string;
  miCuenta: string;
  crearCuenta: string;
  preguntasFrecuentes: string;
  esloganPie: string;
  [clave: string]: string;
}
