using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Catalogo;

// Lo que el API devuelve
public class ServicioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? NombreEn { get; set; }
    public string? Descripcion { get; set; }
    public string? DescripcionEn { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; }
}

// Lo que el API recibe al crear o editar
public class ServicioGuardarDto
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NombreEn { get; set; }

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    [MaxLength(1000)]
    public string? DescripcionEn { get; set; }

    [MaxLength(500), UrlHttpAbsoluta]
    public string? ImagenUrl { get; set; }

    public bool Activo { get; set; } = true;
}
