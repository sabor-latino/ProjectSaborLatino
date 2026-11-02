using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Eventos;

// Lo que el API devuelve: el evento con su equipo, trabajadores e historial
public class EventoDto
{
    public int Id { get; set; }
    public int CotizacionId { get; set; }
    public int SolicitudId { get; set; }
    public string NombreContacto { get; set; } = string.Empty;
    public int? PaqueteId { get; set; }
    public string? PaqueteNombre { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? MotivoCancelacion { get; set; }
    public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<EventoEquipoDto> Equipos { get; set; } = [];
    public List<EventoTrabajadorDto> Trabajadores { get; set; } = [];
    public List<EventoHistorialDto> Historial { get; set; } = [];
}

public class EventoEquipoDto
{
    public int EquipoId { get; set; }
    public string EquipoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class EventoTrabajadorDto
{
    public int EmpleadoId { get; set; }
    public string EmpleadoNombre { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public string? Funcion { get; set; }
}

public class EventoHistorialDto
{
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public string UsuarioId { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Comentario { get; set; }
}

// Lo que manda el admin para crear el evento a partir de una cotización aceptada
public class EventoCrearDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una cotización válida.")]
    public int CotizacionId { get; set; }

    [Required]
    public DateTime? FechaInicio { get; set; }

    [Required]
    public DateTime? FechaFin { get; set; }

    // Si no viene, se usa el lugar de la solicitud
    [MaxLength(300)]
    public string? Lugar { get; set; }

    [MaxLength(2000)]
    public string? Notas { get; set; }

    // true: reserva el mismo equipo que trae el paquete
    public bool CopiarEquipoDelPaquete { get; set; } = true;
}

// Lo que manda el admin para modificar un evento existente (fechas, lugar y notas)
public class EventoModificarDto
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    [MaxLength(300)]
    public string? Lugar { get; set; }

    [MaxLength(2000)]
    public string? Notas { get; set; }
}

// Un elemento de la lista de PUT api/eventos/{id}/equipos
public class EventoEquipoAsignarDto
{
    [Range(1, int.MaxValue)]
    public int EquipoId { get; set; }

    [Range(1, 10000, ErrorMessage = "La cantidad debe ser al menos 1.")]
    public int Cantidad { get; set; }
}

// Un elemento de la lista de PUT api/eventos/{id}/trabajadores
public class EventoTrabajadorAsignarDto
{
    [Range(1, int.MaxValue)]
    public int EmpleadoId { get; set; }

    [MaxLength(100)]
    public string? Funcion { get; set; }
}

// PATCH api/eventos/{id}/estado
public class EventoCambiarEstadoDto
{
    // El siguiente estado: "EnMontaje", "EnServicio" o "Finalizado"
    [Required]
    public string Estado { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Comentario { get; set; }
}

// PATCH api/eventos/{id}/cancelar
public class EventoCancelarDto
{
    [Required, MaxLength(500)]
    public string Motivo { get; set; } = string.Empty;
}

// GET api/equipos/disponibilidad
public class DisponibilidadEquipoDto
{
    public int EquipoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadTotal { get; set; }
    public int Reservada { get; set; }
    public int Disponible { get; set; }
}
