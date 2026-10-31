// Tipos de los datos que devuelve y recibe la API (copian los DTOs del backend)

// =============== Catálogo ===============

export interface Servicio {
  id: number;
  nombre: string;
  descripcion: string | null;
  imagenUrl: string | null;
  activo: boolean;
}

export interface PaqueteEquipo {
  equipoId: number;
  equipoNombre: string;
  cantidad: number;
}

export interface Paquete {
  id: number;
  servicioId: number;
  servicioNombre: string;
  nombre: string;
  descripcion: string | null;
  precioBase: number;
  imagenUrl: string | null;
  activo: boolean;
  equipos: PaqueteEquipo[];
}

export interface ImagenGaleria {
  id: number;
  titulo: string;
  imagenUrl: string;
  orden: number;
  activo: boolean;
}

export interface PreguntaFrecuente {
  id: number;
  pregunta: string;
  respuesta: string;
  orden: number;
  activo: boolean;
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
  correoContacto: string;
  telefonoContacto?: string;
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
  correoContacto: string;
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
  eventosFinalizados: number;
  eventosMinimos: number;
  porcentajeDescuento: number;
  aplica: boolean;
  precioBase: number | null;
  descuentoSugerido: number | null;
}

// =============== Eventos ===============

export interface Equipo {
  id: number;
  nombre: string;
  tipo: string | null;
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
