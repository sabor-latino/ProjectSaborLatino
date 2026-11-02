using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Catalogo;

// Lo que el API devuelve para una promoción de temporada
public class PromocionTemporadaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? NombreEn { get; set; }
    public string? Descripcion { get; set; }
    public string? DescripcionEn { get; set; }
    public string? EtiquetaTemporada { get; set; }
    public string? EtiquetaTemporadaEn { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public string? CodigoPromocional { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? ImagenUrl { get; set; }
    public bool DestacadaEnInicio { get; set; }
    public bool Activo { get; set; }
    public bool Vigente { get; set; }
    public List<PromocionPaqueteItemDto> Paquetes { get; set; } = [];
}

// Paquete incluido en una promoción de temporada, con su precio base y precio con descuento
public class PromocionPaqueteItemDto
{
    public int PaqueteId { get; set; }
    public string PaqueteNombre { get; set; } = string.Empty;
    public string? PaqueteNombreEn { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal PrecioPromocional { get; set; }
}

// Lo que el API recibe al crear o editar una promoción de temporada
public class PromocionTemporadaGuardarDto
{
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? NombreEn { get; set; }

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [MaxLength(1000)]
    public string? DescripcionEn { get; set; }

    [MaxLength(80)]
    public string? EtiquetaTemporada { get; set; }

    [MaxLength(80)]
    public string? EtiquetaTemporadaEn { get; set; }

    [Range(typeof(decimal), "1", "90", ErrorMessage = "El porcentaje de descuento debe estar entre 1 y 90.")]
    public decimal PorcentajeDescuento { get; set; }

    [MaxLength(40)]
    public string? CodigoPromocional { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    [MaxLength(500), UrlHttpAbsoluta]
    public string? ImagenUrl { get; set; }

    public bool DestacadaEnInicio { get; set; } = true;

    public bool Activo { get; set; } = true;

    public List<int> PaqueteIds { get; set; } = [];
}
