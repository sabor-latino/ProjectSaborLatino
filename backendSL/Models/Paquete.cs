using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ProjectSaborLatino.Models;

// Lo que el cliente contrata, por ejemplo "Karaoke bailable"
public class Paquete
{
    public int Id { get; set; }

    // Servicio al que pertenece
    public int ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NombreEn { get; set; }

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [MaxLength(1000)]
    public string? DescripcionEn { get; set; }

    // Hasta 10 dígitos, 2 de ellos decimales (ej. 99999999.99)
    [Precision(10, 2)]
    public decimal PrecioBase { get; set; }

    [MaxLength(500)]
    public string? ImagenUrl { get; set; }

    [MaxLength(100)]
    public string? PotenciaSonido { get; set; }

    [MaxLength(100)]
    public string? CoberturaAforo { get; set; }

    [MaxLength(150)]
    public string? DuracionMontaje { get; set; }

    [MaxLength(250)]
    public string? RequerimientosElectricos { get; set; }

    [MaxLength(150)]
    public string? EspacioMinimo { get; set; }

    [MaxLength(1500)]
    public string? FichaTecnica { get; set; }

    [MaxLength(1500)]
    public string? FichaTecnicaEn { get; set; }

    public bool Activo { get; set; } = true;

    // Equipo que incluye el paquete (con su cantidad)
    public List<PaqueteEquipo> Equipos { get; set; } = [];
}
