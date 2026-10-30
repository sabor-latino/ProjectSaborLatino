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
}
