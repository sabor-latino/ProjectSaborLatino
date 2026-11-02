using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.Models;

// Equipo físico del negocio: parlantes, micrófonos, consola, luces...
public class Equipo
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NombreEn { get; set; }

    // Ej. "Audio", "Iluminación", "Instrumento"
    [MaxLength(50)]
    public string? Tipo { get; set; }

    [MaxLength(500)]
    public string? EspecificacionTecnica { get; set; }

    [MaxLength(500)]
    public string? EspecificacionTecnicaEn { get; set; }

    // Cuántas unidades tiene el negocio en total
    public int CantidadTotal { get; set; }

    public bool Activo { get; set; } = true;
}
