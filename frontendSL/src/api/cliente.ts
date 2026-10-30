// Cliente para hablar con la API: arma la URL, manda y recibe JSON,
// agrega el token de la sesión y traduce los errores a mensajes en español.

const URL_API = import.meta.env.VITE_API_URL ?? 'https://localhost:7154';

export const CLAVE_TOKEN = 'sl_token';
export const CLAVE_EXPIRA = 'sl_expira';

// Se dispara cuando la API responde 401 a una petición con token (sesión vencida)
export const EVENTO_SESION_EXPIRADA = 'sl-sesion-expirada';

// Error con mensajes listos para mostrar al usuario
export class ErrorApi extends Error {
  readonly estado: number;
  readonly mensajes: string[];

  constructor(estado: number, mensajes: string[]) {
    super(mensajes.join(' '));
    this.name = 'ErrorApi';
    this.estado = estado;
    this.mensajes = mensajes;
  }
}

// Mensajes de Identity (vienen en inglés, con un código como clave)
const MENSAJES_IDENTITY: Record<string, string> = {
  DuplicateUserName: 'Ya existe una cuenta con ese correo.',
  DuplicateEmail: 'Ya existe una cuenta con ese correo.',
  InvalidEmail: 'El correo no es válido.',
  PasswordTooShort: 'La contraseña debe tener al menos 8 caracteres.',
  PasswordRequiresDigit: 'La contraseña debe tener al menos un número.',
  PasswordRequiresUpper: 'La contraseña debe tener al menos una letra mayúscula.',
  PasswordRequiresLower: 'La contraseña debe tener al menos una letra minúscula.',
  PasswordRequiresNonAlphanumeric: 'La contraseña debe tener al menos un símbolo (por ejemplo ! o #).',
};

const MENSAJES_POR_ESTADO: Record<number, string> = {
  400: 'Los datos enviados no son válidos.',
  401: 'Debe iniciar sesión.',
  403: 'No tiene permiso para hacer esto.',
  404: 'No se encontró lo que busca.',
  500: 'Ocurrió un error en el servidor. Intente de nuevo.',
};

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

// "The FechaEvento field is required." → "El campo FechaEvento es obligatorio."
function traducir(mensaje: string): string {
  const requerido = /^The (.+) field is required\.$/.exec(mensaje);
  if (requerido) {
    return `El campo ${requerido[1]} es obligatorio.`;
  }
  return mensaje;
}

function mensajesDeError(estado: number, cuerpo: ProblemDetails | null): string[] {
  if (cuerpo?.errors) {
    const mensajes = Object.entries(cuerpo.errors).flatMap(([clave, lista]) =>
      MENSAJES_IDENTITY[clave] ? [MENSAJES_IDENTITY[clave]] : lista.map(traducir),
    );
    if (mensajes.length > 0) {
      return [...new Set(mensajes)];
    }
  }
  return [MENSAJES_POR_ESTADO[estado] ?? `Error inesperado (${estado}).`];
}

export function obtenerToken(): string | null {
  const token = localStorage.getItem(CLAVE_TOKEN);
  const expira = Number(localStorage.getItem(CLAVE_EXPIRA) ?? 0);

  if (!token || Date.now() >= expira) {
    return null;
  }
  return token;
}

export function guardarSesion(token: string, segundos: number): void {
  localStorage.setItem(CLAVE_TOKEN, token);
  localStorage.setItem(CLAVE_EXPIRA, String(Date.now() + segundos * 1000));
}

export function borrarSesion(): void {
  localStorage.removeItem(CLAVE_TOKEN);
  localStorage.removeItem(CLAVE_EXPIRA);
}

async function pedir<T>(metodo: string, ruta: string, cuerpo?: unknown): Promise<T> {
  const token = obtenerToken();
  const cabeceras: Record<string, string> = { Accept: 'application/json' };

  if (cuerpo !== undefined) {
    cabeceras['Content-Type'] = 'application/json';
  }
  if (token) {
    cabeceras.Authorization = `Bearer ${token}`;
  }

  let respuesta: Response;
  try {
    respuesta = await fetch(`${URL_API}${ruta}`, {
      method: metodo,
      headers: cabeceras,
      body: cuerpo === undefined ? undefined : JSON.stringify(cuerpo),
    });
  } catch {
    throw new ErrorApi(0, ['No se pudo conectar con el servidor. Revise que la API esté encendida.']);
  }

  // Con token y 401: la sesión venció o ya no es válida
  if (respuesta.status === 401 && token) {
    borrarSesion();
    window.dispatchEvent(new Event(EVENTO_SESION_EXPIRADA));
  }

  const texto = await respuesta.text();
  const datos = texto ? JSON.parse(texto) : null;

  if (!respuesta.ok) {
    throw new ErrorApi(respuesta.status, mensajesDeError(respuesta.status, datos));
  }

  return datos as T;
}

export const api = {
  get: <T>(ruta: string) => pedir<T>('GET', ruta),
  post: <T>(ruta: string, cuerpo?: unknown) => pedir<T>('POST', ruta, cuerpo ?? {}),
  put: <T>(ruta: string, cuerpo?: unknown) => pedir<T>('PUT', ruta, cuerpo ?? {}),
  patch: <T>(ruta: string, cuerpo?: unknown) => pedir<T>('PATCH', ruta, cuerpo ?? {}),
  delete: <T>(ruta: string) => pedir<T>('DELETE', ruta),
};
