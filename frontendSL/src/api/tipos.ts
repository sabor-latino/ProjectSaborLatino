// Tipos de los datos que devuelve y recibe la API (copian los DTOs del backend)

// =============== Catálogo ===============

export interface Servicio {
  id: number;
  nombre: string;
  nombreEn?: string | null;
  descripcion: string | null;
  descripcionEn?: string | null;
  imagenUrl: string | null;
  activo: boolean;
}

export interface PaqueteEquipo {
  equipoId: number;
  equipoNombre: string;
  equipoNombreEn?: string | null;
  especificacionTecnica?: string | null;
  especificacionTecnicaEn?: string | null;
  cantidad: number;
}

export interface Paquete {
  id: number;
  servicioId: number;
  servicioNombre: string;
  servicioNombreEn?: string | null;
  nombre: string;
  nombreEn?: string | null;
  descripcion: string | null;
  descripcionEn?: string | null;
  precioBase: number;
  imagenUrl: string | null;
  potenciaSonido?: string | null;
  coberturaAforo?: string | null;
  duracionMontaje?: string | null;
  requerimientosElectricos?: string | null;
  espacioMinimo?: string | null;
  fichaTecnica?: string | null;
  fichaTecnicaEn?: string | null;
  activo: boolean;
  equipos: PaqueteEquipo[];
}

export interface ImagenGaleria {
  id: number;
  titulo: string;
  tituloEn?: string | null;
  imagenUrl: string;
  orden: number;
  activo: boolean;
}

export interface PreguntaFrecuente {
  id: number;
  pregunta: string;
  preguntaEn?: string | null;
  respuesta: string;
  respuestaEn?: string | null;
  orden: number;
  activo: boolean;
}

export interface PromocionPaqueteItem {
  paqueteId: number;
  paqueteNombre: string;
  paqueteNombreEn?: string | null;
  precioBase: number;
  precioPromocional: number;
}

export interface PromocionTemporada {
  id: number;
  nombre: string;
  nombreEn?: string | null;
  descripcion?: string | null;
  descripcionEn?: string | null;
  etiquetaTemporada?: string | null;
  etiquetaTemporadaEn?: string | null;
  porcentajeDescuento: number;
  codigoPromocional?: string | null;
  fechaInicio: string;
  fechaFin: string;
  imagenUrl?: string | null;
  destacadaEnInicio: boolean;
  activo: boolean;
  vigente: boolean;
  paquetes: PromocionPaqueteItem[];
}

export interface ContenidoInicio {
  etiquetaPortada: string;
  etiquetaPortadaEn: string;
  tituloPortada: string;
  tituloPortadaEn: string;
  subtituloPortada: string;
  subtituloPortadaEn: string;
  fotoPortadaUrl: string | null;
  fotoCotizarUrl: string | null;
  paso1Titulo: string;
  paso1TituloEn: string;
  paso1Texto: string;
  paso1TextoEn: string;
  paso2Titulo: string;
  paso2TituloEn: string;
  paso2Texto: string;
  paso2TextoEn: string;
  paso3Titulo: string;
  paso3TituloEn: string;
  paso3Texto: string;
  paso3TextoEn: string;
  ctaTitulo: string;
  ctaTituloEn: string;
  ctaSubtitulo: string;
  ctaSubtituloEn: string;
}

// =============== Cuenta ===============

export type Rol = 'Superadministrador' | 'Administrador' | 'Trabajador' | 'Cliente';

export interface Perfil {
  id: string;
  nombreCompleto: string;
  email: string;
  telefono: string | null;
  roles: Rol[];
  fechaRegistro: string;
}

export interface RegistroDatos {
  nombreCompleto: string;
  email: string;
  password: string;
  telefono?: string;
}

// Respuesta de POST /api/identity/login
export interface RespuestaLogin {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

// =============== Solicitudes ===============

export interface SolicitudCrear {
  nombreContacto: string;
  correoContacto?: string | null;
  telefonoContacto?: string;
  claveIdempotencia?: string;
  paqueteId?: number;
  tipoEvento: string;
  fechaEvento: string;
  lugar: string;
  cantidadInvitados?: number;
  comentarios?: string;
}

export interface Solicitud {
  id: number;
  nombreContacto: string;
  correoContacto: string | null;
  recibidoWhatsAppEn?: string | null;
  recibidoWhatsAppPorUsuarioId?: string | null;
  paqueteId: number | null;
  paqueteNombre: string | null;
  tipoEvento: string;
  fechaEvento: string;
  lugar: string;
  estado: string;
  fechaCreacion: string;
  clienteUsuarioId?: string | null;
  telefonoContacto?: string | null;
  cantidadInvitados?: number | null;
  comentarios?: string | null;
  cotizaciones?: Cotizacion[];
}

export interface Cotizacion {
  id: number;
  solicitudId: number;
  monto: number;
  descuento: number;
  total: number;
  detalle: string | null;
  vigenteHasta: string;
  estado: string;
  fechaCreacion: string;
}

export interface Fidelidad {
  solicitudId: number;
  clienteUsuarioId?: string | null;
  eventosFinalizados: number;
  eventosMinimos: number;
  porcentajeDescuento: number;
  aplica: boolean;
  precioBase: number | null;
  descuentoSugerido: number | null;
  promocionTemporadaNombre?: string | null;
  porcentajePromocionTemporada?: number;
  descuentoPromocionSugerido?: number | null;
}

// =============== Eventos ===============

export interface Equipo {
  id: number;
  nombre: string;
  nombreEn?: string | null;
  tipo: string | null;
  especificacionTecnica?: string | null;
  especificacionTecnicaEn?: string | null;
  cantidadTotal: number;
  activo: boolean;
}

export interface Empleado {
  id: number;
  cedula: string;
  nombreCompleto: string;
  puesto: string;
  telefono: string | null;
  activo: boolean;
  usuarioId: string | null;
}

export interface Evento {
  id: number;
  cotizacionId: number;
  solicitudId: number;
  nombreContacto: string;
  paqueteId: number | null;
  paqueteNombre: string | null;
  fechaInicio: string;
  fechaFin: string;
  lugar: string;
  estado: string;
  motivoCancelacion: string | null;
  notas: string | null;
  fechaCreacion: string;
  equipos: { equipoId: number; equipoNombre: string; cantidad: number }[];
  trabajadores: { empleadoId: number; empleadoNombre: string; puesto: string; funcion: string | null }[];
  historial: { estadoAnterior: string | null; estadoNuevo: string; fecha: string; comentario: string | null }[];
}

export interface Disponibilidad {
  equipoId: number;
  nombre: string;
  cantidadTotal: number;
  reservada: number;
  disponible: number;
}

// =============== Sistema ===============

export interface Usuario {
  id: string;
  email: string;
  nombreCompleto: string;
  telefono: string | null;
  rol: Rol;
  activo: boolean;
  fechaRegistro: string;
}

export interface Configuracion {
  clave: string;
  valor: string;
  descripcion: string | null;
}

export interface RegistroBitacora {
  id: number;
  usuarioId: string | null;
  accion: string;
  entidad: string;
  entidadId: string | null;
  detalle: string | null;
  fecha: string;
}

export interface Conteo {
  nombre: string;
  cantidad: number;
}

export interface Resumen {
  anio: number;
  mes: number;
  eventosPorEstado: Conteo[];
  ingresos: number;
  topPaquetes: Conteo[];
  topClientes: { clienteUsuarioId: string; nombreCompleto: string; email: string; eventosFinalizados: number }[];
  solicitudesPendientes: number;
}
