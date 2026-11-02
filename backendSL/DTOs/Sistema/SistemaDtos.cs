using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Sistema;

// =============== Configuración ===============

public class ConfiguracionDto
{
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

// PUT api/configuracion/{clave}
public class ConfiguracionGuardarDto
{
    [Required, MaxLength(500)]
    public string Valor { get; set; } = string.Empty;
}

// =============== Bitácora ===============

public class BitacoraDto
{
    public int Id { get; set; }
    public string? UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string? EntidadId { get; set; }
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; }
}

// =============== Fidelidad ===============

// GET api/solicitudes/{id}/fidelidad
public class FidelidadDto
{
    public int SolicitudId { get; set; }
    public string? ClienteUsuarioId { get; set; }
    public int EventosFinalizados { get; set; }
    public int EventosMinimos { get; set; }
    public int PorcentajeDescuento { get; set; }
    public bool Aplica { get; set; }

    // Solo si la solicitud tiene paquete
    public decimal? PrecioBase { get; set; }
    public decimal? DescuentoSugerido { get; set; }

    // Promoción de temporada aplicable al paquete de la solicitud (si existe y está vigente)
    public string? PromocionTemporadaNombre { get; set; }
    public decimal PorcentajePromocionTemporada { get; set; }
    public decimal? DescuentoPromocionSugerido { get; set; }
}

// =============== Reportes ===============

// GET api/reportes/resumen
public class ResumenDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public List<ConteoDto> EventosPorEstado { get; set; } = [];
    public decimal Ingresos { get; set; }
    public List<ConteoDto> TopPaquetes { get; set; } = [];
    public List<ClienteFrecuenteDto> TopClientes { get; set; } = [];
    public int SolicitudesPendientes { get; set; }
}

// Un nombre con su cantidad (estado → eventos, paquete → contrataciones)
public class ConteoDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class ClienteFrecuenteDto
{
    public string ClienteUsuarioId { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int EventosFinalizados { get; set; }
}
