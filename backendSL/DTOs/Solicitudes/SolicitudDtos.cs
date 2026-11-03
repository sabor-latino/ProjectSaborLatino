using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Solicitudes;

// Lo que el API devuelve
public class SolicitudDto
{
    public int Id { get; set; }
    public string? ClienteUsuarioId { get; set; }
    public string NombreContacto { get; set; } = string.Empty;
    public string? CorreoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
    public int? PaqueteId { get; set; }
    public string? PaqueteNombre { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public DateTime FechaEvento { get; set; }
    public string Lugar { get; set; } = string.Empty;
    public int? CantidadInvitados { get; set; }
    public string? Comentarios { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? RecibidoWhatsAppEn { get; set; }
    public string? RecibidoWhatsAppPorUsuarioId { get; set; }
    public List<CotizacionDto> Cotizaciones { get; set; } = [];
}

// Lo que manda el visitante o el cliente al pedir una cotización
public class SolicitudCrearDto
{
    [Required, MaxLength(150)]
    public string NombreContacto { get; set; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? CorreoContacto { get; set; }

    [MaxLength(30)]
    public string? TelefonoContacto { get; set; }

    public Guid? ClaveIdempotencia { get; set; }

    // Opcional: el paquete que le interesa
    public int? PaqueteId { get; set; }

    [Required, MaxLength(100)]
    public string TipoEvento { get; set; } = string.Empty;

    [Required]
    public DateTime? FechaEvento { get; set; }

    [Required, MaxLength(300)]
    public string Lugar { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "La cantidad de invitados debe ser al menos 1.")]
    public int? CantidadInvitados { get; set; }

    [MaxLength(2000)]
    public string? Comentarios { get; set; }
}

// Lo que manda el admin para rechazar o cancelar una solicitud
public class SolicitudCambiarEstadoDto
{
    // "Rechazada" o "Cancelada"
    [Required]
    public string Estado { get; set; } = string.Empty;
}
