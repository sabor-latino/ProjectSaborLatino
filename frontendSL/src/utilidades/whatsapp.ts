import type { Solicitud } from '../api/tipos';
import type { Idioma } from '../preferencias/PreferenciasContext';

export const NUMERO_WHATSAPP = '50685262096';

const TEXTOS_WHATSAPP: Record<Idioma, {
  saludo: string;
  solicitud: string;
  nombre: string;
  evento: string;
  fechaHora: string;
  lugar: string;
  paquete: string;
  invitados: string;
  telefono: string;
  comentarios: string;
  despedida: string;
  locale: string;
}> = {
  es: {
    saludo: 'Hola, quisiera solicitar una cotización para mi evento.',
    solicitud: 'Solicitud',
    nombre: 'Nombre',
    evento: 'Evento',
    fechaHora: 'Fecha y hora',
    lugar: 'Lugar',
    paquete: 'Paquete de interés',
    invitados: 'Invitados',
    telefono: 'Teléfono de contacto',
    comentarios: 'Comentarios',
    despedida: 'Quisiera confirmar la disponibilidad y recibir su propuesta.',
    locale: 'es-CR',
  },
  en: {
    saludo: 'Hello, I would like a quote for my event.',
    solicitud: 'Request',
    nombre: 'Name',
    evento: 'Event',
    fechaHora: 'Date and time',
    lugar: 'Location',
    paquete: 'Package of interest',
    invitados: 'Guests',
    telefono: 'Contact phone',
    comentarios: 'Comments',
    despedida: 'I would like to confirm availability and receive your proposal.',
    locale: 'en-GB',
  },
  fr: {
    saludo: 'Bonjour, je souhaiterais demander un devis pour mon événement.',
    solicitud: 'Demande',
    nombre: 'Nom',
    evento: 'Événement',
    fechaHora: 'Date et heure',
    lugar: 'Lieu',
    paquete: 'Forfait souhaité',
    invitados: 'Invités',
    telefono: 'Téléphone de contact',
    comentarios: 'Commentaires',
    despedida: 'Je souhaite vérifier la disponibilité et recevoir votre proposition.',
    locale: 'fr-FR',
  },
  pt: {
    saludo: 'Olá, gostaria de solicitar um orçamento para o meu evento.',
    solicitud: 'Solicitação',
    nombre: 'Nome',
    evento: 'Evento',
    fechaHora: 'Data e hora',
    lugar: 'Local',
    paquete: 'Pacote de interesse',
    invitados: 'Convidados',
    telefono: 'Telefone de contato',
    comentarios: 'Comentários',
    despedida: 'Gostaria de confirmar a disponibilidade e receber sua proposta.',
    locale: 'pt-BR',
  },
};

export function crearMensajeWhatsApp(solicitud: Solicitud, idioma: Idioma): string {
  const cfg = TEXTOS_WHATSAPP[idioma] ?? TEXTOS_WHATSAPP.es;
  const fecha = new Intl.DateTimeFormat(cfg.locale, {
    dateStyle: 'long',
    timeStyle: 'short',
  }).format(new Date(solicitud.fechaEvento));

  return [
    cfg.saludo,
    `${cfg.solicitud} #${solicitud.id}`,
    `${cfg.nombre}: ${solicitud.nombreContacto}`,
    `${cfg.evento}: ${solicitud.tipoEvento}`,
    `${cfg.fechaHora}: ${fecha}`,
    `${cfg.lugar}: ${solicitud.lugar}`,
    solicitud.paqueteNombre ? `${cfg.paquete}: ${solicitud.paqueteNombre}` : '',
    solicitud.cantidadInvitados != null ? `${cfg.invitados}: ${solicitud.cantidadInvitados}` : '',
    solicitud.telefonoContacto ? `${cfg.telefono}: ${solicitud.telefonoContacto}` : '',
    solicitud.comentarios ? `${cfg.comentarios}: ${solicitud.comentarios}` : '',
    cfg.despedida,
  ].filter(Boolean).join('\n');
}

export function crearUrlWhatsApp(mensaje: string): string {
  return `https://wa.me/${NUMERO_WHATSAPP}?text=${encodeURIComponent(mensaje)}`;
}

// Navegación en la misma pestaña: no depende de ventanas emergentes tras guardar.
export const navegacionWhatsApp = {
  abrir: (url: string) => window.location.assign(url),
};
