using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.Models;

// Foto de la galería pública (eventos realizados)
public class ImagenGaleria
{
    public int Id { get; set; }

    [MaxLength(150)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? TituloEn { get; set; }

    [MaxLength(500)]
    public string ImagenUrl { get; set; } = string.Empty;

    // Posición en la que se muestra (1, 2, 3...)
    public int Orden { get; set; }

    public bool Activo { get; set; } = true;
}