using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.Models;

// Tipo de servicio que ofrece el negocio: Karaoke, Música en vivo, Sonido
public class Servicio
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NombreEn { get; set; }

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [MaxLength(1000)]
    public string? DescripcionEn { get; set; }

    [MaxLength(500)]
    public string? ImagenUrl { get; set; }

    public bool Activo { get; set; } = true;

    // Un servicio tiene varios paquetes
    public List<Paquete> Paquetes { get; set; } = [];
}
