/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';

export type Idioma = 'es' | 'en';
export type EscalaFuente = 'normal' | 'grande' | 'extra';
export type ModoContraste = 'estandar' | 'alto-claro' | 'alto-oscuro';

const CLAVE_IDIOMA = 'sabor_idioma';
const CLAVE_FUENTE = 'sabor_fuente';
const CLAVE_CONTRASTE = 'sabor_contraste';

export const DICCIONARIO = {
  es: {
    saltarContenido: 'Saltar al contenido principal',
    inicio: 'Inicio',
    paquetes: 'Paquetes',
    galeria: 'Galería',
    preguntas: 'Preguntas',
    panel: 'Panel',
    iniciarSesion: 'Iniciar sesión',
    salir: 'Salir',
    solicitarCotizacion: 'Solicitar cotización',
    explorar: 'Explorar',
    suCuenta: 'Su cuenta',
    miCuenta: 'Mi cuenta',
    crearCuenta: 'Crear cuenta',
    preguntasFrecuentes: 'Preguntas frecuentes',
    esloganPie: 'Karaoke, música en vivo, animación y sonido profesional para sus celebraciones en Costa Rica.',
  },
  en: {
    saltarContenido: 'Skip to main content',
    inicio: 'Home',
    paquetes: 'Packages',
    galeria: 'Gallery',
    preguntas: 'FAQ',
    panel: 'Dashboard',
    iniciarSesion: 'Sign in',
    salir: 'Sign out',
    solicitarCotizacion: 'Request a quote',
    explorar: 'Explore',
    suCuenta: 'Your account',
    miCuenta: 'My account',
    crearCuenta: 'Create account',
    preguntasFrecuentes: 'Frequently asked questions',
    esloganPie: 'Karaoke, live music, hosting, and professional sound for your celebrations in Costa Rica.',
  },
} as const;

export interface Preferencias {
  idioma: Idioma;
  escalaFuente: EscalaFuente;
  contraste: ModoContraste;
  setIdioma: (idioma: Idioma) => void;
  alternarIdioma: () => void;
  setEscalaFuente: (escala: EscalaFuente) => void;
  setContraste: (contraste: ModoContraste) => void;
  t: (esTexto: string, enTexto: string) => string;
  textos: (typeof DICCIONARIO)[Idioma];
}

function leerIdiomaInicial(): Idioma {
  try {
    const guardado = window.localStorage.getItem(CLAVE_IDIOMA);
    if (guardado === 'es' || guardado === 'en') return guardado;
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
    // Ignorar errores de acceso a localStorage
  }
  return 'normal';
}

function leerContrasteInicial(): ModoContraste {
  try {
    const guardado = window.localStorage.getItem(CLAVE_CONTRASTE);
    if (guardado === 'estandar' || guardado === 'alto-claro' || guardado === 'alto-oscuro') return guardado;
  } catch {
    // Ignorar errores de acceso a localStorage
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
    } catch {
      // Ignorar
    }
    if (typeof document !== 'undefined') document.documentElement.lang = idioma;
  },
  alternarIdioma: () => {},
  setEscalaFuente: (escala) => {
    try {
      window.localStorage.setItem(CLAVE_FUENTE, escala);
    } catch {
      // Ignorar
    }
    if (typeof document !== 'undefined') document.documentElement.dataset.fontScale = escala;
  },
  setContraste: (contraste) => {
    try {
      window.localStorage.setItem(CLAVE_CONTRASTE, contraste);
    } catch {
      // Ignorar
    }
    if (typeof document !== 'undefined') document.documentElement.dataset.contrast = contraste;
  },
  t: (esTexto) => esTexto,
  textos: DICCIONARIO.es,
};

const ContextoPreferencias = createContext<Preferencias | null>(null);

export function PreferenciasProvider({ children }: { children: ReactNode }) {
  const [idioma, setIdiomaEstado] = useState<Idioma>(leerIdiomaInicial);
  const [escalaFuente, setEscalaFuenteEstado] = useState<EscalaFuente>(leerFuenteInicial);
  const [contraste, setContrasteEstado] = useState<ModoContraste>(leerContrasteInicial);

  useEffect(() => {
    aplicarEnDocumento(idioma, escalaFuente, contraste);
  }, [idioma, escalaFuente, contraste]);

  const setIdioma = useCallback((nuevo: Idioma) => {
    setIdiomaEstado(nuevo);
    try {
      window.localStorage.setItem(CLAVE_IDIOMA, nuevo);
    } catch {
      // Ignorar
    }
    if (typeof document !== 'undefined') {
      document.documentElement.lang = nuevo;
    }
  }, []);

  const alternarIdioma = useCallback(() => {
    setIdiomaEstado((actual) => {
      const siguiente: Idioma = actual === 'es' ? 'en' : 'es';
      try {
        window.localStorage.setItem(CLAVE_IDIOMA, siguiente);
      } catch {
        // Ignorar
      }
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
    } catch {
      // Ignorar
    }
    if (typeof document !== 'undefined') {
      document.documentElement.dataset.fontScale = nueva;
    }
  }, []);

  const setContraste = useCallback((nuevo: ModoContraste) => {
    setContrasteEstado(nuevo);
    try {
      window.localStorage.setItem(CLAVE_CONTRASTE, nuevo);
    } catch {
      // Ignorar
    }
    if (typeof document !== 'undefined') {
      document.documentElement.dataset.contrast = nuevo;
    }
  }, []);

  const t = useCallback(
    (esTexto: string, enTexto: string) => (idioma === 'en' ? enTexto : esTexto),
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
      textos: DICCIONARIO[idioma],
    }),
    [idioma, escalaFuente, contraste, setIdioma, alternarIdioma, setEscalaFuente, setContraste, t],
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
