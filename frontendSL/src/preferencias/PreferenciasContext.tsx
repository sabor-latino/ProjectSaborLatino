/* eslint-disable react-refresh/only-export-components */
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import {
  IDIOMAS_DISPONIBLES,
  type DiccionarioEstatico,
  type Idioma,
  type IdiomaConfig,
  type TraduccionesInline,
} from '../i18n/tipos';
import { diccionarioEs } from '../i18n/locales/es';
import { diccionarioEn } from '../i18n/locales/en';
import { diccionarioFr } from '../i18n/locales/fr';
import { diccionarioPt } from '../i18n/locales/pt';

export type { Idioma, IdiomaConfig };
export { IDIOMAS_DISPONIBLES };
export type EscalaFuente = 'normal' | 'grande' | 'extra';
export type ModoContraste = 'estandar' | 'alto-claro' | 'alto-oscuro';

const CLAVE_IDIOMA = 'sabor_idioma';
const CLAVE_FUENTE = 'sabor_fuente';
const CLAVE_CONTRASTE = 'sabor_contraste';

export const DICCIONARIO: Record<Idioma, DiccionarioEstatico> = {
  es: diccionarioEs,
  en: diccionarioEn,
  fr: diccionarioFr,
  pt: diccionarioPt,
};

// Catálogo dinámico para traducciones inyectadas en runtime
const CATALOGOS_DINAMICOS: Record<Idioma, Record<string, string>> = {
  es: {},
  en: {},
  fr: {},
  pt: {},
};

export interface Preferencias {
  idioma: Idioma;
  escalaFuente: EscalaFuente;
  contraste: ModoContraste;
  setIdioma: (idioma: Idioma) => void;
  alternarIdioma: () => void;
  setEscalaFuente: (escala: EscalaFuente) => void;
  setContraste: (contraste: ModoContraste) => void;
  t: (esTexto: string, traduccion?: string | null | TraduccionesInline) => string;
  tClave: (clave: string, params?: Record<string, string | number>) => string;
  registrarTraducciones: (idioma: Idioma, traducciones: Record<string, string>) => void;
  textos: DiccionarioEstatico;
}

function esIdiomaValido(valor: string | null): valor is Idioma {
  return valor === 'es' || valor === 'en' || valor === 'fr' || valor === 'pt';
}

function leerIdiomaInicial(): Idioma {
  try {
    const guardado = window.localStorage.getItem(CLAVE_IDIOMA);
    if (esIdiomaValido(guardado)) return guardado;
  } catch {
    // Ignorar errores de acceso a localStorage
  }
  return 'es';
}

function leerFuenteInicial(): EscalaFuente {
  try {
    const guardado = window.localStorage.getItem(CLAVE_FUENTE);
    if (guardado === 'normal' || guardado === 'grande' || guardado === 'extra') return guardado;
  } catch {
    // Ignorar
  }
  return 'normal';
}

function leerContrasteInicial(): ModoContraste {
  try {
    const guardado = window.localStorage.getItem(CLAVE_CONTRASTE);
    if (guardado === 'estandar' || guardado === 'alto-claro' || guardado === 'alto-oscuro') return guardado;
  } catch {
    // Ignorar
  }
  return 'estandar';
}

function aplicarEnDocumento(idioma: Idioma, escalaFuente: EscalaFuente, contraste: ModoContraste) {
  if (typeof document === 'undefined') return;
  document.documentElement.lang = idioma;
  document.documentElement.dataset.fontScale = escalaFuente;
  document.documentElement.dataset.contrast = contraste;
}

const valorPorDefecto: Preferencias = {
  idioma: 'es',
  escalaFuente: 'normal',
  contraste: 'estandar',
  setIdioma: (idioma) => {
    try {
      window.localStorage.setItem(CLAVE_IDIOMA, idioma);
    } catch { /* ignorar */ }
    if (typeof document !== 'undefined') document.documentElement.lang = idioma;
  },
  alternarIdioma: () => {},
  setEscalaFuente: (escala) => {
    try {
      window.localStorage.setItem(CLAVE_FUENTE, escala);
    } catch { /* ignorar */ }
    if (typeof document !== 'undefined') document.documentElement.dataset.fontScale = escala;
  },
  setContraste: (contraste) => {
    try {
      window.localStorage.setItem(CLAVE_CONTRASTE, contraste);
    } catch { /* ignorar */ }
    if (typeof document !== 'undefined') document.documentElement.dataset.contrast = contraste;
  },
  t: (esTexto) => esTexto,
  tClave: (clave) => DICCIONARIO.es[clave] ?? clave,
  registrarTraducciones: () => {},
  textos: DICCIONARIO.es,
};

const ContextoPreferencias = createContext<Preferencias | null>(null);

