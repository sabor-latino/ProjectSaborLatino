erDiagram
    ApplicationUser ||--o{ Solicitud : hace
    ApplicationUser ||--o{ Cotizacion : crea
    ApplicationUser ||--o| Empleado : es
    ApplicationUser ||--o{ EventoHistorialEstado : registra_cambio
    ApplicationUser {
        string Id PK
        string NombreCompleto
        bool Activo
        string FechaRegistro
    }

    Empleado ||--o{ EventoTrabajador : trabaja_en
    Empleado {
        int Id PK
        string Cedula
        string NombreCompleto
        string Puesto
        string Telefono
        bool Activo
        string UsuarioId FK
    }

    Servicio ||--o{ Paquete : ofrece
    Servicio {
        int Id PK
        string Nombre
        string Descripcion
        string ImagenUrl
        bool Activo
    }

    Paquete ||--o{ Solicitud : tiene
    Paquete ||--o{ Evento : incluye
    Paquete ||--o{ PaqueteEquipo : contiene
    Paquete {
        int Id PK
        int ServicioId FK
        string Nombre
        string Descripcion
        decimal PrecioBase
        string ImagenUrl
        bool Activo
    }

    Equipo ||--o{ PaqueteEquipo : pertenece_a
    Equipo ||--o{ EventoEquipo : reservado_en
    Equipo {
        int Id PK
        string Nombre
        string Tipo
        int CantidadTotal
        bool Activo
    }

    PaqueteEquipo {
        int PaqueteId FK
        int EquipoId FK
        int Cantidad
    }

    Solicitud ||--o{ Cotizacion : recibe
    Solicitud {
        int Id PK
        string ClienteUsuarioId FK
        string NombreContacto
        string CorreoContacto
        string TelefonoContacto
        int PaqueteId FK
        string TipoEvento
        string FechaEvento
        string Lugar
        int CantidadInvitados
        string Comentarios
        string Estado
        string FechaCreacion
    }

    Cotizacion ||--o| Evento : genera
    Cotizacion {
        int Id PK
        int SolicitudId FK
        decimal Monto
        decimal Descuento
        string Detalle
        string VigenteHasta
        string Estado
        string CreadaPorUsuarioId FK
        string FechaCreacion
    }

    Evento ||--o{ EventoEquipo : tiene
    Evento ||--o{ EventoTrabajador : asigna
    Evento ||--o{ EventoHistorialEstado : registra
    Evento {
        int Id PK
        int CotizacionId FK
        int PaqueteId FK
        string FechaInicio
        string FechaFin
        string Lugar
        string Estado
        string MotivoCancelacion
        string Notas
        string FechaCreacion
    }

    EventoEquipo {
        int EventoId FK
        int EquipoId FK
        int Cantidad
    }

    EventoTrabajador {
        int EventoId FK
        int EmpleadoId FK
        string Funcion
    }

    EventoHistorialEstado {
        int Id PK
        int EventoId FK
        string EstadoAnterior
        string EstadoNuevo
        string UsuarioId FK
        string Fecha
        string Comentario
    }

    ImagenGaleria {
        int Id PK
        string Titulo
        string ImagenUrl
        int Orden
        bool Activo
    }
    PreguntaFrecuente {
        int Id PK
        string Pregunta
        string Respuesta
        int Orden
        bool Activo
    }
    Configuracion {
        string Clave PK
        string Valor
        string Descripcion
    }
    Bitacora {
        int Id PK
        string UsuarioId
        string Accion
        string Entidad
        string EntidadId
        string Detalle
        string Fecha
    }
