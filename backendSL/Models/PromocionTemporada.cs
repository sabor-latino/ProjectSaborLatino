using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ProjectSaborLatino.Models;

// Promoción por temporada o campaña especial aplicable a paquetes del catálogo
public class PromocionTemporada
{
    public int Id { get; set; }

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

    [Precision(5, 2)]
    public decimal PorcentajeDescuento { get; set; }

    [MaxLength(40)]
    public string? CodigoPromocional { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    [MaxLength(500)]
    public string? ImagenUrl { get; set; }

    public bool DestacadaEnInicio { get; set; } = true;

    public bool Activo { get; set; } = true;

    public List<PromocionTemporadaPaquete> Paquetes { get; set; } = [];
}
