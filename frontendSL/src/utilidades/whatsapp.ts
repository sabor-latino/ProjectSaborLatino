import type { Solicitud } from '../api/tipos';
import type { Idioma } from '../preferencias/PreferenciasContext';

export const NUMERO_WHATSAPP = '50685262096';

export function crearMensajeWhatsApp(solicitud: Solicitud, idioma: Idioma): string {
  const t = (es: string, en: string) => idioma === 'en' ? en : es;
  const fecha = new Intl.DateTimeFormat(idioma === 'en' ? 'en-GB' : 'es-CR', {
    dateStyle: 'long', timeStyle: 'short',
  }).format(new Date(solicitud.fechaEvento));
  return [
    t('Hola, quisiera solicitar una cotización para mi evento.', 'Hello, I would like a quote for my event.'),
    `${t('Solicitud', 'Request')} #${solicitud.id}`,
    `${t('Nombre', 'Name')}: ${solicitud.nombreContacto}`,
    `${t('Evento', 'Event')}: ${solicitud.tipoEvento}`,
    `${t('Fecha y hora', 'Date and time')}: ${fecha}`,
    `${t('Lugar', 'Location')}: ${solicitud.lugar}`,
    solicitud.paqueteNombre ? `${t('Paquete de interés', 'Package of interest')}: ${solicitud.paqueteNombre}` : '',
    solicitud.cantidadInvitados != null ? `${t('Invitados', 'Guests')}: ${solicitud.cantidadInvitados}` : '',
    solicitud.telefonoContacto ? `${t('Teléfono de contacto', 'Contact phone')}: ${solicitud.telefonoContacto}` : '',
    solicitud.comentarios ? `${t('Comentarios', 'Comments')}: ${solicitud.comentarios}` : '',
    t('Quisiera confirmar la disponibilidad y recibir su propuesta.', 'I would like to confirm availability and receive your proposal.'),
  ].filter(Boolean).join('\n');
}

export function crearUrlWhatsApp(mensaje: string): string {
  return `https://wa.me/${NUMERO_WHATSAPP}?text=${encodeURIComponent(mensaje)}`;
}

// Navegación en la misma pestaña: no depende de ventanas emergentes tras guardar.
export const navegacionWhatsApp = {
  abrir: (url: string) => window.location.assign(url),
};
