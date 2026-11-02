using System.ComponentModel.DataAnnotations;

namespace ProjectSaborLatino.DTOs.Catalogo;

// Lo que el API devuelve
public class EquipoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? NombreEn { get; set; }
    public string? Tipo { get; set; }
    public string? EspecificacionTecnica { get; set; }
    public string? EspecificacionTecnicaEn { get; set; }
    public int CantidadTotal { get; set; }
    public bool Activo { get; set; }
}

// Lo que el API recibe al crear o editar
public class EquipoGuardarDto
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NombreEn { get; set; }

    [MaxLength(50)]
    public string? Tipo { get; set; }

    [MaxLength(500)]
    public string? EspecificacionTecnica { get; set; }

    [MaxLength(500)]
    public string? EspecificacionTecnicaEn { get; set; }

    [Range(0, 10000, ErrorMessage = "La cantidad no puede ser negativa.")]
    public int CantidadTotal { get; set; }

    public bool Activo { get; set; } = true;
}