export function PreferenciasProvider({ children }: { children: ReactNode }) {
  const [idioma, setIdiomaEstado] = useState<Idioma>(leerIdiomaInicial);
  const [escalaFuente, setEscalaFuenteEstado] = useState<EscalaFuente>(leerFuenteInicial);
  const [contraste, setContrasteEstado] = useState<ModoContraste>(leerContrasteInicial);
  const [, setVersionDinamica] = useState(0);

  useEffect(() => {
    aplicarEnDocumento(idioma, escalaFuente, contraste);
  }, [idioma, escalaFuente, contraste]);

  const setIdioma = useCallback((nuevo: Idioma) => {
    setIdiomaEstado(nuevo);
    try {
      window.localStorage.setItem(CLAVE_IDIOMA, nuevo);
    } catch { /* ignorar */ }
    if (typeof document !== 'undefined') {
      document.documentElement.lang = nuevo;
    }
  }, []);

  const alternarIdioma = useCallback(() => {
    setIdiomaEstado((actual) => {
      const orden: Idioma[] = ['es', 'en', 'fr', 'pt'];
      const indexActual = orden.indexOf(actual);
      const siguiente = orden[(indexActual + 1) % orden.length];
      try {
        window.localStorage.setItem(CLAVE_IDIOMA, siguiente);
      } catch { /* ignorar */ }
      if (typeof document !== 'undefined') {
        document.documentElement.lang = siguiente;
      }
      return siguiente;
    });
  }, []);

  const setEscalaFuente = useCallback((nueva: EscalaFuente) => {
    setEscalaFuenteEstado(nueva);
    try {
      window.localStorage.setItem(CLAVE_FUENTE, nueva);
    } catch { /* ignorar */ }
    if (typeof document !== 'undefined') {
      document.documentElement.dataset.fontScale = nueva;
    }
  }, []);

  const setContraste = useCallback((nuevo: ModoContraste) => {
    setContrasteEstado(nuevo);
    try {
      window.localStorage.setItem(CLAVE_CONTRASTE, nuevo);
    } catch { /* ignorar */ }
    if (typeof document !== 'undefined') {
      document.documentElement.dataset.contrast = nuevo;
    }
  }, []);

  const registrarTraducciones = useCallback((targetIdioma: Idioma, traducciones: Record<string, string>) => {
    Object.assign(CATALOGOS_DINAMICOS[targetIdioma], traducciones);
    setVersionDinamica((v) => v + 1);
  }, []);

  useEffect(() => {
    let montado = true;
    const urlApi = import.meta.env.VITE_API_URL ?? 'http://localhost:5238';
    fetch(`${urlApi}/api/i18n/catalogo`)
      .then((res) => (res.ok ? res.json() : null))
      .then((catalogo: Record<string, Record<string, string>> | null) => {
        if (!montado || !catalogo) return;
        let huboCambios = false;
        (['en', 'fr', 'pt'] as Idioma[]).forEach((lang) => {
          if (catalogo[lang] && typeof catalogo[lang] === 'object') {
            Object.assign(CATALOGOS_DINAMICOS[lang], catalogo[lang]);
            huboCambios = true;
          }
        });
        if (huboCambios && montado) {
          setVersionDinamica((v) => v + 1);
        }
      })
      .catch(() => {
        // Fallback silencioso: no interrumpe la UI si la API está fuera de línea
      });

    return () => {
      montado = false;
    };
  }, []);

  // Función t inteligente: 100% retrocompatible con t(es, en) histórico y multilingüe para fr y pt
  const t = useCallback(
    (esTexto: string, traduccion?: string | null | TraduccionesInline): string => {
      if (!esTexto) return '';
      if (idioma === 'es') return esTexto;

      // 1. Catálogo dinámico inyectado en runtime
      if (CATALOGOS_DINAMICOS[idioma]?.[esTexto]) {
        return CATALOGOS_DINAMICOS[idioma][esTexto];
      }

      // 2. Diccionario estático de primer nivel
      if (DICCIONARIO[idioma]?.[esTexto]) {
        return DICCIONARIO[idioma][esTexto];
      }

      // 3. Caso A: Forma moderna con objeto de idiomas { en: '...', fr: '...', pt: '...' }
      if (typeof traduccion === 'object' && traduccion !== null) {
        return traduccion[idioma] ?? traduccion.en ?? esTexto;
      }

      // 4. Caso B: Forma histórica t(esTexto, enTexto)
      const enTexto = typeof traduccion === 'string' && traduccion.trim() ? traduccion : esTexto;
      if (idioma === 'en') return enTexto;

      // 5. Fallback a inglés o texto original en español
      return enTexto || esTexto;
    },
    [idioma],
  );

  const tClave = useCallback(
    (clave: string, params?: Record<string, string | number>): string => {
      let texto = CATALOGOS_DINAMICOS[idioma][clave] ?? DICCIONARIO[idioma][clave] ?? DICCIONARIO.es[clave] ?? clave;
      if (params) {
        Object.entries(params).forEach(([k, v]) => {
          texto = texto.replace(new RegExp(`{{${k}}}`, 'g'), String(v));
        });
      }
      return texto;
    },
    [idioma],
  );

  const valor = useMemo<Preferencias>(
    () => ({
      idioma,
      escalaFuente,
      contraste,
      setIdioma,
      alternarIdioma,
      setEscalaFuente,
      setContraste,
      t,
      tClave,
      registrarTraducciones,
      textos: DICCIONARIO[idioma],
    }),
    [idioma, escalaFuente, contraste, setIdioma, alternarIdioma, setEscalaFuente, setContraste, t, tClave, registrarTraducciones],
  );

  return <ContextoPreferencias.Provider value={valor}>{children}</ContextoPreferencias.Provider>;
}

export function PreferenciasAutoProvider({ children }: { children: ReactNode }) {
  const existente = useContext(ContextoPreferencias);
  if (existente) {
    return <>{children}</>;
  }
  return <PreferenciasProvider>{children}</PreferenciasProvider>;
}

export function usePreferencias(): Preferencias {
  return useContext(ContextoPreferencias) ?? valorPorDefecto;
}
