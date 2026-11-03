import type { Solicitud, SolicitudCrear } from '../api/tipos';

const CLAVE = 'sabor_solicitud_whatsapp';
const VIGENCIA = 60 * 60 * 1000;

export interface IntentoSolicitud {
  huella: string;
  clave: string;
  cuerpo: SolicitudCrear;
}
export interface RecuperacionSolicitud {
  intento?: IntentoSolicitud;
  solicitud?: Solicitud;
}

export function limpiarSolicitudPendiente(): void {
  try { sessionStorage.removeItem(CLAVE); } catch { /* Almacenamiento no disponible. */ }
}

export function leerSolicitudPendiente(actor: string): RecuperacionSolicitud | null {
  try {
    const texto = sessionStorage.getItem(CLAVE);
    if (!texto) return null;
    if (texto.length > 30000) throw new Error('Registro inválido');
    const guardado = JSON.parse(texto);
    if (guardado.actor !== actor || typeof guardado.creado !== 'number' ||
        guardado.creado > Date.now() || Date.now() - guardado.creado > VIGENCIA) {
      limpiarSolicitudPendiente();
      return null;
    }
    if (guardado.solicitud) {
      const s = guardado.solicitud;
      if (!Number.isInteger(s.id) || s.id < 1 || !['nombreContacto', 'tipoEvento', 'lugar', 'fechaEvento'].every((campo) =>
        typeof s[campo] === 'string') || !Number.isFinite(Date.parse(s.fechaEvento))) throw new Error('Solicitud inválida');
      return { solicitud: s };
    }
    const intento = guardado.intento;
    if (!intento || typeof intento.clave !== 'string' || typeof intento.huella !== 'string' ||
        !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(intento.clave) ||
        !intento.cuerpo || !['nombreContacto', 'tipoEvento', 'lugar', 'fechaEvento'].every((campo) =>
          typeof intento.cuerpo[campo] === 'string') || !Number.isFinite(Date.parse(intento.cuerpo.fechaEvento)) ||
        !['correoContacto', 'telefonoContacto', 'comentarios'].every((campo) =>
          intento.cuerpo[campo] == null || typeof intento.cuerpo[campo] === 'string')) throw new Error('Intento inválido');
    return { intento };
  } catch {
    limpiarSolicitudPendiente();
    return null;
  }
}

export function guardarSolicitudPendiente(actor: string, datos: RecuperacionSolicitud): boolean {
  try {
    // Guardar solo la solicitud de este flujo. Nunca perfil, tokens o cotizaciones del panel.
    const s = datos.solicitud;
    const solicitud = s ? {
      id: s.id, nombreContacto: s.nombreContacto, correoContacto: s.correoContacto,
      telefonoContacto: s.telefonoContacto, paqueteId: s.paqueteId, paqueteNombre: s.paqueteNombre,
      tipoEvento: s.tipoEvento, fechaEvento: s.fechaEvento, lugar: s.lugar,
      cantidadInvitados: s.cantidadInvitados, comentarios: s.comentarios,
      estado: s.estado, fechaCreacion: s.fechaCreacion,
    } : undefined;
    sessionStorage.setItem(CLAVE, JSON.stringify({ actor, creado: Date.now(), intento: datos.intento, solicitud }));
    return true;
  } catch {
    return false;
  }
}
